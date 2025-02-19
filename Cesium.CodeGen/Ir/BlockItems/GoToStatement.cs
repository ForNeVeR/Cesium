// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

namespace Cesium.CodeGen.Ir.BlockItems;

internal sealed class GoToStatement : IBlockItem
{
    public string Identifier { get; }

    public GoToStatement(Ast.GoToStatement statement)
    {
        Identifier = statement.Identifier;
    }

    public GoToStatement(string identifier)
    {
        Identifier = identifier;
    }
}
