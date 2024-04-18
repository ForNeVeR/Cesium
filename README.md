Cesium [![Status Enfer][status-enfer]][andivionian-status-classifier]
======

Cesium is a fully managed C compiler for CLI platform (.NET).

**What?** Cesium compiles standard C source code to a .NET assembly. No unmanaged/mixed mode (a-lá C++/CLI) code is generated; everything is fully managed (but may be unsafe).

**Why?** C programs are very useful in the modern world and solve practical tasks. At the same time, deploying C code alongside .NET code may be tricky (especially if your application supports multiple platforms). Cesium is designed to resolve the problems of C code deployment, and lift it to the managed state (so it is cross-platform in the same way as the underlying CIL code it is compiled to).

Implementation Status
---------------------

### TL;DR: is it ready for use?

Unfortunately, not yet. You won't be able to use Cesium for anything useful today. Probably, you'll be able to start after [the next milestone][issue.next-milestone] is implemented. Stay tuned!

### Sneak Peek

Currently, Cesium is able to compile a "Hello, world" C23 example to a .NET assembly:

```c
#include <stdio.h>

int main(int argc, char *argv[])
{
    puts("Hello, world!");
    return 42;
}
```

The next milestone is [#61: sha1collisiondetection][issue.next-milestone], which is **80%** complete _(note that the progress estimation is preliminary and may be changed in either direction at any moment)_.

Documentation
-------------

- [C23 Language Standard Draft][c23-draft]

- [Contributor Guide][docs.contributing]
- [Cesium Tests][docs.tests]
- [Cesium Type System][docs.type-system]
- [Cesium SDK][docs.msbuild-sdk]
- [Architecture Sets][docs.architecture-sets]
- [CLI-Related Language Extensions][docs.language-extensions]
- [Built-in Functions][docs.builtins]
- [Exceptions in the Compiler Code][docs.exceptions]
- [Design Notes][docs.design-notes]

- [License (MIT)][docs.license]

Usage
-----

```console
$ dotnet run --project Cesium.Compiler -- <path to the input .c file> --out <path to the output assembly>
```

For example, this will generate an assembly executable by .NET 6, .NET Framework, or Mono:

```console
$ dotnet run --project Cesium.Compiler -- Cesium.Samples/minimal.c --out out.exe
$ dotnet ./out.exe # run with .NET 6
$ ./out.exe # only on Windows, run with .NET Framework
$ mono ./out.exe # run with Mono
```

### Optional Parameters

- `--framework <framework>`: specifies the target framework, defaults to `Net`
  - `NetFramework` for .NET Framework
  - `NetStandard` for .NET Standard
  - `Net` for .NET 5+
- `--arch <architecture-set>`: specifies the [target architecture set][docs.architecture-sets], defaults to `Dynamic`. Possible values are:
  - `Dynamic` (machine-independent, calculates pointer size and structure layout in runtime),
  - `Bit32` (for 32-bit architectures),
  - `Bit64` (for 64-bit architectures),
  - `Wide` (machine-independent, uses 64-bit pointers even on 32-bit architectures).
- `--modulekind <moduleKind>`: specifies the output module kind; by default, it is autodetected from the output file extension
  - `Dll`: gets detected from a `.dll` extension
  - `Console`: gets detected from an `.exe` extension
  - `Windows`: doesn't get detected, so it's only possible to select manually
  - `NetModule`: is a rudiment from Cecil, not supported

Implementation Dashboard
------------------------

- [ ] [C23 standard compatibility][issue.c23-standard]: poor
    - [ ] [Preprocessor][issue.preprocessor]: about **30%** ready
    - [ ] [Lexer][issue.lexer]: mostly works, but needs more tests and validation on its compliance
    - [ ] [Parser][issue.parser]: supports about **25%** of the language syntax
- [ ] **Compiler**
    - [x] CIL code generator: basics are ready, new features get added
    - [ ] [PDB support][issue.pdb]: none
- [ ] [Standard library][stdlib]: a very few functions are supported
- [ ] [.NET SDK][issue.sdk]: none (but planned!)

**Have a question?** Welcome to [the discussions section][discussions]!

**Looking to contribute?** Check [open issues with the "help-wanted" label][issues.help-wanted]. Cesium is a big project which lives thanks to its contributors.

**Not sure where to contribute?** Check [open issues with the "good first issue" label][issues.good-first-issue].

Take a look at [the contributor guide][docs.contributing].

If you're interested in certain project areas, check the per-area issue labels:
- [`area:cil-interop`][issues.cil-interop]: issues related to CLI interop
- [`area:compiler`][issues.compiler]: issues related to the Cesium compiler, type checker, and code analyzer
- [`area:parser`][issues.parser]: issues related to C parsing
- [`area:sdk`][issues.sdk]: issues related to the Cesium .NET SDK
- [`area:standard-support`][issues.standard-support]: issues related to C23 standard support
- [`area:stdlib`][issues.stdlib]: issues related to the standard library implementation

[andivionian-status-classifier]: https://github.com/ForNeVeR/andivionian-status-classifier#status-enfer-
[c23-draft]: https://www.open-std.org/jtc1/sc22/wg14/www/docs/n3096.pdf
[discussions]: https://github.com/ForNeVeR/Cesium/discussions
[docs.architecture-sets]: docs/architecture-sets.md
[docs.builtins]: docs/builtins.md
[docs.contributing]: CONTRIBUTING.md
[docs.design-notes]: docs/design-notes.md
[docs.msbuild-sdk]: docs/msbuild-sdk.md
[docs.exceptions]: docs/exceptions.md
[docs.language-extensions]: docs/language-extensions.md
[docs.license]: LICENSE.md
[docs.tests]: docs/tests.md
[docs.type-system]: docs/type-system.md
[issue.c23-standard]: https://github.com/ForNeVeR/Cesium/issues/62
[issue.lexer]: https://github.com/ForNeVeR/Cesium/issues/76
[issue.next-milestone]: https://github.com/ForNeVeR/Cesium/issues/61
[issue.parser]: https://github.com/ForNeVeR/Cesium/issues/78
[issue.pdb]: https://github.com/ForNeVeR/Cesium/issues/79
[issue.preprocessor]: https://github.com/ForNeVeR/Cesium/issues/77
[issue.sdk]: https://github.com/ForNeVeR/Cesium/issues/80
[issues.cil-interop]: https://github.com/ForNeVeR/Cesium/labels/area%3Acil-interop
[issues.compiler]: https://github.com/ForNeVeR/Cesium/labels/area%3Acompiler
[issues.good-first-issue]: https://github.com/ForNeVeR/Cesium/labels/good-first-issue
[issues.help-wanted]: https://github.com/ForNeVeR/Cesium/labels/status%3Ahelp-wanted
[issues.parser]: https://github.com/ForNeVeR/Cesium/labels/area%3Aparser
[issues.preprocessor]: https://github.com/ForNeVeR/Cesium/labels/area%3Apreprocessor
[issues.sdk]: https://github.com/ForNeVeR/Cesium/labels/area%3Asdk
[issues.standard-support]: https://github.com/ForNeVeR/Cesium/labels/area%3Astandard-support
[issues.stdlib]: https://github.com/ForNeVeR/Cesium/labels/area%3Astdlib
[status-enfer]: https://img.shields.io/badge/status-enfer-orange.svg
[stdlib]: Cesium.Compiler/stdlib
