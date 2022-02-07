using Cesium.Ast;
using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.Expressions;

internal class AstExpression : IExpression // TODO[#73]: Delete this class.
{
    private readonly Expression _expression;

    public AstExpression(Expression expression)
    {
        _expression = expression;
    }

    public IExpression Lower() => this;

    public void EmitTo(FunctionScope scope) => Generators.Expressions.EmitExpression(scope, _expression);
}
