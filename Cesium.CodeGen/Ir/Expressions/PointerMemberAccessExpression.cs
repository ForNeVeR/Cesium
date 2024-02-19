using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class PointerMemberAccessExpression : IValueExpression
{
    private readonly IExpression _target;
    private readonly IdentifierExpression _memberIdentifier;

    public PointerMemberAccessExpression(Ast.PointerMemberAccessExpression accessExpression)
    {
        var (expression, memberAst) = accessExpression;
        _target = expression.ToIntermediate();
        if (memberAst.ToIntermediate() is not IdentifierExpression memberIdentifier)
            throw new CompilationException($"\"{_memberIdentifier}\" is not a valid identifier");
        _memberIdentifier = memberIdentifier;
    }

    internal PointerMemberAccessExpression(IExpression target, IdentifierExpression memberIdentifier)
    {
        _target = target;
        _memberIdentifier = memberIdentifier;
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        var lowered = new PointerMemberAccessExpression(_target.Lower(scope), _memberIdentifier);
        return new GetValueExpression(lowered.Resolve(scope));
    }

    public void EmitTo(IEmitScope scope) => throw new AssertException("Should be lowered");

    public IType GetExpressionType(IDeclarationScope scope) => _target.GetExpressionType(scope);

    public IValue Resolve(IDeclarationScope scope)
    {
        var valueType = _target.GetExpressionType(scope);
        return new LValueInstanceField(_target, (PointerType)valueType, _memberIdentifier.Identifier);
    }
}
