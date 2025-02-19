// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

namespace Cesium.Core;

public sealed class PreprocessorException(SourceLocationInfo location, string message)
    : CesiumException($"{location}: {message}")
{
    public SourceLocationInfo? Location { get; } = location;
    public string RawMessage { get; } = message;
}
