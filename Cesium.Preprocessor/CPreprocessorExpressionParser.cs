using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Parser;
using Yoakke.SynKit.Parser.Attributes;

namespace Cesium.Preprocessor;

using ICPreprocessorToken = IToken<CPreprocessorTokenType>;

[Parser(typeof(CPreprocessorTokenType))]
[SuppressMessage("ReSharper", "UnusedParameter.Local")] // parser parameters are mandatory even if unused
internal partial class CPreprocessorExpressionParser
{
    [Rule("identifier: PreprocessingToken")]
    private static IPreprocessorExpression MakeIdentifier(ICPreprocessorToken token) => new IdentifierExpression(token.Text);

    [Rule("simple_expression: identifier")]
    private static IPreprocessorExpression MakeSimpleExpression(IPreprocessorExpression expression) => expression;

    [Rule("equality_expression: simple_expression '==' simple_expression")]
    private static EqualityExpression MakeEqaulityExpression(IPreprocessorExpression left, ICPreprocessorToken token, IPreprocessorExpression right) => new EqualityExpression(left, right);

    [Rule("expression: identifier")]
    [Rule("expression: equality_expression")]
    private static IPreprocessorExpression MakeExpression(IPreprocessorExpression expression) => expression;
}

internal class IdentifierExpression : IPreprocessorExpression
{
    public IdentifierExpression(string identifer)
    {
        this.Identifer = identifer;
    }

    public string Identifer { get; }

    public string? EvaluateExpression(IMacroContext context)
    {
        if (context.TryResolveMacro(this.Identifer, out var result))
        {
            return result;
        }

        if (Regex.IsMatch(this.Identifer, Regexes.IntLiteral))
        {
            return this.Identifer;
        }

        return result;
    }
}

internal class EqualityExpression : IPreprocessorExpression
{
    public EqualityExpression(IPreprocessorExpression first, IPreprocessorExpression second)
    {
        First = first;
        Second = second;
    }

    public IPreprocessorExpression First { get; }
    public IPreprocessorExpression Second { get; }

    public string? EvaluateExpression(IMacroContext context)
    {
        string? firstValue = First.EvaluateExpression(context);
        string? secondValue = Second.EvaluateExpression(context);
        return firstValue == secondValue ? "1" : null;
    }
}
