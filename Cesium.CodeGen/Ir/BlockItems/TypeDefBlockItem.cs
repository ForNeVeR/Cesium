// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Ir.Declarations;

namespace Cesium.CodeGen.Ir.BlockItems;

internal sealed class TypeDefBlockItem : IBlockItem
{
    public ICollection<LocalDeclarationInfo> Types { get; }

    public TypeDefBlockItem(TypeDefDeclaration declaration)
    {
        Types = declaration.Types;
    }

    public TypeDefBlockItem(ICollection<LocalDeclarationInfo> types)
    {
        Types = types;
    }
}
