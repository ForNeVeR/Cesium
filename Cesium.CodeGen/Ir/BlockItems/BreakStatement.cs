using Cesium.CodeGen.Contexts;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class BreakStatement : IBlockItem
{
    public void EmitTo(IEmitScope scope) => throw new AssertException("Break statement should be lowered");
}
