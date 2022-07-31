using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class IfElseStatement : IBlockItem
{
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

    bool IBlockItem.HasDefiniteReturn => _trueBranch.HasDefiniteReturn && _falseBranch?.HasDefiniteReturn == true;

    public IBlockItem Lower() => new IfElseStatement(_expression.Lower(), _trueBranch.Lower(), _falseBranch?.Lower());

    public void EmitTo(IDeclarationScope scope)
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
