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
    /// <para>
    ///     Prefix of the function's name that will be removed to construct its P/Invoke name.
    /// </para>
    /// <para>
    ///     For example, consider this definition:
    ///     <code>
    ///         pinvoke("mscvrt", windows_);
    ///         int windows_printf(char*, ...);
    ///     </code>
    ///     This will be compiled as:
    ///     <code>
    ///         [DllImport("mscvrt", entryPoint: "printf")]
    ///         int windows_printf(char*, ...);
    ///     </code>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    ///     This is useful in cases when we need several C entry points for similarly-name OS function in portable code.
    ///     For example, system function <c>puts</c> is named the same on all operating systems (included in different
    ///     system libraries, though). However, a Cesium program may need to distinguish between function
    ///     implementations for different OSes in runtime, and thus see different names for them.
    /// </para>
    /// <para>
    ///     For an example of using this construct, see the <c>pinvoke.c</c> file in the integration test directory.
    /// </para>
    /// </remarks>
    internal string? Prefix { get; }

    /// <summary>
    /// Displays that it's #pragma pinvoke(end);
    /// </summary>
    internal bool IsEnd => LibName == "end";
}
