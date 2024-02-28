using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cesium.CodeGen.Ir.BlockItems;
internal sealed class PinvokeDefinition : IBlockItem
{
    internal PinvokeDefinition(string libName, string? prefix)
    {
        LibName = libName;
        Prefix = prefix;
    }

    internal string LibName { get; }

    internal string? Prefix { get; }

    internal bool IsEnd => LibName == "end";
}
