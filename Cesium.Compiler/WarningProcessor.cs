// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.Core.Warnings;

namespace Cesium.Compiler;

public class WarningProcessor : IWarningProcessor<DiagnosticWarning>
{
    public void EmitWarning(DiagnosticWarning warning)
    {
        Console.Error.WriteLine($"{warning.Location}: warning: {warning.Message}");
    }
}
