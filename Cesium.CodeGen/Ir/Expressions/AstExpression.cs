using Cesium.Ast;
using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.Expressions;

public class AstExpression : IExpression
{
    private readonly Expression _expression;

    public AstExpression(Expression expression)
    {
        _expression = expression;
    }

    public void EmitTo(FunctionScope scope) => Generators.Expressions.EmitExpression(scope, _expression);
}
