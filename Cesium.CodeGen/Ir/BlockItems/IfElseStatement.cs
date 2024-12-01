using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal record IfElseStatement : IBlockItem
{
    public IExpression Expression { get; init; }
    public IBlockItem TrueBranch { get; init; }
    public IBlockItem? FalseBranch { get; init; }

    public bool? IsEscapeBranchRequired { get; set; }

    public IfElseStatement(IExpression expression, IBlockItem trueBranch, IBlockItem? falseBranch)
    {
        Expression = expression;
        TrueBranch = trueBranch;
        FalseBranch = falseBranch;
    }

    public IfElseStatement(Ast.IfElseStatement statement, IDeclarationScope scope)
    {
        var (expression, trueBranch, falseBranch) = statement;
        Expression = expression.ToIntermediate(scope);
        TrueBranch = trueBranch.ToIntermediate(scope);
        FalseBranch = falseBranch?.ToIntermediate(scope);
    }
}
