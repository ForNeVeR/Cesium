using System.Collections.Immutable;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Parser;
using Yoakke.SynKit.Parser.Attributes;

namespace Cesium.Preprocessor;

using ICPreprocessorToken = IToken<CPreprocessorTokenType>;

/// <remarks>C23 Standard, section 6.10 Preprocessing directives.</remarks>
/// TODO: Figure out the final newline treatment. Currently if it is absent then some directives won't be parsed properly.
/// TODO: Figure out the line concatenation using the `\` character.
[Parser(typeof(CPreprocessorTokenType))]
internal class CPreprocessorParser(ILexer<ICPreprocessorToken> Lexer)
{
    public ParseResult<PreprocessingFile> ParsePreprocessingFile()
    {
        var group = new List<IGroupPart>();
        ParseResult<IGroupPart> groupPart;
        while ((groupPart = ParseGroupPart()).IsOk)
        {
            group.Add(groupPart.Ok.Value);
        }

        return ParseResult.Ok(
            new PreprocessingFile(group.ToImmutableArray()),
            groupPart.Ok.Offset,
            groupPart.FurthestError);
    }

    private ParseResult<IGroupPart> ParseGroupPart()
    {
        var ifSection = ParseIfSection();
        if (ifSection.IsOk) return ifSection;

        var controlLine = ParseControlLine();
        if (controlLine.IsOk) return controlLine;

        var textLine = ParseTextLine();
        if (textLine.IsOk) return textLine;

        if (PeekNonWs() is var token and not { Kind: CPreprocessorTokenType.Hash })
            return ParseResult.Error("#", token, Lexer.Position, "group-part");
        _ = NextNonWs();

        return ParseNonDirective();
    }

    private ParseResult<IfSection> ParseIfSection()
    {
        var ifGroup = ParseIfGroup();
        if (!ifGroup.IsOk) return ifGroup.Error;

        var elIfGroups = new List<GuardedGroup>();
        ParseResult<GuardedGroup> elIfGroup;
        while ((elIfGroup = ParseElIfGroup()).IsOk)
        {
            elIfGroups.Add(elIfGroup.Ok.Value);
        }

        var elseGroup = ParseElseGroup();

        var endIfLine = ParseEndIfLine();
        if (!endIfLine.IsOk) return endIfLine.Error; // TODO: Create a mechanism to roll back the lexer to the previous state.

        return ParseResult.Ok(
            new IfSection(ifGroup.Ok.Value, elIfGroups.ToImmutableArray(), elseGroup),
            endIfLine.Ok.Offset,
            endIfLine.FurthestError);
    }

    private ParseResult<GuardedGroup> ParseIfGroup()
    {
        if (PeekNonWs() is var token and not { Kind: CPreprocessorTokenType.Hash })
            return ParseResult.Error("#", token, CurrentPosition, "if-group");
        _ = NextNonWs();

        if (PeekNonWs() is var token and not { Text: "if" or "ifdef" or "ifndef" })
        {
            // TODO: Reset the lexer to the beginning of the line
            return ParseResult.Error("if or ifdef or ifndef", token, CurrentPosition, "if-group");
        }
        var keyword = NextNonWs();


        if (keyword.Text == "if")
        {
            var expression = ParseConstantExpression();
            if (!expression.IsOk) return expression.Error;
        }
        else
        {
            var identifier = ParseIdentifier();
            if (!identifier.IsOk) return identifier.Error;
        }

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return newLine.Error;

        var group = ParseGroup();

        return ParseResult.Ok(
            new GuardedGroup(keyword.Ok, expression, group),
            lastOffset,
            lastSuccessfulToken.FurthestError);
    }

    private ParseResult<GuardedGroup> ParseElIfGroup()
    {
        if (PeekNonWs() is var token and not { Kind: CPreprocessorTokenType.Hash })
            return ParseResult.Error("#", token, CurrentPosition, "elif-group");
        _ = NextNonWs();

        if (PeekNonWs() is var token and not { Text: "elif" or "elifdef" or "elifndef" })
        {
            // TODO: Reset the lexer to the beginning of the line
            return ParseResult.Error("elif or elifdef or elifndef", token, CurrentPosition, "elif-group");
        }

        var keyword = NextNonWs();

        if (keyword.Text == "elif")
        {
            var expression = ParseConstantExpression();
            if (!expression.IsOk) return expression.Error;
        }
        else
        {
            var identifier = ParseIdentifier();
            if (!identifier.IsOk) return identifier.Error;
        }

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return newLine.Error;

        var group = ParseGroup();
        return ParseResult.Ok(
            new GuardedGroup(keyword.Ok, expression, group),
            lastOffset,
            lastSuccessfulToken.FurthestError);
    }

