using System.Collections.Immutable;

namespace Cesium.Ast;

// 6.8 Statements and blocks
public abstract record Statement : IBlockItem;

// 6.8.2 Compound statement
public record CompoundStatement(ImmutableArray<IBlockItem> Block) : Statement;

public interface IBlockItem {}

// 6.8.3 Expression and null statements
public record ExpressionStatement(Expression? Expression) : Statement;

// 6.8.6 Jump statements
public record GoToStatement(string Identifier) : Statement;
public record ReturnStatement(Expression Expression) : Statement;
