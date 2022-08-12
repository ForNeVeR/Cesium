using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Expressions;

internal class MemberAccessExpression : IExpression, ILValueExpression
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

    public IExpression Lower()
        => new PointerMemberAccessExpression(
            new UnaryOperatorExpression(UnaryOperator.AddressOf, _target.Lower()),
            _memberIdentifier.Lower());

    public void EmitTo(IDeclarationScope scope) => throw new NotSupportedException("Should be lowered");

    public TypeReference GetExpressionType(IDeclarationScope scope) => throw new NotSupportedException("Should be lowered");

    public IValue Resolve(IDeclarationScope scope) => throw new NotSupportedException("Should be lowered");
}
