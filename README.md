Cesium [![Status Enfer][status-enfer]][andivionian-status-classifier]
======

Cesium is a fully managed C compiler for CLI platform (.NET).

**What?** Cesium compiles standard C source code to a .NET assembly. No unmanaged/mixed mode (a-lá C++/CLI) code is generated; everything is fully managed (but may be unsafe).

**Why?** C programs are very useful in the modern world and solve practical tasks. At the same time, deploying C code alongside .NET code may be tricky (especially if your application supports multiple platforms). Cesium is designed to resolve the problems of C code deployment, and lift it to the managed state (so it is cross-platform in the same way as the underlying CIL code it is compiled to).

### Sneak Peek

Currently, Cesium is able to compile a "Hello, world" C17 example to a .NET assembly:

```c
#include <stdio.h>

int main(int argc, char *argv[])
{
    puts("Hello, world!");
    return 42;
}
```

The next milestone is [#61: sha1collisiondetection][issue.next-milestone], which is **60%** complete _(note that the progress estimation is preliminary and may be changed in either direction at any moment)_.

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
- `--modulekind <moduleKind>`: specifies the output module kind; by default, it is autodetected from the output file extension
  - `Dll`: gets detected from a `.dll` extension
  - `Console`: gets detected from an `.exe` extension
  - `Windows`: doesn't get detected, so it's only possible to select manually
  - `NetModule`: is a rudiment from Cecil, not supported

Implementation Status
---------------------

### TL;DR: is it ready for use?

Unfortunately, not yet. You won't be able to use Cesium for anything useful today. Probably, you'll be able to start after [the next milestone][issue.next-milestone] is implemented. Stay tuned!

### Dashboard

- [ ] [C17 standard compatibility][issue.c17-standard]: poor
  - [ ] [Preprocessor][issue.preprocessor]: about **10%** of all features are supported
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

If you're interested in certain project areas, check the per-area issue labels:
- [`area:cil-interop`][issues.cil-interop]: issues related to CLI interop
- [`area:compiler`][issues.compiler]: issues related to the Cesium compiler, type checker, and code analyzer
- [`area:parser`][issues.parser]: issues related to C parsing
- [`area:sdk`][issues.sdk]: issues related to the Cesium .NET SDK
- [`area:standard-support`][issues.standard-support]: issues related to C17 standard support
- [`area:stdlib`][issues.stdlib]: issues related to the standard library implementation

Testing
-------

**Want to add new tests to Cesium?** Read a separate [document on tests][docs.tests].

There are two kinds of tests in Cesium: unit tests and integration tests.

Run the unit tests using this shell command:

```console
$ dotnet test
```

Run the integration tests using this shell command ([PowerShell][powershell] is required):

```console
$ pwsh -c ./Cesium.IntegrationTests/Run-Tests.ps1 -NoBuild
```

(don't pass `-NoBuild` if you want to automatically rebuild the compiler before running the integration tests)

If you debug integration tests and want to run just a single test, use this shell command:

```console
pwsh -c ./Cesium.IntegrationTests/Run-Tests.ps1 -TestCaseName quoted_include_fallback.c
```

where `quoted_include_fallback.c` is path within `Cesium.IntegrationTests` folder.

Publishing
----------

For producing standalone compiler executable run

```shell
dotnet publish Cesium.Compiler/Cesium.Compiler.csproj -r win-x64 --self-contained
```

Then navigate to `Cesium.Compiler\bin\Debug\net6.0\win-x64\publish\` and that's your Cesium.

Documentation
-------------

- [C17 Language Standard Draft][c17-draft]

- [Cesium Tests][docs.tests]
- [Cesium Type System][docs.type-system]
- [CLI-Related Language Extensions][docs.language-extensions]
- [Exceptions in the Compiler Code][docs.exceptions]

- [License (MIT)][docs.license]

Code Quality (Experimental)
------------

See [the Sonar dashboard](https://sonarcloud.io/project/overview?id=ForNeVeR_Cesium).

[andivionian-status-classifier]: https://github.com/ForNeVeR/andivionian-status-classifier#status-enfer-
[c17-draft]: http://www.open-std.org/jtc1/sc22/wg14/www/docs/n2310.pdf
[discussions]: https://github.com/ForNeVeR/Cesium/discussions
[docs.exceptions]: docs/exceptions.md
[docs.language-extensions]: docs/language-extensions.md
[docs.license]: LICENSE.md
[docs.tests]: docs/tests.md
[docs.type-system]: docs/type-system.md
[issue.c17-standard]: https://github.com/ForNeVeR/Cesium/issues/62
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
[powershell]: https://github.com/PowerShell/PowerShell
[status-enfer]: https://img.shields.io/badge/status-enfer-orange.svg
[stdlib]: Cesium.Compiler/stdlib
