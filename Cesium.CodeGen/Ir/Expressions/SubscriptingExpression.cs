using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.LValues;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Expressions;

internal class SubscriptingExpression : IExpression, ILValueExpression
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

    public TypeReference GetExpressionType(IDeclarationScope scope) => Resolve(scope).GetValueType();

    public ILValue Resolve(IDeclarationScope scope)
    {
        switch (_expression) {
            case IdentifierExpression identifier:
                return new LValueArrayElement(identifier.Resolve(scope), _index);
            case PointerMemberAccessExpression pointerMemberAccess:
                return new LValueArrayElement(pointerMemberAccess.Resolve(scope), _index);
            default:
                throw new NotImplementedException($"Subscription supported only for IdentifierConstantExpression, but {_expression.GetType().Name} seen.");
        }
    }
}
