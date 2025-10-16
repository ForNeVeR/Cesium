// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Contexts;

internal record VariableInfo(StorageClass StorageClass, IType Type, IExpression? Constant)
{
    static int CurrentIndex = 0;
    public int Index { get; } = checked(CurrentIndex++);
    public string? EmitName { get; set; }
}
