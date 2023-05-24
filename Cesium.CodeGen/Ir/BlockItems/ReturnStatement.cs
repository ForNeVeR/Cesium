using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class ReturnStatement : IBlockItem
{
    public IExpression? Expression { get; }

    public ReturnStatement(Ast.ReturnStatement statement)
    {
        Expression = statement.Expression?.ToIntermediate();
    }

    public ReturnStatement(IExpression? expression)
    {
        Expression = expression;
    }

    public void EmitTo(IEmitScope scope)
    {
        Expression?.EmitTo(scope);

        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    }
}
