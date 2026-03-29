// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

namespace Cesium.Core.Warnings;

public interface IWarningProcessor<in T> where T : DiagnosticWarning
{
    public void EmitWarning(T warning);
}
