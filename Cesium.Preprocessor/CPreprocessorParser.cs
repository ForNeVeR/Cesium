using System.Collections.Immutable;
using Cesium.Core;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Parser;
using Yoakke.SynKit.Text;

namespace Cesium.Preprocessor;

using ICPreprocessorToken = IToken<CPreprocessorTokenType>;

/// <remarks>C23 Standard, section 6.10 Preprocessing directives.</remarks>
/// TODO: Figure out the final newline treatment. Currently if it is absent then some directives won't be parsed properly.
internal class CPreprocessorParser(TransactionalLexer lexer)
{
    public ParseResult<PreprocessingFile> ParsePreprocessingFile()
    {
        var group = ParseGroup();
        if (!group.IsOk) return group.Error;

        if (lexer.IsEnd) return Ok(new PreprocessingFile(group.Ok.Value.ToImmutableArray()));

        if (group.FurthestError != null)
            return group.FurthestError;

        var nextToken = lexer.Next();
        return Error("end of stream", nextToken, "preprocessing-file");

    }

    private ParseResult<List<IGroupPart>> ParseGroup()
    {
        var parts = new List<IGroupPart>();
        ParseResult<IGroupPart> groupPart;
        while (!lexer.IsEnd && (groupPart = ParseGroupPart()).IsOk)
        {
            parts.Add(groupPart.Ok.Value);
        }

        return Ok(parts, groupPart.IsError ? groupPart.Error : null);
    }

    private ParseResult<IGroupPart> ParseGroupPart()
    {
        using var transaction = lexer.BeginTransaction();

        var ifSection = ParseIfSection();
        if (ifSection.IsOk) return transaction.End(ifSection);

        var controlLine = ParseControlLine();
        if (controlLine.IsOk) return transaction.End(controlLine);

        var textLine = ParseTextLine();
        if (textLine.IsOk) return transaction.End(textLine);

        if (Peek() is var token and not { Kind: CPreprocessorTokenType.Hash })
            return transaction.End(Error("#", token, "group-part"));
        _ = Next();

        return transaction.End(ParseNonDirective());
    }

    private ParseResult<IGroupPart> ParseIfSection()
    {
        using var transaction = lexer.BeginTransaction();

        var ifGroup = ParseIfGroup();
        if (!ifGroup.IsOk) return transaction.End(ifGroup.Error);

        var elIfGroups = new List<GuardedGroup>();
        ParseResult<GuardedGroup> elIfGroup;
        while ((elIfGroup = ParseElIfGroup()).IsOk)
        {
            elIfGroups.Add(elIfGroup.Ok.Value);
        }

        var elseGroup = ParseElseGroup();

        var endIfLine = ParseEndIfLine();
        if (!endIfLine.IsOk) return transaction.End(endIfLine.Error);

        return transaction.End(
            Ok<IGroupPart>(
                new IfSection(
                    ifGroup.Ok.Value,
                    elIfGroups.ToImmutableArray(),
                    elseGroup.IsOk ? elseGroup.Ok.Value : null)));
    }

    private ParseResult<GuardedGroup> ParseIfGroup()
    {
        using var transaction = lexer.BeginTransaction();

        if (Peek() is var shouldBeHash and not { Kind: CPreprocessorTokenType.Hash })
            return transaction.End(Error("#", shouldBeHash, "if-group"));
        _ = Next();

        if (Peek() is var token and not { Text: "if" or "ifdef" or "ifndef" })
        {
            return transaction.End(Error("if or ifdef or ifndef", token, "if-group"));
        }

        IList<ICPreprocessorToken> expressionTokens;
        var keyword = Next();
        if (keyword.Text == "if")
        {
            var expression = ParsePpTokens();
            if (!expression.IsOk) return transaction.End(expression.Error);

            expressionTokens = expression.Ok.Value;
        }
        else
        {
            var identifier = ParseIdentifier();
            if (!identifier.IsOk) return transaction.End(identifier.Error);

            expressionTokens = [identifier.Ok.Value];
        }

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return transaction.End(newLine.Error);

        var group = ParseGroup();
        return transaction.End(
            Ok(
                new GuardedGroup(
                    keyword,
                    expressionTokens.ToImmutableArray(),
                    group.IsOk ? group.Ok.Value.ToImmutableArray() : [])));
    }

