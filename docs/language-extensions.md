<!--
SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>

SPDX-License-Identifier: MIT
-->

CLI-Related Language Extensions
===============================

Syntax Extensions
-----------------
Cesium is a programming language targeting CLI, so it provides some extensions to call CLI code.

Here's EBNF of syntax extensions:

```
declaration_specifiers: cli_import_specifier declaration_specifiers
cli_import_specifier: '__cli_import' '(' StringLiteral ')'
type_specifier: __nint
type_specifier: __nuint
```

Any function declaration may be preceded with `__cli_import("Fully.Qualified.Type::Method")` which will mean that this function is to be associated with the corresponding CLI method from a referenced assembly.

Type Extensions
---------------
`__nint` is a synonym for `System.IntPtr` in .NET or `nint` in C#.

`__nint` is a synonym for `System.UIntPtr` in .NET or `nint` in C#.

Macro Extensions
----------------
Cesium provides a built-in macro, `__CESIUM__`, defined to `1`.
