// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Yoakke.SynKit.Text;

namespace Cesium.Preprocessor;

internal interface IPreprocessorExpression
{
    Location Location { get; }
    string? EvaluateExpression(IMacroContext context);
}
