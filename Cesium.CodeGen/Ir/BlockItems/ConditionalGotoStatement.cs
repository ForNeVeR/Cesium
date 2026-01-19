// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal record ConditionalGotoStatement(IExpression Condition, ConditionalJumpType JumpType, string Identifier) : IBlockItem
{
}

internal enum ConditionalJumpType
{
    True,
    False,
}

internal record LabeledNopStatement(string Label) : IBlockItem;
