using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
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

    public IExpression Lower(IDeclarationScope scope)
        => new PointerMemberAccessExpression(_target.Lower(scope), _memberIdentifier.Lower(scope));

    public void EmitTo(IEmitScope scope) => Resolve(scope).EmitGetValue(scope);

    public IType GetExpressionType(IDeclarationScope scope) => _target.GetExpressionType(scope);

    public IValue Resolve(IDeclarationScope scope)
    {
        if (_memberIdentifier is not IdentifierExpression memberIdentifier)
            throw new CompilationException($"\"{_memberIdentifier}\" is not a valid identifier");

        var valueType = _target.GetExpressionType(scope);
        return new LValueField(_target, valueType, memberIdentifier.Identifier);
    }
}
