Architecture Sets
=================

Cesium targets .NET virtual machine, the CLI, which is independent of the target computer architecture in its nature.

At the same time, there are a lot of samples of C code that are machine-dependent. Certain parts of the C standard are impossible to represent in the platform-independent manner.

To manage that, Cesium introduces a concept of the _architecture sets_. An _architecture set_ describes CPU architecture sets that share a common set of memory layout features for a Cesium program. Cesium cannot guarantee that all features of the C standard are supported on every architecture set.

Architecture set influences the following features of compiled programs:
- pointer size,
- size of pointer-dependent memory areas (such as stack arrays and arrays embedded into structures),
- target architecture of the produced .NET assembly (not implemented yet, see issue [#353: Assembly target architecture support](https://github.com/ForNeVeR/Cesium/issues/353)),
- ability to compile certain C constructs.

Cesium aims to support the following architecture sets:
- **32b** (aka `Bit32`): the set of architectures where the pointers are 32-bit-wide. Supports the C standard completely.

  **Example architectures** of this architecture set are x86 and ARM32.
- **64b** (aka `Bit64`): the set of architectures where the pointers are 64-bit-wide. Supports the C standard completely.

  **Example architectures** of this architecture set are x86_64 and ARM64.
- **Dynamic** architecture has no fixed pointer size, and will calculate it in the runtime when required.

  Not every C construct allows to use dynamically-calculated size (in particular, it's impossible for pointer-dependent arrays embedded into structures), so this architecture doesn't support all the C standard. It still should be practical for many applications.

  **This architecture is machine-independent** and results in producing of an Any CPU-targeting assembly.
- **Wide** architecture uses the fixed pointer size of 64 bits on all computers. This allows it to cover all the features of the C standard, for the cost of some redundancy on 32-bit architectures, and slightly different method signatures for .NET interop.

  **This architecture is machine-independent** and results in producing of an Any CPU-targeting assembly.

Specifically for the **wide** architecture, the types `VoidPtr`, `CPtr<T>` and `FuncPtr<TDelegate>` were introduced. They correspond to 64-bit pointers universally, and the **wide** architecture uses it in place of normal pointer types everywhere in the API. For cross-compatibility with any architecture, these types are also used in the Cesium.Runtime library. See [the type system documentation][docs.type-system] for more information.

[docs.type-system]: type-system.md