    private ParseResult<GuardedGroup> ParseElseGroup()
    {
        if (PeekNonWs() is var token and not { Kind: CPreprocessorTokenType.Hash })
            return ParseResult.Error("#", token, CurrentPosition, "else-group");
        _ = NextNonWs();

        if (PeekNonWs() is var token and not { Text: "else" })
        {
            // TODO: Reset the lexer to the beginning of the line
            return ParseResult.Error("else", token, CurrentPosition, "elif-group");
        }

        var keyword = NextNonWs();

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return newLine.Error;

        var group = ParseGroup();
        return ParseResult.Ok(
            new GuardedGroup(keyword.Ok, null, group),
            lastOffset,
            lastSuccessfulToken.FurthestError);
    }

    private ParseResult<object?> ParseEndIfLine()
    {
        if (PeekNonWs() is var token and not { Kind: CPreprocessorTokenType.Hash })
            return ParseResult.Error("#", token, CurrentPosition, "endif-line");
        _ = NextNonWs();

        if (PeekNonWs() is var token and not { Text: "endif" })
        {
            // TODO: Reset the lexer to the beginning of the line
            return ParseResult.Error("endif", token, CurrentPosition, "endif-line");
        }
        _ = NextNonWs();

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return newLine.Error;

        return ParseResult.Ok(null, CurrentOffset);
    }

    private ParseResult<IGroupPart> ParseControlLine()
    {
        if (Peek() is var token and not { Kind: CPreprocessorTokenType.Hash })
            return ParseResult.Error("#", token, Lexer.Position, "control-line");
        var hash = Next();

        var include = ParseInclude();
        if (include.IsOk) return include;

        var embed = ParseEmbed();
        if (embed.IsOk) return embed;

        var define = ParseDefine();
        if (define.IsOk) return define;

        var undef = ParseUndef();
        if (undef.IsOk) return undef;

        var line = ParseLine();
        if (line.IsOk) return line;

        var error = ParseError();
        if (error.IsOk) return error;

        var warning = ParseWarning();
        if (warning.IsOk) return warning;

        var pragma = ParsePragma();
        if (pragma.IsOk) return pragma;

        if (Peek() is var token and not { Kind: CPreprocessorTokenType.NewLine })
        {
            // TODO: Reset the lexer to the beginning of the line
            return ParseResult.Error("newline", token, Lexer.Position, "control-line");
        }

        _ = Next();

        return ParseResult.Ok(new EmptyDirective(), CurrentOffset);
    }

    private ParseResult<IncludeDirective> ParseInclude()
    {
        if (Peek() is var token and not { Text: "include" })
            return ParseResult.Error("include", token, Lexer.Position, "include");
        _ = Next();

        var tokens = ParsePPTokens();
        if (!tokens.IsOk) return tokens.Error;

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return newLine.Error;

        return ParseResult.Ok(new IncludeDirective(tokens.Ok.Value), CurrentOffset);
    }

    private ParseResult<EmbedDirective> ParseEmbed()
    {
        if (Peek() is var token and not { Text: "embed" })
            return ParseResult.Error("embed", token, Lexer.Position, "embed");
        _ = Next();

        var tokens = ParsePPTokens();
        if (!tokens.IsOk) return tokens.Error;

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return newLine.Error;

        return ParseResult.Ok(new EmbedDirective(tokens.Ok.Value), CurrentOffset);
    }

    private ParseResult<DefineDirective> ParseDefine()
    {
        if (Peek() is var token and not { Text: "define" })
            return ParseResult.Error("define", token, Lexer.Position, "define");
        _ = Next();

        var identifier = ParseIdentifier();
        if (!identifier.IsOk) return identifier.Error;

        MacroParameters parameters;
        var lParen = ParseLParen();
        if (lParen.IsOk)
        {
            var parameterBlock = ParseMacroParameters();
            if (!parameterBlock.IsOk) return parameterBlock.Error;

            parameters = parameterBlock.Ok.Value;
        }
        else
        {
            parameters = null;
        }

        var replacementList = ParseReplacementList();
        if (!replacementList.IsOk) return replacementList.Error;

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return newLine.Error;

        return ParseResult.Ok(new DefineDirective(identifier.Ok.Value, parameters.Ok.Value, replacementList.Ok.Value), CurrentOffset);
    }

    private ParseResult<MacroParameters> ParseMacroParameters()
    {
        var identifiers = new List<ICPreprocessorToken>();
        var hasEllipsis = false;
        while (true)
        {
            if (identifiers.Count > 0)
            {
                var comma = Next();
                if (comma is not { Text: "," })
                    return ParseResult.Error(",", comma, Lexer.Position, "identifier-list");
            }

            var token = Next();
            switch (token)
            {
                case { Text: "..." }:
                    hasEllipsis = true;
                    break;
                case {Kind: CPreprocessorTokenType.RightParen}:
                    return ParseResult.Ok(
                        new MacroParameters(identifiers.ToImmutableArray(), hasEllipsis),
                        CurrentOffset);
                case {Kind: CPreprocessorTokenType.PreprocessingToken}:
                    if (hasEllipsis) return ParseResult.Error(")", token, Lexer.Position, "identifier-list");
                    identifiers.Add(token);
                    break;
            }
        }
    }

