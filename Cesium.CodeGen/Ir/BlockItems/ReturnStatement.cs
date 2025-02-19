// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal sealed class ReturnStatement : IBlockItem
{
    public IExpression? Expression { get; }

    public ReturnStatement(Ast.ReturnStatement statement, IDeclarationScope scope)
    {
        Expression = statement.Expression?.ToIntermediate(scope);
    }

    public ReturnStatement(IExpression? expression)
    {
        Expression = expression;
    }
}
