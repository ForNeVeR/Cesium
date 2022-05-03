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

    public IBlockItem Lower() => new IfElseStatement(_expression.Lower(), _trueBranch.Lower(), _falseBranch?.Lower());

    public void EmitTo(IDeclarationScope scope)
    {
        // TODO[#113]: when branch ends with ret opcode if can be optimized and not generate jmp label

        _expression.EmitTo(scope);
        var bodyProcessor = scope.Method.Body.GetILProcessor();
        var elseStartLabel = bodyProcessor.Create(OpCodes.Nop);
        bodyProcessor.Emit(OpCodes.Brfalse, elseStartLabel);

        _trueBranch.EmitTo(scope);

        if (_falseBranch == null)
        {
            bodyProcessor.Append(elseStartLabel);
            return;
        }

        var statementEndLabel = bodyProcessor.Create(OpCodes.Nop);
        bodyProcessor.Emit(OpCodes.Br, statementEndLabel);

        bodyProcessor.Append(elseStartLabel);
        _falseBranch?.EmitTo(scope);
        bodyProcessor.Append(statementEndLabel);
    }
}
