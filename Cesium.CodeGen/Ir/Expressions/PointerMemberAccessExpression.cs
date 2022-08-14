using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Values;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Expressions;

internal class PointerMemberAccessExpression : IExpression, IValueExpression
{
    private readonly IExpression _target;
    private readonly IExpression _memberIdentifier;

    public PointerMemberAccessExpression(Ast.PointerMemberAccessExpression accessExpression)
    {
        var (expression, memberIdentifier) = accessExpression;
        _target = expression.ToIntermediate();
        _memberIdentifier = memberIdentifier.ToIntermediate();
    }

    internal PointerMemberAccessExpression(IExpression target, IExpression memberIdentifier)
    {
        _target = target;
        _memberIdentifier = memberIdentifier;
    }

    public IExpression Lower()
        => new PointerMemberAccessExpression(_target.Lower(), _memberIdentifier.Lower());

    public void EmitTo(IDeclarationScope scope) => Resolve(scope).EmitGetValue(scope);

    public TypeReference GetExpressionType(IDeclarationScope scope) => Resolve(scope).GetValueType();

    public IValue Resolve(IDeclarationScope scope)
    {
        if (_memberIdentifier is not IdentifierExpression memberIdentifier)
            throw new NotSupportedException($"\"{_memberIdentifier}\" is not a valid identifier");

        var valueType = _target.GetExpressionType(scope);
        var valueTypeDef = valueType.Resolve();

        var field = valueTypeDef.Fields.FirstOrDefault(f => f?.Name == memberIdentifier.Identifier)
                    ?? throw new CesiumCompilationException(
                        $"\"{valueTypeDef.Name}\" has no member named \"{memberIdentifier.Identifier}\"");
        return new LValueField(_target, new FieldReference(field.Name, field.FieldType, field.DeclaringType));
    }
}
