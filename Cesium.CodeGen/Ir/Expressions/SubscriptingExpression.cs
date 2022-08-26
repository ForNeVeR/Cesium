using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.Expressions;

internal class SubscriptingExpression : IExpression, IValueExpression
{
    private readonly IExpression _expression;
    private readonly IExpression _index;

    public SubscriptingExpression(Ast.SubscriptingExpression subscriptingExpression)
    {
        var (expression, index) = subscriptingExpression;
        _expression = expression.ToIntermediate();
        _index = index.ToIntermediate();
    }

    private SubscriptingExpression(IExpression expression, IExpression index)
    {
        _expression = expression;
        _index = index;
    }

    public IExpression Lower()
        => new SubscriptingExpression(_expression.Lower(), _index.Lower());

    public void EmitTo(IDeclarationScope scope) => Resolve(scope).EmitGetValue(scope);

    public IType GetExpressionType(IDeclarationScope scope) => Resolve(scope).GetValueType();

    public IValue Resolve(IDeclarationScope scope)
    {
        if (_expression is not IdentifierExpression identifier)
            throw new WipException(230, "Subscription supported only for IdentifierConstantExpression");

        return new LValueArrayElement(identifier.Resolve(scope), _index);
    }
}
