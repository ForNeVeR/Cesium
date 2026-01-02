// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class GetValueExpression : IValueExpression
{
    internal IValue Value { get; }

    public GetValueExpression(IValue value)
    {
        Value = value;
    }

    public IExpression Lower(IDeclarationScope scope) => this;

    public void EmitTo(IEmitScope scope) => Value.EmitGetValue(scope);

    public IType GetExpressionType(IDeclarationScope scope) => Value.GetValueType();

    public IValue Resolve(IDeclarationScope scope) => Value;
}
