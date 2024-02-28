using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cesium.CodeGen.Ir.BlockItems;
internal sealed class PinvokeDefinition : IBlockItem
{
    internal PinvokeDefinition(string libName)
    {
        LibName = libName;
    }

    internal string LibName { get; }

    internal bool IsEnd => LibName == "end";
}
