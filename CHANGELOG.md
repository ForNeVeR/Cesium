<!--
SPDX-FileCopyrightText: 2024-2026 Friedrich von Never <friedrich@fornever.me>

SPDX-License-Identifier: MIT
-->

Changelog
=========
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.1] - 2026-03-29
### Fixed
- [#975: Struct layout should be sequential](https://github.com/ForNeVeR/Cesium/issues/975).

## [0.4.0] - 2026-03-28
### Changed
- All the Cesium-generated struct types are now `public`.

### Fixed
- Issues with "Size of field of type \<name> is too large" when compiled with the **wide** architecture set.
- Issues with resolving `System.Private.CoreLib` when referencing Cesium-produced assemblies from external code.

## [0.3.0] - 2026-03-09
### Added
- [#944](https://github.com/ForNeVeR/Cesium/pull/944): Implement string comparison functions from POSIX. Thanks to @kant2002!
- [#949](https://github.com/ForNeVeR/Cesium/pull/949): Implement more cases for struct initialization. Thanks to @kant2002!
- [#950](https://github.com/ForNeVeR/Cesium/pull/950): Allow multiple typedefs for same type. Thanks to @kant2002!
- [#954](https://github.com/ForNeVeR/Cesium/pull/954): Add unsigned long long conversion. Thanks to @kant2002!
- [#957](https://github.com/ForNeVeR/Cesium/pull/957): Document --ast-dump option for diagnostic purposes. Thanks to @pkazakov-dev!
- [#962](https://github.com/ForNeVeR/Cesium/pull/962): Add support for g/G formats in printf/fprinf. Thanks to @kant2002!
- [#956](https://github.com/ForNeVeR/Cesium/pull/956): Add `strtod`/`strtof` functions. Thanks to @kant2002!
- [#948](https://github.com/ForNeVeR/Cesium/pull/948): Improve support for non-native TFMs in Cesium SDK. Thanks to @kant2002!

### Fixed
- [#952](https://github.com/ForNeVeR/Cesium/pull/952): Fix saving indirection for uint64 types. Thanks to @kant2002!
- [#953](https://github.com/ForNeVeR/Cesium/pull/953): Fix using anonymous unions in the function variables. Thanks to @kant2002!
- [#959](https://github.com/ForNeVeR/Cesium/pull/959): The project templates now define the `RollForward` option. Thanks to @pkazakov-dev!

### Changed
- [#965](https://github.com/ForNeVeR/Cesium/pull/965): New infrastructure for AST dumper. Thanks to @pkazakov-dev!

## [0.2.0] - 2026-02-14
### Changed
- Cesium now runs under .NET 10. Thanks to @pkazakov-dev!
- Update the dependency library versions.

### Added
- [#532](https://github.com/ForNeVeR/Cesium/issues/532): negative number support in preprocessor expressions. Thanks to @nt-devilboi!
- [#863](https://github.com/ForNeVeR/Cesium/pull/863): experimental `open_memstream` extension. Thanks to @kant2002!
- [#935](https://github.com/ForNeVeR/Cesium/pull/935): compound multidimensional array initialization. Thanks to @kant2002!
- [#865](https://github.com/ForNeVeR/Cesium/pull/865): `strstr` function. Thanks to @kant2002!
- [#867](https://github.com/ForNeVeR/Cesium/pull/867): anonymous global enum constant support. Thanks to @kant2002!
- [#931](https://github.com/ForNeVeR/Cesium/pull/931): anonymous local enum constant support. Thanks to @kant2002!
- [#868](https://github.com/ForNeVeR/Cesium/pull/868): floating point array initializer support. Thanks to @kant2002!
- [#870](https://github.com/ForNeVeR/Cesium/pull/870): IO functions
  - `fread`,
  - `fwrite`,
  - `fputc`,
  - `vprintf`,
  - `fflush`.
  Thanks to @kant2002!
- [#355](https://github.com/ForNeVeR/Cesium/issues/355): static struct layout calculation. Thanks to @kant2002!
- [#878](https://github.com/ForNeVeR/Cesium/pull/878): global constant `typedef` declaration. Thanks to @kant2002!
- [#879](https://github.com/ForNeVeR/Cesium/pull/879): function signature validity check during the link step. Thanks to @kant2002!
- [#942](https://github.com/ForNeVeR/Cesium/pull/942): `int64_t` cast support. Thanks to @kant2002!
- [#840: SDK: check for executable file on Unix]. Thanks to @evgTSV!

### Fixed
- [#862](https://github.com/ForNeVeR/Cesium/pull/862): fix codegen when if is last statement in the function returning void. Thanks to @kant2002!
- [#875](https://github.com/ForNeVeR/Cesium/pull/875): fix `fread` contract for reading stdin (no crash if reading from a closed pipe). Thanks to @kant2002!
- [#876](https://github.com/ForNeVeR/Cesium/pull/876), [#941](https://github.com/ForNeVeR/Cesium/pull/941): static identifiers' names are now scoped to translation unit. Thanks to @kant2002!
- [#930](https://github.com/ForNeVeR/Cesium/pull/930): correct `sizeof('a')`. Thanks to @kant2002!
- SDK was incorrectly treating non-existent `dotnet.exe` runtime file as executable on Windows.

## [0.1.2] - 2025-10-01
### Fixed
- [#849: SDK templates broken for 0.1.1](https://github.com/ForNeVeR/Cesium/issues/849).

## [0.1.1] - 2025-09-29
### Fixed
- [#843: SDK templates broken for 0.1.0](https://github.com/ForNeVeR/Cesium/issues/843).

### Changed
- Expression parsing speedup. Thanks to @kant2002!

## [0.1.0] - 2025-09-28
This is the initial release of Cesium.

### Added
- New component: Cesium.Compiler, packaged as
  - a .NET tool,
  - a framework-dependent bundle for SDK use,
  - a bunch of self-contained bundles for external use.

  The compiler currently supports about 25% of the language features.
- New component: Cesium.Sdk.
- New component: Cesium.Templates.

Thanks to our benefactors (in the alphabetical order):
- @7645re
- @abrahamFerga
- @AFernandezAtAriox
- @BadRyuner
- @BaLiKfromUA
- @BoundedChenn31
- @DavTen
- @epeshk
- @evgTSV
- @Fantoom
- @ForNeVeR
- @FrediKats
- @Griboedoff
- @gsomix
- @impworks
- @kant2002
- @kekyo
- @KeterSCP
- @kolosovpetro
- @leha-bot
- @maksimowiczm
- @MarkCiliaVincenti
- @Newlifer
- @PapisKang
- @reima
- @rstm-sf
- @seclerp
- @te9c
- @zawodskoj

[0.1.0]: https://github.com/ForNeVeR/Cesium/releases/tag/v0.1.0
[0.1.1]: https://github.com/ForNeVeR/Cesium/compare/v0.1.0...v0.1.1
[0.1.2]: https://github.com/ForNeVeR/Cesium/compare/v0.1.1...v0.1.2
[0.2.0]: https://github.com/ForNeVeR/Cesium/compare/v0.1.2...v0.2.0
[0.3.0]: https://github.com/ForNeVeR/Cesium/compare/v0.2.0...v0.3.0
[0.4.0]: https://github.com/ForNeVeR/Cesium/compare/v0.3.0...v0.4.0
[0.4.1]: https://github.com/ForNeVeR/Cesium/compare/v0.4.0...v0.4.1
[Unreleased]: https://github.com/ForNeVeR/Cesium/compare/v0.4.1...HEAD
