// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class IndirectionExpression : IExpression, IValueExpression
{
    internal IExpression Target { get; }

    public IndirectionExpression(IExpression target)
    {
        Target = target;
    }

    internal IndirectionExpression(Ast.IndirectionExpression expression, IDeclarationScope scope)
    {
        expression.Deconstruct(out var target);
        Target = target.ToIntermediate(scope);
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        var lowered = new IndirectionExpression(Target.Lower(scope));
        return new GetValueExpression(lowered.Resolve(scope));
    }

    public void EmitTo(IEmitScope scope) => throw new AssertException("Should be lowered");

    public IType GetExpressionType(IDeclarationScope scope) => Resolve(scope).GetValueType();

    public IValue Resolve(IDeclarationScope scope)
    {
        var targetType = Target.GetExpressionType(scope);
        return targetType switch
        {
            InPlaceArrayType arrayType => new LValueIndirection(Target, new PointerType(arrayType.Base)),
            PointerType pointerType => new LValueIndirection(Target, pointerType),
            _ => throw new CompilationException($"Required a pointer or an array type, got {targetType} instead.")
        };
    }
}
