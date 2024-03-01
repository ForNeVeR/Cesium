using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class MemberAccessExpression : IExpression, IValueExpression
{
    private readonly IExpression _target;
    private readonly IdentifierExpression _memberIdentifier;

    public MemberAccessExpression(Ast.MemberAccessExpression accessExpression)
    {
        var (expression, memberAst) = accessExpression;
        _target = expression.ToIntermediate();
        if (memberAst.ToIntermediate() is not IdentifierExpression memberIdentifier)
            throw new CompilationException($"\"{_memberIdentifier}\" is not a valid identifier");
        _memberIdentifier = memberIdentifier;
    }

    public IExpression Lower(IDeclarationScope scope)
        => new PointerMemberAccessExpression(
            new UnaryOperatorExpression(UnaryOperator.AddressOf, _target.Lower(scope)).Lower(scope),
            _memberIdentifier).Lower(scope);

    public void EmitTo(IEmitScope scope) => throw new AssertException("Should be lowered");

    public IType GetExpressionType(IDeclarationScope scope) => throw new AssertException("Should be lowered");

    public IValue Resolve(IDeclarationScope scope) => throw new AssertException("Should be lowered");

    internal (IExpression target, string member) Deconstruct() => (_target, _memberIdentifier.Identifier);
}
