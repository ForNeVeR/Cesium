// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal sealed class ExpressionStatement : IBlockItem
{
    public IExpression? Expression { get; }

    internal ExpressionStatement(IExpression? expression)
    {
        Expression = expression switch
        {
            PostfixIncrementDecrementExpression => new DiscardResultExpression(expression),
            _ => expression
        };
    }

    public ExpressionStatement(Ast.ExpressionStatement statement, IDeclarationScope scope) : this(statement.Expression?.ToIntermediate(scope))
    {
    }
}
