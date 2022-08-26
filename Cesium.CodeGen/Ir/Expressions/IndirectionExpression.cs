using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.Expressions;

internal class IndirectionExpression : IExpression, IValueExpression
{
    private readonly IExpression _target;

    private IndirectionExpression(IExpression target)
    {
        _target = target;
    }

    internal IndirectionExpression(Ast.IndirectionExpression expression)
    {
        expression.Deconstruct(out var target);
        _target = target.ToIntermediate();
    }

    public IExpression Lower() => new IndirectionExpression(_target.Lower());

    public void EmitTo(IDeclarationScope scope) => Resolve(scope).EmitGetValue(scope);

    public IType GetExpressionType(IDeclarationScope scope) => Resolve(scope).GetValueType();

    public IValue Resolve(IDeclarationScope scope)
    {
        var targetType = _target.GetExpressionType(scope);
        if (targetType is not PointerType pointerType)
            throw new CompilationException($"Required a pointer, got {targetType} instead.");

        return new LValueIndirection(_target, pointerType);
    }
}