    private ParseResult<ICPreprocessorToken> ParseIdentifier()
    {
        using var transaction = lexer.BeginTransaction();
        if (Peek() is var shouldBeIdentifier and not { Kind: CPreprocessorTokenType.PreprocessingToken })
        {
            return transaction.End(Error("an identifier", shouldBeIdentifier, "identifier"));
        }

        return transaction.End(Ok(Next()));
    }

    private ParseResult<GuardedGroup> ParseElIfGroup()
    {
        using var transaction = lexer.BeginTransaction();

        if (Peek() is var shouldBeHash and not { Kind: CPreprocessorTokenType.Hash })
            return transaction.End(Error("#", shouldBeHash, "elif-group"));
        _ = Next();

        if (Peek() is var shouldBeKeyword and not { Text: "elif" or "elifdef" or "elifndef" })
        {
            return transaction.End(Error(["elif", "elifdef", "elifndef"], shouldBeKeyword, "elif-group"));
        }

        IList<ICPreprocessorToken> expressionTokens;
        var keyword = Next();
        if (keyword.Text == "elif")
        {
            var expression = ParsePpTokens();
            if (!expression.IsOk) return transaction.End(expression.Error);

            expressionTokens = expression.Ok.Value;
        }
        else
        {
            var identifier = ParseIdentifier();
            if (!identifier.IsOk) return transaction.End(identifier.Error);

            expressionTokens = [identifier.Ok.Value];
        }

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return transaction.End(newLine.Error);

        var group = ParseGroup();
        return transaction.End(
            Ok(
                new GuardedGroup(
                    keyword,
                    expressionTokens.ToImmutableArray(),
                    group.IsOk ? group.Ok.Value.ToImmutableArray() : [])));
    }

    private ParseResult<GuardedGroup> ParseElseGroup()
    {
        using var transaction = lexer.BeginTransaction();

        if (Peek() is var shouldBeHash and not { Kind: CPreprocessorTokenType.Hash })
            return transaction.End(Error("#", shouldBeHash, "else-group"));
        _ = Next();

        if (Peek() is var shouldBeElse and not { Text: "else" })
        {
            return transaction.End(Error("else", shouldBeElse, "elif-group"));
        }

        var keyword = Next();

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return transaction.End(newLine.Error);

        var group = ParseGroup();
        return transaction.End(
            Ok(new GuardedGroup(keyword, null, group.IsOk ? group.Ok.Value.ToImmutableArray() : [])));
    }

    private ParseResult<object?> ParseEndIfLine()
    {
        using var transaction = lexer.BeginTransaction();

        if (Peek() is var shouldBeHash and not { Kind: CPreprocessorTokenType.Hash })
            return transaction.End(Error("#", shouldBeHash, "endif-line"));
        _ = Next();

        if (Peek() is var shouldBeEndIf and not { Text: "endif" })
        {
            return transaction.End(Error("endif", shouldBeEndIf, "endif-line"));
        }
        _ = Next();

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return transaction.End(newLine.Error);

        return transaction.End(Ok<object?>(null));
    }

    private ParseResult<IGroupPart> ParseControlLine()
    {
        using var transaction = lexer.BeginTransaction();

        if (Peek() is var shouldBeHash and not { Kind: CPreprocessorTokenType.Hash })
            return transaction.End(Error("#", shouldBeHash, "control-line"));
        var hash = Next();

        var include = ParseInclude();
        if (include.IsOk) return transaction.End(include);

        var embed = ParseEmbed();
        if (embed.IsOk) return transaction.End(embed);

        var define = ParseDefine();
        if (define.IsOk) return transaction.End(define);

        var undef = ParseUndef();
        if (undef.IsOk) return transaction.End(undef);

        var line = ParseLine();
        if (line.IsOk) return transaction.End(line);

        var error = ParseError(hash.Location);
        if (error.IsOk) return transaction.End(error);

        var warning = ParseWarning();
        if (warning.IsOk) return transaction.End(warning);

        var pragma = ParsePragma();
        if (pragma.IsOk) return transaction.End(pragma);

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return transaction.End(newLine.Error);

        return transaction.End(Ok<IGroupPart>(new EmptyDirective()));
    }

    private ParseResult<IGroupPart> ParseInclude()
    {
        using var transaction = lexer.BeginTransaction();

        if (Peek() is var shouldBeInclude and not { Text: "include" })
            return transaction.End(
                Error("include", shouldBeInclude, "include"));
        _ = Next();

        var tokens = ParsePpTokens();
        if (!tokens.IsOk) return transaction.End(tokens.Error);

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return transaction.End(newLine.Error);

        return transaction.End(Ok<IGroupPart>(new IncludeDirective(tokens.Ok.Value.ToImmutableArray())));
    }

