using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.LValues;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Expressions;

internal class PointerMemberAccessExpression : IExpression, ILValueExpression
{
    private readonly IExpression _expression;
    private readonly IExpression _memberIdentifier;

    public PointerMemberAccessExpression(Ast.PointerMemberAccessExpression accessExpression)
    {
        var (expression, memberIdentifier) = accessExpression;
        _expression = expression.ToIntermediate();
        _memberIdentifier = memberIdentifier.ToIntermediate();
    }

    private PointerMemberAccessExpression(IExpression expression, IExpression memberIdentifier)
    {
        _expression = expression;
        _memberIdentifier = memberIdentifier;
    }

    public IExpression Lower()
        => new PointerMemberAccessExpression(_expression.Lower(), _memberIdentifier.Lower());

    public void EmitTo(IDeclarationScope scope) => Resolve(scope).EmitGetValue(scope);

    public ILValue Resolve(IDeclarationScope scope)
    {
        if (_expression is not ILValueExpression expression)
            throw new NotSupportedException("Pointer member access supported only for lvalues");

        if (_memberIdentifier is not IdentifierExpression memberIdentifier)
            throw new NotSupportedException($"\"{_memberIdentifier}\" is not a valid identifier");

        var lvalue = expression.Resolve(scope);
        var valueType = lvalue.GetValueType();
        var valueTypeDef = valueType.Resolve();

        try
        {
            var field = valueTypeDef.Fields.First(f => f?.Name == memberIdentifier.Identifier);
            return new LValueField(lvalue, new FieldReference(field.Name, field.FieldType, field.DeclaringType));
        }
        catch (InvalidOperationException _)
        {
            throw new NotSupportedException($"Unable to find field \"{memberIdentifier.Identifier}\" for type \"{valueTypeDef.Name}\"");
        }
    }
}
