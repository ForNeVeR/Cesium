using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.Expressions;

internal class MemberAccessExpression : IExpression, IValueExpression
{
    private readonly IExpression _target;
    private readonly IExpression _memberIdentifier;

    public MemberAccessExpression(Ast.MemberAccessExpression accessExpression)
    {
        var (expression, memberIdentifier) = accessExpression;
        _target = expression.ToIntermediate();
        _memberIdentifier = memberIdentifier.ToIntermediate();
    }

    private MemberAccessExpression(IExpression target, IExpression memberIdentifier)
    {
        _target = target;
        _memberIdentifier = memberIdentifier;
    }

    public IExpression Lower(IDeclarationScope scope)
        => new PointerMemberAccessExpression(
            new UnaryOperatorExpression(UnaryOperator.AddressOf, _target.Lower(scope)),
            _memberIdentifier.Lower(scope));

    public void EmitTo(IEmitScope scope) => throw new AssertException("Should be lowered");

    public IType GetExpressionType(IDeclarationScope scope) => throw new AssertException("Should be lowered");

    public IValue Resolve(IDeclarationScope scope) => throw new AssertException("Should be lowered");
}
