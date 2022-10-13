using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions;

internal class ConstantExpression : IExpression
{
    internal IExpression Expression { get; }

    public ConstantExpression(Ast.ConstantExpression expression)
    {
        Expression = expression.Expression.ToIntermediate();
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        return Expression.Lower(scope);
    }

    public IType GetExpressionType(IDeclarationScope scope)
    {
        return Expression.GetExpressionType(scope);
    }

    public void EmitTo(IEmitScope scope)
    {
        Expression.EmitTo(scope);
    }
}
