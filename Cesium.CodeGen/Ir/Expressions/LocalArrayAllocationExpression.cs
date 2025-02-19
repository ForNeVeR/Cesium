// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions;

internal record LocalAllocationExpression(InPlaceArrayType ArrayType) : IExpression
{
    public IExpression Lower(IDeclarationScope scope) => this;

    public void EmitTo(IEmitScope scope)
    {
        ArrayType.EmitInitializer(scope);
    }

    public IType GetExpressionType(IDeclarationScope scope) => ArrayType;
}
