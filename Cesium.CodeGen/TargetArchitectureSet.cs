namespace Cesium.CodeGen;

/// <summary>Describes the set of system architectures targeted by the assembly.</summary>
public enum TargetArchitectureSet
{
    /// <summary>
    /// <para>Dynamic architecture.</para>
    /// <para>
    ///     May not support every feature of the C programming language, but compiles to AnyCPU assembly. Performs some
    ///     calculations, such as pointer array size calculations, in runtime.
    /// </para>
    /// </summary>
    Dynamic,

    /// <summary>An architecture with 32-bit pointers. Targets ARM32 and x86 CPUs.</summary>
    Bit32,

    /// <summary>An architecture with 64-bit pointers. Targets ARM64 and x86-64 CPUs.</summary>
    Bit64,

    /// <summary>An architecture with 64-bit pointers (even on 32-bit runtime). Targets any CPUs.</summary>
    Wide
}
