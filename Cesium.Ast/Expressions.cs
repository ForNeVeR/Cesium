using System.Collections.Immutable;
using Yoakke.SynKit.C.Syntax;
using Yoakke.SynKit.Lexer;

namespace Cesium.Ast;

public abstract record Expression;

// 6.4.5 String literals
public sealed record StringLiteralListExpression(ImmutableArray<IToken<CTokenType>> ConstantList) : Expression;

// 6.5.1 Primary expressions
public sealed record IdentifierExpression(string Identifier) : Expression;
public sealed record ConstantLiteralExpression(IToken<CTokenType> Constant) : Expression;
public sealed record ParenExpression(Expression Contents) : Expression;

// 6.5.2 Postfix operators
public sealed record SubscriptingExpression(Expression Base, Expression Index) : Expression;
public sealed record FunctionCallExpression(Expression Function, ImmutableArray<Expression>? Arguments) : Expression;
public sealed record TypeCastOrNamedFunctionCallExpression(string TypeOrFunctionName, ImmutableArray<Expression> Arguments) : Expression;
public sealed record MemberAccessExpression(Expression Target, IdentifierExpression Identifier) : Expression;
public sealed record PointerMemberAccessExpression(Expression Target, IdentifierExpression Identifier) : Expression;
public sealed record PostfixIncrementDecrementExpression(IToken<CTokenType> PrefixOperator, Expression Target) : Expression;

// 6.5.3 Unary operators
public sealed record PrefixIncrementDecrementExpression(IToken<CTokenType> PrefixOperator, Expression Target) : Expression;
public sealed record UnaryOperatorExpression(string Operator, Expression Target) : Expression;
public sealed record IndirectionExpression(Expression Target) : Expression;

// 6.5.3.4 The sizeof and _Alignof operators
public sealed record UnaryExpressionSizeOfOperatorExpression(Expression TargetExpession) : Expression;

public sealed record TypeNameSizeOfOperatorExpression(TypeName TypeName) : Expression;

// 6.5.4 Cast expression
public sealed record CastExpression(TypeName TypeName, Expression Target) : Expression;

// 6.5.5–6.5.14: Various binary operators
public abstract record BinaryOperatorExpression(Expression Left, string Operator, Expression Right) : Expression;
public sealed record LogicalBinaryOperatorExpression(Expression Left, string Operator, Expression Right)
    : BinaryOperatorExpression(Left, Operator, Right);
public sealed record ArithmeticBinaryOperatorExpression(Expression Left, string Operator, Expression Right)
    : BinaryOperatorExpression(Left, Operator, Right);
public sealed record BitwiseBinaryOperatorExpression(Expression Left, string Operator, Expression Right)
    : BinaryOperatorExpression(Left, Operator, Right);
public sealed record ComparisonBinaryOperatorExpression(Expression Left, string Operator, Expression Right)
    : BinaryOperatorExpression(Left, Operator, Right);

// 6.5.15: Conditional operator
public sealed record ConditionalExpression(Expression Condition, Expression TrueExpression, Expression FalseExpression)
    : Expression;

// 6.5.16 Assignment operators
public sealed record AssignmentExpression(Expression Left, string Operator, Expression Right)
    : BinaryOperatorExpression(Left, Operator, Right);

// 6.5.17 Comma operator
public sealed record CommaExpression(Expression Left, Expression Right) : Expression;
