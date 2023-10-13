using System.Collections.Immutable;
using Yoakke.SynKit.C.Syntax;
using Yoakke.SynKit.Lexer;

namespace Cesium.Ast;

public abstract record Expression;

// 6.4.5 String literals
public record StringLiteralListExpression(ImmutableArray<IToken<CTokenType>> ConstantList) : Expression;

// 6.5.1 Primary expressions
public record IdentifierExpression(string Identifier) : Expression;
public record ConstantLiteralExpression(IToken<CTokenType> Constant) : Expression;
public record ParenExpression(Expression Contents) : Expression;

// 6.5.2 Postfix operators
public record SubscriptingExpression(Expression Base, Expression Index) : Expression;
public record FunctionCallExpression(Expression Function, ImmutableArray<Expression>? Arguments) : Expression;
public record TypeCastOrNamedFunctionCallExpression(string TypeOrFunctionName, ImmutableArray<Expression> Arguments) : Expression;
public record MemberAccessExpression(Expression Target, IdentifierExpression Identifier) : Expression;
public record PointerMemberAccessExpression(Expression Target, IdentifierExpression Identifier) : Expression;
public record PostfixIncrementDecrementExpression(IToken<CTokenType> PrefixOperator, Expression Target) : Expression;

// 6.5.3 Unary operators
public record PrefixIncrementDecrementExpression(IToken<CTokenType> PrefixOperator, Expression Target) : Expression;
public record UnaryOperatorExpression(string Operator, Expression Target) : Expression;
public record IndirectionExpression(Expression Target) : Expression;

// 6.5.4 Cast expression
public record CastExpression(TypeName TypeName, Expression Target) : Expression;

// 6.5.5–6.5.14: Various binary operators
public record BinaryOperatorExpression(Expression Left, string Operator, Expression Right) : Expression;
public record LogicalBinaryOperatorExpression(Expression Left, string Operator, Expression Right)
    : BinaryOperatorExpression(Left, Operator, Right);
public record ArithmeticBinaryOperatorExpression(Expression Left, string Operator, Expression Right)
    : BinaryOperatorExpression(Left, Operator, Right);
public record BitwiseBinaryOperatorExpression(Expression Left, string Operator, Expression Right)
    : BinaryOperatorExpression(Left, Operator, Right);
public record ComparisonBinaryOperatorExpression(Expression Left, string Operator, Expression Right)
    : BinaryOperatorExpression(Left, Operator, Right);

// 6.5.15: Conditional operator
public record ConditionalExpression(Expression Condition, Expression TrueExpression, Expression FalseExpression)
    : Expression;

// 6.5.16 Assignment operators
public record AssignmentExpression(Expression Left, string Operator, Expression Right)
    : BinaryOperatorExpression(Left, Operator, Right);

// 6.5.17 Comma operator
public record CommaExpression(Expression Left, Expression Right) : Expression;
