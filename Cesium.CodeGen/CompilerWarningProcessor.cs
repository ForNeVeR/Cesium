// SPDX-FileCopyrightText: 2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.Core;
using Cesium.Core.Warnings;

namespace Cesium.CodeGen;

public class CompilerWarningProcessor(WarningsSet set) : IWarningProcessor<CompilerWarning>
{
    public void EmitWarning(CompilerWarning warning)
    {
        if (set.HasFlag(warning.Set))
            Console.Error.WriteLine($"Warning: {warning.Message} [-W{warning.Set.ToString().FromCamelToKebab()}]");
    }
}
