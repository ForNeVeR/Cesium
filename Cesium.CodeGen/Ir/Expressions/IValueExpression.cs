// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.Values;

namespace Cesium.CodeGen.Ir.Expressions;

internal interface IValueExpression : IExpression
{
    IValue Resolve(IDeclarationScope scope);
}
