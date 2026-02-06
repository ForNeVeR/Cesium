<!--
SPDX-FileCopyrightText: 2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>

SPDX-License-Identifier: MIT
-->

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

Installation
------------
Cesium consists of different components, which could be installed separately.

### Project Templates
Install the templates:
```console
$ dotnet new install Cesium.Templates
```

Then use:
```
$ dotnet new cesiumapp
$ dotnet new cesiumlib
```

The `cesiumapp` template will create a new application (executable), `cesiumlib` will create a library (a non-executable assembly).

Both templates integrate with MSBuild and are buildable using `dotnet build`. This is possible thanks to the [Cesium SDK][docs.msbuild-sdk]. Speaking of which…

### Cesium SDK
To start working on a new project using Cesium.SDK, write a `.ceproj` file:
```xml
<Project Sdk="Cesium.Sdk/[version]">
    <PropertyGroup>
        <TargetFramework>[tfm]</TargetFramework>
        <OutputType>Exe</OutputType>
    </PropertyGroup>
</Project>
```

Replace `[version]` with the Cesium SDK version you want to use, and `[tfm]` with the target framework of your program (e.g. `net6.0`).

And add a `program.c` file (or any other `.c` and `.h` files) to the same folder.

This is essentially the same as what's contained in the corresponding project template.

Then, build your project via
```console
$ dotnet build
```

And run it via
```console
$ dotnet run
```

### Compiler
If you want to install the compiler separately from the SDK, you have two options:
1. Install the compiler as a [.NET global tool][dotnet.tools]:

   ```console
   $ dotnet tool install --global Cesium.Compiler
   $ Cesium.Compiler --help
   ```

   (See the arguments and how to run the compiler below.)

2. Install a self-contained version of Cesium for your platform: download the corresponding `Cesium.Compiler.Bundle.<platform>.zip` from the [releases] page. Unpack and then use the `Cesium.Compiler(.exe)` executable file.

   These platform-specific bundles provide [self-contained executables][dotnet.self-contained] for all the supported platforms, so they don't have a dependency on .NET runtime installed in the target environment.

Packages
--------

| Package                    | Link                                                                                    |
|----------------------------|-----------------------------------------------------------------------------------------|
| **Cesium.Compiler.Bundle** | [![Cesium.Compiler.Bundle][badge.cesium.compiler.bundle]][nuget.cesium.compiler.bundle] |
| **Cesium.Compiler**        | [![Cesium.Compiler][badge.cesium.compiler]][nuget.cesium.compiler]                      |
| **Cesium.Runtime**         | [![Cesium.Runtime][badge.cesium.runtime]][nuget.cesium.runtime]                         |
| **Cesium.Sdk**             | [![Cesium.Sdk][badge.cesium.sdk]][nuget.cesium.sdk]                                     |
| **Cesium.Templates**       | [![Cesium.Templates][badge.cesium.templates]][nuget.cesium.templates]                   |

Compiler Usage
--------------
```console
$ Cesium.Compiler <list of the input files> --out <path to the output assembly> [optional parameters go here]
```

For run from sources, use `dotnet run --project Cesium.Compiler` in the repository root directory, followed by `--` and the same arguments.

For example, this will generate an assembly executable by .NET 5+, .NET Framework, or Mono:

```console
$ dotnet run --project Cesium.Compiler -- Cesium.Samples/minimal.c --out out.exe
$ dotnet ./out.exe # run with .NET 5+
$ ./out.exe # only on Windows, run with .NET Framework
$ mono ./out.exe # run with Mono
```

As inputs, Cesium accepts its own internal `.obj` file format produced by the `-c` option (see below), or standard C source files (normally kept with `.c` file extension, but anything else than `.obj` will be treated as C anyway).

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
- `-c`: will produce a JSON-based object file imitation in the output file. This mode is supposed to be used when using Cesium compiler as a C compiler for an existing toolset.

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

Documentation
-------------

- [C23 Language Standard Draft][c23-draft]

- [Changelog][docs.changelog]
- [Contributor Guide][docs.contributing]
- [Cesium Tests][docs.tests]
- [Cesium Type System][docs.type-system]
- [Cesium SDK][docs.msbuild-sdk]
- [Architecture Sets][docs.architecture-sets]
- [CLI-Related Language Extensions][docs.language-extensions]
- [Built-in Functions][docs.builtins]
- [Exceptions in the Compiler Code][docs.exceptions]
- [Design Notes][docs.design-notes]
- [Maintainer Guide][docs.maintaining]

License
-------
The project's sources, except the project templates, are distributed under the terms of [the MIT license][docs.license.mit].

The project templates (from the directory `Cesium.Templates`) are distributed under the terms of [the CC-0 license][docs.license.cc0].

The license indication in the project's sources is compliant with the [REUSE specification v3.3][reuse.spec].

[andivionian-status-classifier]: https://andivionian.fornever.me/v1/#status-enfer-
[badge.cesium.compiler.bundle]: https://img.shields.io/nuget/v/Cesium.Compiler.Bundle
[badge.cesium.compiler]: https://img.shields.io/nuget/v/Cesium.Compiler
[badge.cesium.runtime]: https://img.shields.io/nuget/v/Cesium.Runtime
[badge.cesium.sdk]: https://img.shields.io/nuget/v/Cesium.Sdk
[badge.cesium.templates]: https://img.shields.io/nuget/v/Cesium.Templates
[c23-draft]: https://www.open-std.org/jtc1/sc22/wg14/www/docs/n3096.pdf
[discussions]: https://github.com/ForNeVeR/Cesium/discussions
[docs.architecture-sets]: docs/architecture-sets.md
[docs.builtins]: docs/builtins.md
[docs.changelog]: CHANGELOG.md
[docs.contributing]: CONTRIBUTING.md
[docs.design-notes]: docs/design-notes.md
[docs.exceptions]: docs/exceptions.md
[docs.language-extensions]: docs/language-extensions.md
[docs.license.cc0]: LICENSES/CC0-1.0.txt
[docs.license.mit]: LICENSE.md
[docs.maintaining]: MAINTAINING.md
[docs.msbuild-sdk]: docs/msbuild-sdk.md
[docs.tests]: docs/tests.md
[docs.type-system]: docs/type-system.md
[dotnet.self-contained]: https://learn.microsoft.com/en-us/dotnet/core/deploying/
[dotnet.tools]: https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools
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
[nuget.cesium.compiler.bundle]: https://www.nuget.org/packages/Cesium.Compiler.Bundle
[nuget.cesium.compiler]: https://www.nuget.org/packages/Cesium.Compiler
[nuget.cesium.runtime]: https://www.nuget.org/packages/Cesium.Runtime
[nuget.cesium.sdk]: https://www.nuget.org/packages/Cesium.Sdk
[nuget.cesium.templates]: https://www.nuget.org/packages/Cesium.Templates
[releases]: https://github.com/ForNeVeR/Cesium/releases
[reuse.spec]: https://reuse.software/spec-3.3/
[status-enfer]: https://img.shields.io/badge/status-enfer-orange.svg
[stdlib]: Cesium.Compiler/stdlib
