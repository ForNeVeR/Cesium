using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class ExpressionSizeOfOperatorExpression : IExpression
{
    private readonly IExpression _expression;

    public ExpressionSizeOfOperatorExpression(IExpression expression)
    {
        _expression = expression;
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        IType? typeResolved = null;
        if (_expression is IdentifierExpression identifierExpression)
        {
            typeResolved = scope.TryGetType(identifierExpression.Identifier);
        }

        typeResolved ??= _expression.GetExpressionType(scope);
        var sizeOfExpression = new SizeOfOperatorExpression(typeResolved);
        return sizeOfExpression.Lower(scope);
    }

    public void EmitTo(IEmitScope scope) => throw new AssertException("Should be lowered");

    public IType GetExpressionType(IDeclarationScope scope) => throw new AssertException("Should be lowered");
}
