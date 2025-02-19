// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Ir.Declarations;

namespace Cesium.CodeGen.Ir.BlockItems;

internal sealed class TagBlockItem : IBlockItem
{
    public ICollection<LocalDeclarationInfo> Types { get; }

    public TagBlockItem(ICollection<LocalDeclarationInfo> types)
    {
        Types = types;
    }
}
