using System.Collections.Immutable;

namespace Cesium.Ast;

// 6.8 Statements and blocks
public abstract record Statement : IBlockItem;

// 6.8.2 Compound statement
public record CompoundStatement(ImmutableArray<IBlockItem> Block) : Statement;

public interface IBlockItem
{
}

// 6.8.3 Expression and null statements
public record ExpressionStatement(Expression? Expression) : Statement;

/// <summary>
/// An expression of form <code>item1(item2);</code> which may be either a function call or a variable definition,
/// depending on the context.
/// </summary>
public record AmbiguousBlockItem(string Item1, string Item2) : IBlockItem;

// 6.8.4 Selection statements
public record IfElseStatement(Expression Expression, Statement TrueBranch, Statement? FalseBranch) : Statement;

// 6.8.5 Iteration statements
public record ForStatement(
    Expression? InitExpression,
    Expression? TestExpression,
    Expression? UpdateExpression,
    IBlockItem Body) : Statement;

// 6.8.6 Jump statements
public record GoToStatement(string Identifier) : Statement;

public record BreakStatement : Statement;

public record ReturnStatement(Expression Expression) : Statement;
