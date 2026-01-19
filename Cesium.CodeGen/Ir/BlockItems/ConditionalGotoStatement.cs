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
