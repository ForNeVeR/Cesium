Cesium Type System
==================

This document explains how C types are mapped onto CLI ones in Cesium.

This should be kept in sync with section **6.7.2 Type Specifiers** of the actual C standard.

| C type                                                                      | CLI type         |
|-----------------------------------------------------------------------------|------------------|
| `void`                                                                      | `System.Void`    |
| `char`                                                                      | `System.Byte`    |
| `signed char`                                                               | `System.SByte`   |
| `unsigned char`                                                             | `System.Byte`    |
| `short`, `signed short`, `short int`, or `signed short int`                 | `System.Int16`   |
| `unsigned short`, or `unsigned short int`                                   | `System.UInt16`  |
| `int`, `signed`, or `signed int`                                            | `System.Int32`   |
| `unsigned`, or `unsigned int`                                               | `System.UInt32`  |
| `long`, `signed long`, `long int`, or `signed long int`                     | `System.Int64`   |
| `unsigned long`, or `unsigned long int`                                     | `System.UInt64`  |
| `long long`, `signed long long`, `long long int`, or `signed long long int` | `System.Int64`   |
| `unsigned long long`, or `unsigned long long int`                           | `System.UInt64`  |
| `float`                                                                     | `System.Float`   |
| `double`                                                                    | `System.Double`  |
| `long double`                                                               | `System.Double`  |
| `_Bool`                                                                     | `System.Bool`    |
| `float _Complex`                                                            | N/A              |
| `double _Complex`                                                           | N/A              |
| `long double _Complex`                                                      | N/A              |
| `__nint`[^1]                                                                | `System.IntPtr`  |
| `__nuint`[^1]                                                               | `System.UIntPtr` |

All the pointer types are mapped to the CLI pointers of the corresponding type on **dynamic**, **32b** and **64b** architecture sets.

The **wide** architecture set supports mapping to raw pointers as well, but supports additional types that have architecture-independent size and memory alignment, according to the following table.

| C type                                                 | CLI Type                            |
|--------------------------------------------------------|-------------------------------------|
| `void*`                                                | `Cesium.Runtime.VoidPtr`            |
| Function pointer                                       | `Cesium.Runtime.FuncPtr<TDelegate>` |
| `T*` (where `T` is not `void` and not a function type) | `Cesium.Runtime.CPtr<T>`            |

Note that function and function pointer signatures (i.e. the arguments and the return types) still use raw pointers even in the **wide** architecture set, because this has no effect on memory requirement and alignment, and thus more type safety is preferred by default.

To be compatible with both **wide** and other architecture sets, the Cesium.Runtime library uses `VoidPtr`, `CPtr<T>` and `FuncPtr<TDelegate>`, where appropriate, in all of its standard APIs.

[^1]: Cesium-specific extensions.
