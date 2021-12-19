namespace Cesium.Ast;

public record Expression;
public record IdentifierExpression(string Identifier) : Expression;
