using System.Collections.Immutable;

namespace Cesium.Ast;

// 6.8 Statements and blocks
public abstract record Statement : IBlockItem;
public record CompoundStatement(ImmutableArray<IBlockItem> Block) : Statement;

public interface IBlockItem {}

// 6.8.6 Jump statements
public record GoToStatement(string Identifier) : Statement;
public record ReturnStatement(Expression Expression) : Statement;