    private ParseResult<UndefDirective> ParseUndef()
    {
        if (Peek() is var token and not { Text: "undef" })
            return ParseResult.Error("undef", token, Lexer.Position, "undef");
        _ = Next();

        var identifier = ParseIdentifier();
        if (!identifier.IsOk) return identifier.Error;

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return newLine.Error;

        return ParseResult.Ok(new UndefDirective(identifier.Ok.Value), CurrentOffset);
    }

    private ParseResult<LineDirective> ParseLine()
    {
        if (Peek() is var token and not { Text: "line" })
            return ParseResult.Error("line", token, Lexer.Position, "line");
        _ = Next();

        var tokens = ParsePPTokens();
        if (!tokens.IsOk) return tokens.Error;

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return newLine.Error;

        return ParseResult.Ok(new LineDirective(tokens.Ok.Value), CurrentOffset);
    }

    private ParseResult<ErrorDirective> ParseError()
    {
        if (Peek() is var token and not { Text: "error" })
            return ParseResult.Error("error", token, Lexer.Position, "error");
        _ = Next();

        var tokens = ParsePPTokens();
        var newLine = ParseNewLine();
        if (!newLine.IsOk) return newLine.Error;

        return ParseResult.Ok(new ErrorDirective(tokens.IsOk ? tokens.Ok.Value : null), CurrentOffset);
    }

    private ParseResult<WarningDirective> ParseWarning()
    {
        if (Peek() is var token and not { Text: "warning" })
            return ParseResult.Error("warning", token, Lexer.Position, "warning");
        _ = Next();

        var tokens = ParsePPTokens();
        if (!tokens.IsOk) return tokens.Error;

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return newLine.Error;

        return ParseResult.Ok(new WarningDirective(tokens.IsOk ? tokens.Ok.Value : null), CurrentOffset);
    }

    private ParseResult<PragmaDirective> ParsePragma()
    {
        if (Peek() is var token and not { Text: "pragma" })
            return ParseResult.Error("pragma", token, Lexer.Position, "pragma");
        _ = Next();

        var tokens = ParsePPTokens();
        if (!tokens.IsOk) return tokens.Error;

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return newLine.Error;

        return ParseResult.Ok(new PragmaDirective(tokens.IsOk ? tokens.Ok.Value : null), CurrentOffset);
    }

    // TODO: Add test about comment preservation in such lines. According to the current implementation, they should
    // not be preserved, even though that's incorrect.
    private ParseResult<TextLine> ParseTextLine()
    {
        var tokens = ParsePPTokens();
        var newLine = ParseNewLine();
        if (!newLine.IsOk) return newLine.Error;

        return ParseResult.Ok(new TextLine(tokens.IsOk ? tokens.Ok.Value : null), CurrentOffset);
    }

    private ParseResult<NonDirective> ParseNonDirective()
    {
        var tokens = ParsePPTokens();
        if (!tokens.IsOk) return tokens.Error;

        var newLine = ParseNewLine();
        if (!newLine.IsOk) return newLine.Error;

        return ParseResult.Ok(new NonDirective(tokens.Ok.Value), CurrentOffset);
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
        if (Peek() is var token and not { Kind: CPreprocessorTokenType.LeftParen })
            return ParseResult.Error("(", token, CurrentPosition, "lparen");

        var lastPrecedingToken = NextWithNonSignificant();
        if (lastPrecedingToken == token) // not preceded by anything
            return ParseResult.Ok(token, CurrentOffset);

        return ParseResult.Error(token, lastPrecedingToken, Lexer.Position, "lparen");
    }

    private ParseResult<IList<ICPreprocessorToken>> ParseReplacementList()
    {
        var ppTokens = ParsePPTokens();
        return ppTokens.IsOk ? ParseResult.Ok(ppTokens.Ok.Value, CurrentOffset) : ParseResult.Ok([], CurrentOffset);
    }

    private ParseResult<IList<ICPreprocessorToken>> ParsePPTokens()
    {
        var result = new List<ICPreprocessorToken>();
        while (Peek() is not { Kind: CPreprocessorTokenType.NewLine })
            result.Add(Next());

        return ParseResult.Ok(result, CurrentOffset);
    }

    private ParseResult<ICPreprocessorToken> ParseNewLine()
    {
        if (Peek() is var token and not { Kind: CPreprocessorTokenType.NewLine })
            return ParseResult.Error("new-line character", token, CurrentPosition, "new-line");

        var newLine = Next();
        return ParseResult.Ok(newLine, CurrentOffset);
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
    private ICPreprocessorToken Next();

    /// <remarks>Only significant tokens.</remarks>
    private ICPreprocessorToken? Peek();

    private ICPreprocessorToken NextWithNonSignificant();
    private ICPreprocessorToken? PeekWithNonSignificant();
}
