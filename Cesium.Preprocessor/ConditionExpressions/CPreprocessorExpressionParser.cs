// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.Core;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Parser.Attributes;

namespace Cesium.Preprocessor;

using ICPreprocessorToken = IToken<CPreprocessorTokenType>;

[Parser(typeof(CPreprocessorTokenType))]
internal partial class CPreprocessorExpressionParser
{
    [Rule("identifier: PreprocessingToken")]
    private static IPreprocessorExpression MakeIdentifier(ICPreprocessorToken token) => new IdentifierExpression(token.Location, token.Text);

    [Rule("identifier_defined: 'defined' PreprocessingToken")]
    private static IPreprocessorExpression MakeIdentifier(
        ICPreprocessorToken definedToken,
        ICPreprocessorToken token) => new DefinedExpression(definedToken.Location, token.Text);

    [Rule("identifier_defined: 'defined' '(' PreprocessingToken ')' ")]
    private static IPreprocessorExpression MakeIdentifier(
        ICPreprocessorToken definedToken,
        ICPreprocessorToken openToken,
        ICPreprocessorToken token,
        ICPreprocessorToken closedToken) => new DefinedExpression(definedToken.Location, token.Text);

    [Rule("simple_expression: identifier")]
    private static IPreprocessorExpression MakeSimpleExpression(IPreprocessorExpression expression) => expression;

    [Rule("multiplicative_expression: multiplicative_expression '*' cast_expression")]
    [Rule("multiplicative_expression: multiplicative_expression '/' cast_expression")]
    [Rule("multiplicative_expression: multiplicative_expression '%' cast_expression")]
    [Rule("additive_expression: additive_expression '+' multiplicative_expression")]
    [Rule("additive_expression: additive_expression '-' multiplicative_expression")]
    [Rule("shift_expression: shift_expression '<=' additive_expression")]
    [Rule("shift_expression: shift_expression '>=' additive_expression")]
    [Rule("relational_expression: relational_expression '<=' shift_expression")]
    [Rule("relational_expression: relational_expression '>=' shift_expression")]
    [Rule("relational_expression: relational_expression '<' shift_expression")]
    [Rule("relational_expression: relational_expression '>' shift_expression")]
    [Rule("equality_expression: equality_expression '==' relational_expression")]
    [Rule("equality_expression: equality_expression '!=' relational_expression")]
    [Rule("and_expression: and_expression '&' equality_expression")]
    [Rule("exclusive_or_expression: exclusive_or_expression '^' and_expression")]
    [Rule("inclusive_or_expression: inclusive_or_expression '|' exclusive_or_expression")]
    [Rule("logical_or_expression: logical_or_expression '||' logical_and_expression")]
    [Rule("logical_and_expression: logical_and_expression '&&' inclusive_or_expression")]
    private static IPreprocessorExpression MakeBinaryExpression(IPreprocessorExpression left, ICPreprocessorToken token, IPreprocessorExpression right)
        => new BinaryExpression(left, GetOperator(token), right);

    [Rule("conditional_expression: logical_or_expression '?' expression ':' conditional_expression")]
    private static IPreprocessorExpression MakeConditionalExpression(IPreprocessorExpression condition, ICPreprocessorToken token, IPreprocessorExpression trueExpression, ICPreprocessorToken token2, IPreprocessorExpression falseExpression)
        => new ConditionalExpression(condition, trueExpression, falseExpression);

    [Rule("unary_expression: '!' cast_expression")]
    [Rule("unary_expression: '+' cast_expression")]
    [Rule("unary_expression: '-' cast_expression")]
    [Rule("unary_expression: '~' cast_expression")]
    private static IPreprocessorExpression MakeUnaryExpression(ICPreprocessorToken token, IPreprocessorExpression expression)
        => new UnaryExpression(token.Location, GetOperator(token), expression);

    [Rule("postfix_expression: identifier")]
    [Rule("unary_expression: identifier_defined")]
    [Rule("unary_expression: postfix_expression")]
    [Rule("cast_expression: unary_expression")]
    [Rule("multiplicative_expression: cast_expression")]
    [Rule("additive_expression: multiplicative_expression")]
    [Rule("shift_expression: additive_expression")]
    [Rule("relational_expression: shift_expression")]
    [Rule("equality_expression: relational_expression")]
    [Rule("and_expression: equality_expression")]
    [Rule("exclusive_or_expression: and_expression")]
    [Rule("inclusive_or_expression: exclusive_or_expression")]
    [Rule("logical_and_expression: inclusive_or_expression")]
    [Rule("logical_or_expression: logical_and_expression")]
    [Rule("conditional_expression: logical_or_expression")]
    [Rule("expression: conditional_expression")]
    private static IPreprocessorExpression MakeExpression(IPreprocessorExpression expression) => expression;

    [Rule("postfix_expression: '(' conditional_expression ')' ")]
    private static IPreprocessorExpression MakeExpression(ICPreprocessorToken lparen, IPreprocessorExpression expression, ICPreprocessorToken rparen) => expression;

    private static CPreprocessorOperator GetOperator(ICPreprocessorToken token) => token.Text switch
    {
        "+" => CPreprocessorOperator.Add,
        "-" => CPreprocessorOperator.Sub,
        "*" => CPreprocessorOperator.Mul,
        "/" => CPreprocessorOperator.Div,
        "==" => CPreprocessorOperator.Equals,
        "!=" => CPreprocessorOperator.NotEquals,
        "<=" => CPreprocessorOperator.LessOrEqual,
        ">=" => CPreprocessorOperator.GreaterOrEqual,
        "<" => CPreprocessorOperator.LessThan,
        ">" => CPreprocessorOperator.GreaterThan,
        "!" => CPreprocessorOperator.Negation,
        "||" => CPreprocessorOperator.LogicalOr,
        "&&" => CPreprocessorOperator.LogicalAnd,
        _ => throw new CompilationException($"Operator {token.Text} cannot be used in the preprocessor directives"),
    };
}