    private ParseResult<IGroupPart> ParseEmbed()
    {
        using var transaction = lexer.BeginTransaction();

        if (Peek() is var shouldBeEmbed and not { Text: "embed" })
            return transaction.End(Error("embed", shouldBeEmbed, "embed"));
        _ = Next();

        var tokens = ParsePpTokens();
        if (!tokens.IsOk) return transaction.End(tokens.Error);

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return transaction.End(newLine.Error);

        return transaction.End(Ok<IGroupPart>(new EmbedDirective(tokens.Ok.Value.ToImmutableArray())));
    }

    private ParseResult<IGroupPart> ParseDefine()
    {
        using var transaction = lexer.BeginTransaction();

        if (Peek() is var shouldBeDefine and not { Text: "define" })
            return transaction.End(Error("define", shouldBeDefine, "define"));
        _ = Next();

        var identifier = ParseIdentifier();
        if (!identifier.IsOk) return transaction.End(identifier.Error);

        MacroParameters? parameters;
        var lParen = ParseLParen();
        if (lParen.IsOk)
        {
            var parameterBlock = ParseMacroParameters();
            if (!parameterBlock.IsOk) return transaction.End(parameterBlock.Error);

            parameters = parameterBlock.Ok.Value;
        }
        else
        {
            parameters = null;
        }

        var replacementList = ParseReplacementList();
        if (!replacementList.IsOk) return transaction.End(replacementList.Error);

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return transaction.End(newLine.Error);

        return transaction.End(
            Ok<IGroupPart>(
                new DefineDirective(identifier.Ok.Value, parameters, replacementList.Ok.Value.ToImmutableArray())));
    }

    private ParseResult<MacroParameters> ParseMacroParameters()
    {
        using var transaction = lexer.BeginTransaction();

        var identifiers = new List<ICPreprocessorToken>();
        var hasEllipsis = false;

        ParseError NoParametersAfterEllipsis(ICPreprocessorToken token) =>
            transaction.End(Error(")", token, "identifier-list"));

        ParseResult<MacroParameters> SuccessParsing() =>
            transaction.End(Ok(new MacroParameters(identifiers.ToImmutableArray(), hasEllipsis)));

        while (true)
        {
            var token = Next();
            switch (token)
            {
                case { Kind: CPreprocessorTokenType.Ellipsis }:
                    if (hasEllipsis) return NoParametersAfterEllipsis(token);
                    hasEllipsis = true;
                    break;
                case {Kind: CPreprocessorTokenType.PreprocessingToken}:
                    if (hasEllipsis) return NoParametersAfterEllipsis(token);
                    identifiers.Add(token);
                    break;
                case { Kind: CPreprocessorTokenType.RightParen }:
                    return SuccessParsing();
            }

            var nextToken = Peek();
            switch (nextToken)
            {
                case { Kind: CPreprocessorTokenType.RightParen }:
                    _ = Next();
                    return SuccessParsing();
                case { Text: "," }:
                    _ = Next();
                    continue;
                default:
                    return transaction.End(Error([",", ")"], token, "identifier-list"));
            }
        }
    }

    private ParseResult<IGroupPart> ParseUndef()
    {
        using var transaction = lexer.BeginTransaction();

        if (Peek() is var shouldBeUnDef and not { Text: "undef" })
            return transaction.End(Error("undef", shouldBeUnDef, "undef"));
        _ = Next();

        var identifier = ParseIdentifier();
        if (!identifier.IsOk) return transaction.End(identifier.Error);

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return transaction.End(newLine.Error);

        return transaction.End(Ok<IGroupPart>(new UnDefDirective(identifier.Ok.Value)));
    }

    private ParseResult<IGroupPart> ParseLine()
    {
        using var transaction = lexer.BeginTransaction();

        if (Peek() is var shouldBeLine and not { Text: "line" })
            return transaction.End(Error("line", shouldBeLine, "line"));
        _ = Next();

        var tokens = ParsePpTokens();
        if (!tokens.IsOk) return transaction.End(tokens.Error);

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return transaction.End(newLine.Error);

        return transaction.End(Ok<IGroupPart>(new LineDirective(tokens.Ok.Value.ToImmutableArray())));
    }

