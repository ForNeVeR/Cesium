// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

namespace Cesium.CodeGen.Ir.Expressions;

public enum UnaryOperator
{
    Negation, // -
    Promotion, // +
    BitwiseNot, // ~
    LogicalNot, // !
    AddressOf, // &
    Indirection, // *
}
