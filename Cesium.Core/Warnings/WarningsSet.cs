// SPDX-FileCopyrightText: 2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

namespace Cesium.Core.Warnings;

[Flags]
public enum WarningsSet : uint
{
    None                    = 0,

    // --- Group "All" ---
    ReturnType              = 1u << 0,  // -Wreturn-type
    Format                  = 1u << 1,  // -Wformat
    Unused                  = 1u << 2,  // -Wunused
    Implicit                = 1u << 3,  // -Wimplicit
    Parentheses             = 1u << 4,  // -Wparentheses
    Switch                  = 1u << 5,  // -Wswitch
    Uninitialized           = 1u << 6,  // -Wuninitialized
    MissingBraces           = 1u << 7,  // -Wmissing-braces
    SequencePoint           = 1u << 8,  // -Wsequence-point

    // --- Group "Extra" ---
    SignCompare             = 1u << 9,  // -Wsign-compare
    TypeLimits              = 1u << 10, // -Wtype-limits
    UnusedParameter         = 1u << 11, // -Wunused-parameter
    MissingFieldInits       = 1u << 12, // -Wmissing-field-initializers

    // --- Group "Pedantic" ---
    Shadow                  = 1u << 13, // -Wshadow
    Conversion              = 1u << 14, // -Wconversion
    PointerArith            = 1u << 15, // -Wpointer-arith
    CastAlign               = 1u << 16, // -Wcast-align
    FloatEqual              = 1u << 17, // -Wfloat-equal
    LogicalOp               = 1u << 18, // -Wlogical-op
    Pedantic                = 1u << 19, // -Wpedantic

    // --- Combinations ---

    All = ReturnType | Format | Unused | Implicit | Parentheses | Switch | Uninitialized | MissingBraces | SequencePoint,
    Extra = SignCompare | TypeLimits | UnusedParameter | MissingFieldInits,
    Full = All | Extra | Shadow | Conversion | PointerArith | CastAlign | FloatEqual | LogicalOp | Pedantic,
}
