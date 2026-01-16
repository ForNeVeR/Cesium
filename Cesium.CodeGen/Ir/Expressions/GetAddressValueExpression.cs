// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class GetAddressValueExpression : IExpression
{
    internal IAddressableValue Value { get; }

    public GetAddressValueExpression(IAddressableValue value)
    {
        Value = value;
    }

    public IExpression Lower(IDeclarationScope scope) => this;

    public void EmitTo(IEmitScope scope)
    {
        Value.EmitGetAddress(scope);
    }

    public IType GetExpressionType(IDeclarationScope scope)
    {
        IType valueType = Value.GetValueType();
        return GetBasePointer(valueType);
    }

    private static IType GetBasePointer(IType valueType)
    {
        return valueType.MakePointerType();
    }
}
