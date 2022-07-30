using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class ReturnStatement : IBlockItem
{
    private readonly IExpression _expression;

    public ReturnStatement(Ast.ReturnStatement statement)
    {
        _expression = statement.Expression.ToIntermediate();
    }

    private ReturnStatement(IExpression expression)
    {
        _expression = expression;
    }

    bool IBlockItem.HasDefiniteReturn => true;

    public IBlockItem Lower() => new ReturnStatement(_expression.Lower());

    public void EmitTo(IDeclarationScope scope)
    {
        _expression.EmitTo(scope);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    }
}
