Cesium Type System
==================

This document explains how C types are mapped onto CLI ones in Cesium.

This should be kept in sync with section **6.7.2 Type Specifiers** of the actual C standard.

| C type                                                                      | CLI type        |
|-----------------------------------------------------------------------------|-----------------|
| `void`                                                                      | `System.Void`   |
| `char`                                                                      | `System.Byte`   |
| `signed char`                                                               | `System.SByte`  |
| `unsigned char`                                                             | `System.Byte`   |
| `short`, `signed short`, `short int`, or `signed short int`                 | `System.Int16`  |
| `unsigned short`, or `unsigned short int`                                   | `System.UInt16` |
| `int`, `signed`, or `signed int`                                            | `System.Int32`  |
| `unsigned`, or `unsigned int`                                               | `System.UInt32` |
| `long`, `signed long`, `long int`, or `signed long int`                     | `System.Int64`  |
| `unsigned long`, or `unsigned long int`                                     | `System.UInt64` |
| `long long`, `signed long long`, `long long int`, or `signed long long int` | `System.Int64`  |
| `unsigned long long`, or `unsigned long long int`                           | `System.UInt64` |
| `float`                                                                     | `System.Float`  |
| `double`                                                                    | `System.Double` |
| `long double`                                                               | `System.Double` |
| `_Bool`                                                                     | `System.Bool`   |
| `float _Complex`                                                            | N/A             |
| `double _Complex`                                                           | N/A             |
| `long double _Complex`                                                      | N/A             |

All pointer types are mapped to the CLI pointers of corresponding type.
