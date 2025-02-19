// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

namespace Cesium.Core.Warnings;

public interface IWarningProcessor
{
    public void EmitWarning(PreprocessorWarning warning);
}
