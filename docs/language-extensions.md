CLI-Related Language Extensions
===============================

Cesium is a programming language targeting CLI, so it provides some extensions to call CLI code.

Here's EBNF of syntax extensions:

```
declaration_specifiers: cli_import_specifier declaration_specifiers
cli_import_specifier: '__cli_import' '(' StringLiteral ')'
```

Any function declaration may be preceded with `__cli_import("Fully.Qualified.Type::Method")` which will mean that this function is to be associated with the corresponding CLI method from a referenced assembly.
