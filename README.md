Cesium [![Status Umbra][status-umbra]][andivionian-status-classifier]
======

Cesium is a fully managed C compiler for CLI platform (.NET).

**What?** Cesium compiles standard C source code to a .NET assembly. No unmanaged/mixed mode (a-lá C++/CLI) code is expected to be generated; everything is fully managed (but unsafe).

**Why?** C programs are very useful in the modern world, and solve practical tasks. At the same time, deploying C code alongside .NET code may be tricky (especially if your application supports multiple platforms). Cesium is designed to resolve the problems of C code deployment, and lift it to the managed state (so it is crossplatform in the same way the underlying CIL code it is compiled to).

Implementation Status
---------------------

- [ ] C17 parser: _just started_
- [ ] CIL code generator: _just started_
- [ ] PDB support: _none_
- [ ] C preprocessor: _none_
- [ ] .NET SDK: _none_
- [ ] Standard library: _none_

Documentation
-------------

- [C17 Language Standard Draft][c17-draft]
- [CLI-Related Language Extensions][docs.language-extensions]

Usage
-----

```console
$ dotnet run --project Cesium.Compiler -- <path to the input .c file> <path to the output assembly>
```

For example, this will generate an assembly executable by .NET 6, .NET Framework, or Mono:

```console
$ dotnet run --project Cesium.Compiler -- Cesium.Samples/minimal.c out.exe
$ dotnet ./out.exe # run with .NET 6
$ ./out.exe # only on Windows, run with .NET Framework
$ mono ./out.exe # run with Mono
```

[andivionian-status-classifier]: https://github.com/ForNeVeR/andivionian-status-classifier#status-umbra-
[c17-draft]: http://www.open-std.org/jtc1/sc22/wg14/www/docs/n2310.pdf
[status-umbra]: https://img.shields.io/badge/status-umbra-red.svg
[docs.language-extensions]: docs/language-extensions.md
