// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal sealed class CaseStatement : IBlockItem
{
    public CaseStatement(Ast.CaseStatement statement, IDeclarationScope scope)
    {
        var (constant, body) = statement;

        Expression = constant?.ToIntermediate(scope);
        Statement = body.ToIntermediate(scope);
    }

    public string Label { get; } = Guid.NewGuid().ToString();
    public IBlockItem Statement { get; }
    public IExpression? Expression { get; }
}