    private ParseResult<IGroupPart> ParseError(Location hashLocation)
    {
        using var transaction = lexer.BeginTransaction();

        if (Peek() is var shouldBeError and not { Text: "error" })
            return transaction.End(Error("error", shouldBeError, "error"));
        _ = Next();

        var tokens = GetAllUntilNewLine();
        var newLine = ParseNewLine();
        if (!newLine.IsOk) return transaction.End(newLine.Error);

        return transaction.End(
            Ok<IGroupPart>(new ErrorDirective(hashLocation, tokens.IsOk ? tokens.Ok.Value.ToImmutableArray() : null)));
    }

    private ParseResult<IGroupPart> ParseWarning()
    {
        using var transaction = lexer.BeginTransaction();

        if (Peek() is var shouldBeWarning and not { Text: "warning" })
            return transaction.End(
                Error("warning", shouldBeWarning, "warning"));
        _ = Next();

        var tokens = ParsePpTokens();
        if (!tokens.IsOk) return transaction.End(tokens.Error);

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return transaction.End(newLine.Error);

        return transaction.End(
            Ok<IGroupPart>(new WarningDirective(tokens.IsOk ? tokens.Ok.Value.ToImmutableArray() : null)));
    }

    private ParseResult<IGroupPart> ParsePragma()
    {
        using var transaction = lexer.BeginTransaction();

        if (Peek() is var shouldBePragma and not { Text: "pragma" })
            return transaction.End(Error("pragma", shouldBePragma, "pragma"));
        _ = Next();

        var tokens = ParsePpTokens();
        if (!tokens.IsOk) return transaction.End(tokens.Error);

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return transaction.End(newLine.Error);

        return transaction.End(
            Ok<IGroupPart>(new PragmaDirective(tokens.IsOk ? tokens.Ok.Value.ToImmutableArray() : null)));
    }

    // TODO: Add test about comment preservation in such lines. According to the current implementation, they should
    // not be preserved, even though that's incorrect.
    private ParseResult<IGroupPart> ParseTextLine()
    {
        using var transaction = lexer.BeginTransaction();

        if (Peek() is { Kind: CPreprocessorTokenType.Hash } token)
            return transaction.End(Error("anything but #", token, "text-line"));

        var tokens = GetAllUntilNewLine();
        var newLine = ParseNewLine();
        if (!newLine.IsOk) return transaction.End(newLine.Error);

        return transaction.End(Ok<IGroupPart>(new TextLine(tokens.IsOk ? tokens.Ok.Value.ToImmutableArray() : null)));
    }

    private ParseResult<IGroupPart> ParseNonDirective()
    {
        using var transaction = lexer.BeginTransaction();

        if (PeekKeyword() is { } token)
            return transaction.End(
                Error("anything but a preprocessor directive keyword", token, "non-directive"));

        var tokens = ParsePpTokens();
        if (!tokens.IsOk) return transaction.End(tokens.Error);

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return transaction.End(newLine.Error);

        return transaction.End(Ok<IGroupPart>(new NonDirective(tokens.Ok.Value.ToImmutableArray())));
    }

    // TODO: Check that it is not immediately preceded by whitespace, according to the standard
    // TODO:  - Implemented, add test
    // TODO: What if it's not a whitespace but a comment?
    // TODO:  - The answer is that whitespace counts as whitespace as well (implemented ✓); add a test for it!
    // TODO: Test example below
    // #define BRACES/**/()
    // #define BRACES2 ()
    // #define BRACES3()
    //
    // foo BRACES
    // foo BRACES2
    // foo BRACES3()
    //
    // This yields to "foo ()\nfoo ()\nfoo" in gcc -E
    private ParseResult<ICPreprocessorToken> ParseLParen()
    {
        using var transaction = lexer.BeginTransaction();

        var lParen = Peek();
        if (lParen is not { Kind: CPreprocessorTokenType.LeftParen })
            return transaction.End(Error("(", lParen, "lparen"));

        var lastPrecedingToken = NextWithNonSignificant();
        if (lastPrecedingToken.Equals(lParen)) // not preceded by anything
            return transaction.End(Ok(lParen));

        return transaction.End(Error(lParen.Text, lastPrecedingToken, "lparen"));
    }

