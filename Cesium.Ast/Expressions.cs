using System.Collections.Immutable;
using Yoakke.C.Syntax;
using Yoakke.Lexer;

namespace Cesium.Ast;

public abstract record Expression;

// 6.5.1 Primary expressions
public record IdentifierExpression(string Identifier) : Expression;
public record ConstantExpression(IToken<CTokenType> Constant) : Expression;
public record StringConstantExpression(string ConstantContent) : Expression;

// 6.5.2 Postfix operators
public record SubscriptingExpression(Expression Base, Expression Index) : Expression;
public record FunctionCallExpression(Expression Function, ImmutableArray<Expression>? Arguments) : Expression;

// 6.5.3 Unary operators
public record NegationExpression(Expression Target) : Expression;

// 6.5.5–6.5.15: Various binary operators
public record BinaryOperatorExpression(Expression Left, string Operator, Expression Right) : Expression;

// 6.5.16 Assignment operators
public record AssignmentExpression(Expression Left, string Operator, Expression Right)
    : BinaryOperatorExpression(Left, Operator, Right);
