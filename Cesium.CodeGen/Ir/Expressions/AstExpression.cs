using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Generators;

namespace Cesium.CodeGen.Ir.Expressions;

internal class AstExpression : IExpression
{
    private readonly Expression _expression;

    public AstExpression(Expression expression)
    {
        _expression = expression;
    }

    public IExpression Lower() => Lowering.LowerExpression(_expression).ToIntermediate();

    public void EmitTo(FunctionScope scope) => Generators.Expressions.EmitExpression(scope, _expression);
}