    private ParseResult<List<ICPreprocessorToken>> ParseReplacementList()
    {
        // Skip the whitespace after the macro name:
        while (PeekWithNonSignificant() is { Kind: CPreprocessorTokenType.WhiteSpace })
            _ = NextWithNonSignificant();

        var tokens = GetAllUntilNewLine();
        return tokens.IsOk ? Ok(tokens.Ok.Value) : Ok<List<ICPreprocessorToken>>([]);
    }

    private ParseResult<List<ICPreprocessorToken>> ParsePpTokens()
    {
        var result = new List<ICPreprocessorToken>();
        while (Peek() is not { Kind: CPreprocessorTokenType.NewLine or CPreprocessorTokenType.End })
            result.Add(Next());

        return Ok(result);
    }

    private ParseResult<List<ICPreprocessorToken>> GetAllUntilNewLine()
    {
        var result = new List<ICPreprocessorToken>();
        while (PeekWithNonSignificant() is not { Kind: CPreprocessorTokenType.NewLine or CPreprocessorTokenType.End })
            result.Add(NextWithNonSignificant());

        return Ok(result);
    }

    private ParseResult<ICPreprocessorToken> ParseNewLine()
    {
        if (Peek() is var token and not { Kind: CPreprocessorTokenType.NewLine or CPreprocessorTokenType.End })
            return Error("new-line character or end of stream", token, "new-line");

        var newLine = Next();
        return Ok(newLine);
    }

    // TODO: The following rules are only used in #embed:
    // pp-parameter:
    //  pp-parameter-name pp-parameter-clauseₒₚₜ
    // pp-parameter-name:
    //  pp-standard-parameter
    //  pp-prefixed-parameter
    // pp-standard-parameter:
    //  identifier
    // pp-prefixed-parameter:
    //  identifier :: identifier
    // pp-parameter-clause:
    //  '(' pp-balanced-token-sequenceₒₚₜ ')'
    // pp-balanced-token-sequence:
    //  pp-balanced-token
    //  pp-balanced-token-sequence pp-balanced-token
    // pp-balanced-token:
    //  '(' pp-balanced-token-sequenceₒₚₜ ')'
    //  '[' pp-balanced-token-sequenceₒₚₜ ']'
    //  '{' pp-balanced-token-sequenceₒₚₜ '}'
    //  any pp-token other than a parenthesis, a bracket, or a brace
    // embed-parameter-sequence:
    //  pp-parameter
    //  embed-parameter-sequence pp-parameter

    // TODO: 6.10.1 Conditional inclusion
    // - __has_include
    // - __has_embed
    // - __has_c_attribute

    /// <remarks>Only significant tokens.</remarks>
    private ICPreprocessorToken Next()
    {
        ICPreprocessorToken token;
        do
        {
            token = lexer.Next();
        } while (token is { Kind: CPreprocessorTokenType.WhiteSpace or CPreprocessorTokenType.Comment });

        return token;
    }

    /// <remarks>Only significant tokens.</remarks>
    private ICPreprocessorToken Peek()
    {
        ICPreprocessorToken token;
        var index = 0;
        do
        {
            token = lexer.Peek(index++);
        } while (token is { Kind: CPreprocessorTokenType.WhiteSpace or CPreprocessorTokenType.Comment });

        return token;
    }

    private ICPreprocessorToken? PeekKeyword()
    {
        var token = Peek();
        return token is
        {
            Kind: CPreprocessorTokenType.PreprocessingToken,
            Text: "if"
            or "ifdef"
            or "ifndef"
            or "elif"
            or "elifdef"
            or "elifndef"
            or "else"
            or "endif"
            or "include"
            or "embed"
            or "define"
            or "undef"
            or "line"
            or "error"
            or "warning"
            or "pragma"
        } ? token : null;
    }

    private ICPreprocessorToken NextWithNonSignificant() => lexer.Next();
    private ICPreprocessorToken PeekWithNonSignificant() => lexer.Peek();

    ParseError Error(string expected, ICPreprocessorToken got, string rule) =>
        ParseResult.Error(expected, got, (ErrorLocationInfo)got.Location, rule);

    ParseError Error(string[] expected, ICPreprocessorToken got, string rule) =>
        Error(string.Join(" or ", expected), got, rule);

    /// <remarks>
    /// Offset is set to 0 since we never use the combining operator <c>|</c> here and thus offset is not required.
    /// </remarks>
    private ParseResult<T> Ok<T>(T value, ParseError? furthestError = null) =>
        ParseResult.Ok(value, 0, furthestError);
}
