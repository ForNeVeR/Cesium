using System.Collections.Immutable;

namespace Cesium.Ast;

// 6.8 Statements and blocks
public abstract record Statement : IBlockItem;

// 6.8.1 Labeled statement
public sealed record LabelStatement(string Identifier, Statement Body) : Statement;

public sealed record CaseStatement(Expression? Constant, Statement Body) : Statement;

// 6.8.2 Compound statement
public sealed record CompoundStatement(ImmutableArray<IBlockItem> Block) : Statement;

public interface IBlockItem
{
}

// 6.8.3 Expression and null statements
public sealed record ExpressionStatement(Expression? Expression) : Statement;

/// <summary>
/// An expression of form <code>item1(item2);</code> which may be either a function call or a variable definition,
/// depending on the context.
/// </summary>
public sealed record AmbiguousBlockItem(string Item1, string Item2) : IBlockItem;

// 6.8.4 Selection statements
public sealed record IfElseStatement(Expression Expression, Statement TrueBranch, Statement? FalseBranch) : Statement;

public sealed record SwitchStatement(Expression Expression, Statement Body) : Statement;

// 6.8.5 Iteration statements
public sealed record WhileStatement(
    Expression TestExpression,
    IBlockItem Body) : Statement;

public sealed record DoWhileStatement(
    Expression TestExpression,
    IBlockItem Body) : Statement;

public sealed record ForStatement(
    IBlockItem? InitDeclaration,
    Expression? InitExpression,
    Expression? TestExpression,
    Expression? UpdateExpression,
    IBlockItem Body) : Statement;

// 6.8.6 Jump statements
public sealed record GoToStatement(string Identifier) : Statement;

public sealed record BreakStatement : Statement;
public sealed record ContinueStatement : Statement;

public sealed record ReturnStatement(Expression Expression) : Statement;
