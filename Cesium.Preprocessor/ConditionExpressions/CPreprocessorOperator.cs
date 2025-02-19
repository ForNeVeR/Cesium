// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

namespace Cesium.Preprocessor;

internal enum CPreprocessorOperator
{
    Equals,
    NotEquals,
    LessOrEqual,
    GreaterOrEqual,
    LessThan,
    GreaterThan,
    Negation,
    LogicalOr,
    LogicalAnd,
    Add,
    Sub,
    Mul,
    Div,
}
