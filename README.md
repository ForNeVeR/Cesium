Cesium [![Status Enfer][status-enfer]][andivionian-status-classifier]
======

Cesium is a fully managed C compiler for CLI platform (.NET).

**What?** Cesium compiles standard C source code to a .NET assembly. No unmanaged/mixed mode (a-lá C++/CLI) code is generated; everything is fully managed (but may be unsafe).

**Why?** C programs are very useful in the modern world, and solve practical tasks. At the same time, deploying C code alongside .NET code may be tricky (especially if your application supports multiple platforms). Cesium is designed to resolve the problems of C code deployment, and lift it to the managed state (so it is cross-platform in the same way the underlying CIL code it is compiled to).

Implementation Status
---------------------

### TL;DR: is it ready for use?

Unfortunately, not yet. You won't be able to use Cesium for anything useful today. Probably, you'll be able to start after [the next milestone][issue.next-milestone] is implemented. Stay tuned!

### Dashboard

- [ ] [C17 standard compatibility][issue.c17-standard]: poor
  - [ ] [Preprocessor][issue.preprocessor]: only `#include` is supported
  - [ ] [Lexer][issue.lexer]: mostly works, but needs more tests and validation on its compliance
  - [ ] [Parser][issue.parser]: supports about 20% of the language syntax
- [ ] **Compiler**
  - [x] CIL code generator: basics are ready, new features get added
  - [ ] [PDB support][issue.pdb]: none
- [ ] [Standard library][stdlib]: a very few functions are supported
- [ ] [.NET SDK][issue.sdk]: none

**Have a question?** Welcome to [the discussions section][discussions]!

**Looking to contribute?** Check [open issues with the "help wanted" label][issues.help-wanted]. Cesium is a big project which lives thanks to its contributors.

If you're interested in certain project area, check the per-area issue labels:
- [`area:cil-interop`][issues.cil-interop]: issues related to CLI interop
- [`area:compiler`][issues.compiler]: issues related to the Cesium compiler, type checker, and code analyzer
- [`area:parser`][issues.parser]: issues related to C parsing
- [`area:standard-support`][issues.standard-support]: issues related to C17 standard support
- [`area:sdk`][issues.sdk]: issues related to the Cesium .NET SDK

### Sneak peek

Currently, Cesium is able to compile a "Hello, world" C17 example to a .NET assembly:

```c
#include <stdio.h>

int main(int argc, char *argv[])
{
    puts("Hello, world!");
    return 42;
}
```

The next milestone is [#61: sha1collisiondetection][issue.next-milestone], which is **10%** complete.

Documentation
-------------

- [C17 Language Standard Draft][c17-draft]


- [Cesium Type System][docs.type-system]
- [CLI-Related Language Extensions][docs.language-extensions]


- [License (MIT)][docs.license]

Usage
-----

```console
$ dotnet run --project Cesium.Compiler -- <path to the input .c file> --out <path to the output assembly>
```

For example, this will generate an assembly executable by .NET 6, .NET Framework, or Mono:

```console
$ dotnet run --project Cesium.Compiler -- Cesium.Samples/minimal.c out.exe
$ dotnet ./out.exe # run with .NET 6
$ ./out.exe # only on Windows, run with .NET Framework
$ mono ./out.exe # run with Mono
```

## Testing

In order to test changes please run following for fast-cycle testing

```console
$ dotnet test
```

And if you want to run integration tests

```console
$ pwsh -c ./Cesium.IntegrationTests/Run-Tests.ps1 -NoBuild
```

(don't pass `-NoBuild` if you want to automatically rebuild the compiler before running the integration tests)

If you debug integration tests and want to run just single test

```console
pwsh -c ./Cesium.IntegrationTests/Run-Tests.ps1 -TestCaseName quoted_include_fallback.c
```

where `quoted_include_fallback.c` is path within `Cesium.IntegrationTests` folder.

[andivionian-status-classifier]: https://github.com/ForNeVeR/andivionian-status-classifier#status-enfer-
[c17-draft]: http://www.open-std.org/jtc1/sc22/wg14/www/docs/n2310.pdf
[discussions]: https://github.com/ForNeVeR/Cesium/discussions
[docs.language-extensions]: docs/language-extensions.md
[docs.license]: LICENSE.md
[docs.type-system]: docs/type-system.md
[issue.c17-standard]: https://github.com/ForNeVeR/Cesium/issues/62
[issue.lexer]: https://github.com/ForNeVeR/Cesium/issues/76
[issue.next-milestone]: https://github.com/ForNeVeR/Cesium/issues/61
[issue.parser]: https://github.com/ForNeVeR/Cesium/issues/78
[issue.pdb]: https://github.com/ForNeVeR/Cesium/issues/79
[issue.preprocessor]: https://github.com/ForNeVeR/Cesium/issues/77
[issue.sdk]: https://github.com/ForNeVeR/Cesium/issues/80
[issues.cil-interop]: https://github.com/ForNeVeR/Cesium/issues?q=is%3Aissue+is%3Aopen+label%3Aarea%3Acil-interop
[issues.compiler]: https://github.com/ForNeVeR/Cesium/labels/area%3Acompiler
[issues.help-wanted]: https://github.com/ForNeVeR/Cesium/labels/status%3Ahelp-wanted
[issues.parser]: https://github.com/ForNeVeR/Cesium/labels/area%3Aparser
[issues.preprocessor]: https://github.com/ForNeVeR/Cesium/labels/area%3Apreprocessor
[issues.sdk]: https://github.com/ForNeVeR/Cesium/labels/area%3Asdk
[issues.standard-support]: https://github.com/ForNeVeR/Cesium/labels/area%3Astandard-support
[status-enfer]: https://img.shields.io/badge/status-enfer-orange.svg
[stdlib]: Cesium.Compiler/stdlib
