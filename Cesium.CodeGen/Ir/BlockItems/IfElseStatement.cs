using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class IfElseStatement : IBlockItem
{
    public List<IBlockItem>? NextNodes { get; set; }
    public IBlockItem? Parent { get; set; }

    private readonly IExpression _expression;
    private readonly IBlockItem _trueBranch;
    private readonly IBlockItem? _falseBranch;

    private IfElseStatement(IExpression expression, IBlockItem trueBranch, IBlockItem? falseBranch)
    {
        _expression = expression;
        _trueBranch = trueBranch;
        _falseBranch = falseBranch;
    }

    public IfElseStatement(Ast.IfElseStatement statement)
    {
        var (expression, trueBranch, falseBranch) = statement;
        _expression = expression.ToIntermediate();
        _trueBranch = trueBranch.ToIntermediate();
        _falseBranch = falseBranch?.ToIntermediate();
    }

    public void ResolveNextNodes(IBlockItem root, IBlockItem parent)
    {
        NextNodes = new List<IBlockItem> { _trueBranch };

        _trueBranch.ResolveNextNodes(root, this);

        if (_falseBranch != null)
        {
            NextNodes.Add(_falseBranch);

            _falseBranch.ResolveNextNodes(root, this);
        }
    }

    public IEnumerable<IBlockItem> GetChildren(IBlockItem root)
    {
        yield return _trueBranch;

        if (_falseBranch != null)
            yield return _falseBranch;
    }

    bool IBlockItem.HasDefiniteReturn => _trueBranch.HasDefiniteReturn && _falseBranch?.HasDefiniteReturn == true;

    public IBlockItem Lower(IDeclarationScope scope) => new IfElseStatement(_expression.Lower(scope), _trueBranch.Lower(scope), _falseBranch?.Lower(scope));

    public void EmitTo(IEmitScope scope)
    {
        var bodyProcessor = scope.Method.Body.GetILProcessor();
        var ifFalseLabel = bodyProcessor.Create(OpCodes.Nop);

        _expression.EmitTo(scope);
        bodyProcessor.Emit(OpCodes.Brfalse, ifFalseLabel);

        _trueBranch.EmitTo(scope);

        if (_falseBranch == null)
        {
            bodyProcessor.Append(ifFalseLabel);
            return;
        }

        if (_trueBranch.HasDefiniteReturn)
        {
            bodyProcessor.Append(ifFalseLabel);
            _falseBranch.EmitTo(scope);
        }
        else
        {
            var statementEndLabel = bodyProcessor.Create(OpCodes.Nop);
            bodyProcessor.Emit(OpCodes.Br, statementEndLabel);

            bodyProcessor.Append(ifFalseLabel);
            _falseBranch.EmitTo(scope);
            bodyProcessor.Append(statementEndLabel);
        }
    }
}
