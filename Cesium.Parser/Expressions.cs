namespace Cesium.Parser;

public record Expression;
public record IdentifierExpression(string Identifier) : Expression;
