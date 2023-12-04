using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal record InitializerPart(IExpression? PrimaryInitializer, IExpression? SecondaryInitializer);

internal record InitializationBlockItem(ICollection<InitializerPart> Items) : IBlockItem
{
}
