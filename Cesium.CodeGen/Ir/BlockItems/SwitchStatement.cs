// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal sealed class SwitchStatement : IBlockItem
{
    public IExpression Expression { get; }
    public IBlockItem Body { get; }

    public SwitchStatement(Ast.SwitchStatement statement, IDeclarationScope scope)
    {
        var (expression, body) = statement;

        Expression = expression.ToIntermediate(scope);
        Body = body.ToIntermediate(scope);
    }
}
