// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class SizeOfOperatorExpression : IExpression
{
    internal IType Type { get; }

    public SizeOfOperatorExpression(IType Type)
    {
        this.Type = Type;
    }

    public IExpression Lower(IDeclarationScope scope) => Type switch
    {
        InPlaceArrayType arrayType => arrayType.GetSizeInBytesExpression(scope.ArchitectureSet),
        StructType structType => this,
        _ => this
    };

    public void EmitTo(IEmitScope scope)
    {
        var context = scope.Context;

        // As sizeof is a type operator, it may need to emit anonymous types right here.
        if (Type is IGeneratedType generatedType && !generatedType.IsAlreadyEmitted(context))
        {
            generatedType.EmitType(context);
        }

        var type = Type.Resolve(context);
        scope.SizeOf(type);
    }

    public IType GetExpressionType(IDeclarationScope scope) => CTypeSystem.UnsignedInt;
}
