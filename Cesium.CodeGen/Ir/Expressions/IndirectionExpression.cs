using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class IndirectionExpression : IExpression, IValueExpression
{
    private readonly IExpression _target;

    public IndirectionExpression(IExpression target)
    {
        _target = target;
    }

    internal IndirectionExpression(Ast.IndirectionExpression expression)
    {
        expression.Deconstruct(out var target);
        _target = target.ToIntermediate();
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        var lowered = new IndirectionExpression(_target.Lower(scope));
        return new GetValueExpression(lowered.Resolve(scope));
    }

    public void EmitTo(IEmitScope scope) => throw new AssertException("Should be lowered");

    public IType GetExpressionType(IDeclarationScope scope) => Resolve(scope).GetValueType();

    public IValue Resolve(IDeclarationScope scope)
    {
        var targetType = _target.GetExpressionType(scope);
        return targetType switch
        {
            InPlaceArrayType arrayType => new LValueIndirection(_target, new PointerType(arrayType.Base)),
            PointerType pointerType => new LValueIndirection(_target, pointerType),
            _ => throw new CompilationException($"Required a pointer or an array type, got {targetType} instead.")
        };
    }
}
