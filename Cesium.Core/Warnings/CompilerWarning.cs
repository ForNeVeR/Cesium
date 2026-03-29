// SPDX-FileCopyrightText: 2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

namespace Cesium.Core.Warnings;

public record CompilerWarning(string Message, WarningsSet Set)
    : DiagnosticWarning(new SourceLocationInfo(string.Empty, 0, 0), Message);
// TODO: In future we have to determine a location where a warning is triggered
