<!--
SPDX-FileCopyrightText: 2024-2025 Friedrich von Never <friedrich@fornever.me>

SPDX-License-Identifier: MIT
-->

Changelog
=========
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] (0.2.0)
### Added
- [#840: SDK: check for executable file on Unix]. Thanks to @evgTSV!

### Fixed
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
[Unreleased]: https://github.com/ForNeVeR/Cesium/compare/v0.1.2...HEAD
