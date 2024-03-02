namespace Cesium.CodeGen.Ir.BlockItems;
internal sealed class PInvokeDefinition : IBlockItem, IPragma
{
    internal PInvokeDefinition(string libName, string? prefix)
    {
        LibName = libName;
        Prefix = prefix;
    }

    /// <summary>
    /// Name of the library from which the function will be imported
    /// </summary>
    internal string LibName { get; }

    /// <summary>
    /// Prefix of the function, from the name of which will be removed
    /// </summary>
    // ex: pinvoke("mscvrt", windows_);
    // int windows_printf(char*, ...);
    // Compiled as:
    // [DllImport("mscvrt", entryPoint: "printf")] int windows_printf(char*, ...);
    internal string? Prefix { get; }

    /// <summary>
    /// Displays that it's #pragma pinvoke(end);
    /// </summary>
    internal bool IsEnd => LibName == "end";
}
