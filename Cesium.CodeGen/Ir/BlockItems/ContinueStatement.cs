using Cesium.CodeGen.Contexts;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class ContinueStatement : IBlockItem
{
    public void EmitTo(IEmitScope scope) => throw new AssertException("Continue statement should be lowered");
}
