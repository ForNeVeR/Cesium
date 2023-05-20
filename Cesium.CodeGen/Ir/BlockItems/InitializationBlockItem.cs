using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal record InitializerPart(IExpression? PrimaryInitializer, IExpression? SecondaryInitializer);

internal record InitializationBlockItem(ICollection<InitializerPart> Items) : IBlockItem
{
    public IBlockItem Lower(IDeclarationScope scope) => this; // already lowered

    public void EmitTo(IEmitScope scope)
    {
        foreach (var (primInt, secInt) in Items)
        {
            primInt?.EmitTo(scope);
            secInt?.EmitTo(scope);
        }
    }
}
