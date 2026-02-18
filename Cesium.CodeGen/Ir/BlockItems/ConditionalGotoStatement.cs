// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal record ConditionalGotoStatement(IExpression Condition, ConditionalJumpType JumpType, string Identifier) : IBlockItem
{
    public ConditionalValue Value { get; init; } = ConstantEvaluator.EvaluateCondition(Condition);

    /// Returns JumpType if it's necessary otherwise returns null
    public ConditionalJumpType? EffectiveJumpType => Value switch
    {
        ConditionalValue.ConstantlyTrue or
            ConditionalValue.ConstantlyFalse => null,
        _ => JumpType
    };
}

internal enum ConditionalJumpType
{
    True,
    False,
}

internal enum ConditionalValue
{
    ConstantlyFalse,
    ConstantlyTrue,
    Unknown
}

internal record LabeledNopStatement(string Label) : IBlockItem;
