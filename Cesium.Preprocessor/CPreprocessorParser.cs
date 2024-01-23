using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Parser.Attributes;

namespace Cesium.Preprocessor;

using ICPreprocessorToken = IToken<CPreprocessorTokenType>;
using Group = ImmutableArray<IGroupPart>;
using GuardedGroups = ImmutableArray<GuardedGroup>;
using PreprocessorTokens = ImmutableArray<IToken<CPreprocessorTokenType>>;
using IdentifierList = ImmutableArray<IToken<CPreprocessorTokenType>>;

/// <remarks>C23 Standard, section 6.10 Preprocessing directives.</remarks>
[Parser(typeof(CPreprocessorTokenType))]
[SuppressMessage("ReSharper", "UnusedParameter.Local")] // parser parameters are mandatory even if unused
internal partial class CPreprocessorParser
{
    [Rule("preprocessing_file: group?")]
    private static PreprocessingFile MakePreprocessingFile(Group? group) => new(group ?? []);

    [Rule("group: group_part")]
    private static Group MakeGroup(IGroupPart groupPart) => [groupPart];

    [Rule("group: group group_part")]
    private static Group MakeGroup(Group groups, IGroupPart groupPart) => groups.Add(groupPart);

    [Rule("group_part: if_section")]
    [Rule("group_part: control_line")]
    [Rule("group_part: text_line")]
    private static IGroupPart MakeGroupPart(IGroupPart groupPart) => groupPart;

    [Rule("group_part: '#' non_directive")]
    private static NonDirective MakeGroupPart(ICPreprocessorToken _, NonDirective nonDirective) => nonDirective;

    [Rule("if_section: if_group elif_groups? else_group? endif_line")]
    private static IfSection MakeIfSection(GuardedGroup ifGroup, GuardedGroups? elIfGroups, GuardedGroup? elseGroup, EndIfLine _) => new(ifGroup, elIfGroups ?? [], elseGroup);

    [Rule("if_group: '#' 'if' expression new_line group?")]
    private static GuardedGroup MakeIfGroup(
        ICPreprocessorToken hash,
        ICPreprocessorToken @if,
        IPreprocessorExpression expression,
        ICPreprocessorToken newLine,
        Group? group) => new(@if,  expression, group ?? []);

    [Rule("ifdef identifier new_line group?")]
    private static GuardedGroup MakeIfDefGroup(
        ICPreprocessorToken ifDef,
        ICPreprocessorToken identifier,
        ICPreprocessorToken newLine,
        Group? group) => new(ifDef, new IdentifierExpression(identifier.Text), group ?? []);

    [Rule("ifndef identifier new_line group?")]
    private static GuardedGroup MakeIfNDefGroup(
        ICPreprocessorToken ifNDef,
        ICPreprocessorToken identifier,
        ICPreprocessorToken newLine,
        Group? group) => new(ifNDef, new IdentifierExpression(identifier.Text), group ?? []);

    [Rule("elif_groups: elif_group")]
    private static GuardedGroups MakeElIfGroups(GuardedGroup elIfGroup) => [elIfGroup];

    [Rule("elif_groups: elif_groups elif_group")]
    private static GuardedGroups MakeElIfGroups(GuardedGroups elIfGroups, GuardedGroup elIfGroup) =>
        elIfGroups.Add(elIfGroup);

    [Rule("elif_group: '#' 'elif' expression new_line group?")]
    private static GuardedGroup MakeElIfGroup(
        ICPreprocessorToken hash,
        ICPreprocessorToken elIf,
        IPreprocessorExpression expression,
        ICPreprocessorToken newLine,
        Group? group) => new(elIf, expression, group ?? []);

    [Rule("elifdef_group: '#' 'elifdef' identifier new_line group?")]
    private static GuardedGroup MakeElIfDefGroup(
        ICPreprocessorToken hash,
        ICPreprocessorToken elIfDef,
        IPreprocessorExpression identifier,
        ICPreprocessorToken newLine,
        Group? group) => new(elIfDef, identifier, group ?? []);

    [Rule("elifndef_group: '#' 'elifndef' identifier new_line group?")]
    private static GuardedGroup MakeElIfNDefGroup(
        ICPreprocessorToken hash,
        ICPreprocessorToken elIfNDef,
        IPreprocessorExpression identifier,
        ICPreprocessorToken newLine,
        Group? group) => new(elIfNDef, identifier, group ?? []);

    [Rule("else_group: '#' 'else' new_line group?")]
    private static GuardedGroup MakeElseGroup(
        ICPreprocessorToken hash,
        ICPreprocessorToken @else,
        ICPreprocessorToken newLine,
        Group? group) => new(@else, null, group ?? []);

    [Rule("endif_line: '#' 'endif' new_line")]
    private static EndIfLine MakeEndIfLine(
        ICPreprocessorToken hash,
        ICPreprocessorToken endIf,
        ICPreprocessorToken newLine) => new();

    [Rule("control_line: '#' 'include' pp_tokens new_line")]
    private static IncludeDirective MakeInclude(
        ICPreprocessorToken _,
        ICPreprocessorToken include,
        PreprocessorTokens tokens,
        ICPreprocessorToken __) => new(tokens);

    [Rule("control_line: '#' 'embed' pp_tokens new_line")]
    private static EmbedDirective MakeEmbed(
        ICPreprocessorToken _,
        ICPreprocessorToken embed,
        PreprocessorTokens tokens,
        ICPreprocessorToken __) => new(tokens);

    [Rule("control_line: '#' 'define' identifier replacement_list new_line")]
    private static DefineDirective MakeDefine(
        ICPreprocessorToken _,
        ICPreprocessorToken define,
        ICPreprocessorToken identifier,
        PreprocessorTokens replacement,
        ICPreprocessorToken __) => new(identifier, [], replacement);

    [Rule("control_line: '#' 'define' identifier lparen identifier_list? ')' replacement_list new_line")]
    private static DefineDirective MakeDefine(
        ICPreprocessorToken _,
        ICPreprocessorToken define,
        ICPreprocessorToken identifier,
        ICPreprocessorToken __,
        IdentifierList? parameters,
        ICPreprocessorToken ___,
        PreprocessorTokens replacementList,
        ICPreprocessorToken ____) => new(identifier, parameters ?? [], replacementList);

    [Rule("control_line: '#' 'define' identifier lparen '...' ')' replacement_list new_line")]
    private static DefineDirective MakeDefine(
        ICPreprocessorToken _,
        ICPreprocessorToken define,
        ICPreprocessorToken identifier,
        ICPreprocessorToken __,
        ICPreprocessorToken ellipsis,
        ICPreprocessorToken ____,
        PreprocessorTokens replacementList,
        ICPreprocessorToken _____) => new(identifier, new MacroParameters([], HasEllipsis: true), replacementList);

    [Rule("control_line: '#' 'define' identifier lparen identifier_list ',' '...' ')' replacement_list new_line")]
    private static DefineDirective MakeDefine(
        ICPreprocessorToken _,
        ICPreprocessorToken define,
        ICPreprocessorToken identifier,
        ICPreprocessorToken __,
        IdentifierList parameters,
        ICPreprocessorToken ___,
        ICPreprocessorToken ellipsis,
        ICPreprocessorToken _____,
        PreprocessorTokens replacementList,
        ICPreprocessorToken ______) =>
        new(identifier, new MacroParameters(parameters, HasEllipsis: true), replacementList);

    [Rule("control_line: '#' 'undef' identifier new_line")]
    private static UndefDirective MakeUndef(
        ICPreprocessorToken _,
        ICPreprocessorToken undef,
        ICPreprocessorToken identifier,
        ICPreprocessorToken __) => new(identifier);

    [Rule("control_line: '#' 'line' pp_tokens new_line")]
    private static LineDirective MakeLine(
        ICPreprocessorToken _,
        ICPreprocessorToken line,
        PreprocessorTokens tokens,
        ICPreprocessorToken __) => new(tokens);

    [Rule("control_line: '#' 'error' pp_tokens? new_line")]
    private static ErrorDirective MakeError(
        ICPreprocessorToken _,
        ICPreprocessorToken error,
        PreprocessorTokens? tokens,
        ICPreprocessorToken __) => new(tokens);

    [Rule("control_line: '#' 'warning' pp_tokens? new_line")]
    private static WarningDirective MakeWarning(
        ICPreprocessorToken _,
        ICPreprocessorToken warning,
        PreprocessorTokens? tokens,
        ICPreprocessorToken __) => new(tokens);

    [Rule("control_line: '#' 'pragma' pp_tokens? new_line")]
    private static PragmaDirective MakePragma(
        ICPreprocessorToken _,
        ICPreprocessorToken pragma,
        PreprocessorTokens? tokens,
        ICPreprocessorToken __) => new(tokens);

    [Rule("control_line: '#' new_line")]
    private static EmptyDirective MakeEmptyDirective(ICPreprocessorToken hash, ICPreprocessorToken newLine) => new();

    [Rule("text_line: pp_tokens? new_line")]
    private static TextLine MakeTextLine(PreprocessorTokens? tokens, ICPreprocessorToken _) => new(tokens);

    [Rule("non_directive: pp_tokens new_line")]
    private static NonDirective MakeNonDirective(PreprocessorTokens tokens, ICPreprocessorToken _) => new(tokens);

    [Rule("lparen: '('")]
    // TODO: Check that it is not immediately preceded by whitespace, according to the standard
    private static ICPreprocessorToken MakeLParen(ICPreprocessorToken token) => token;

// TODO: replacement-list:
// TODO: pp-tokensopt
// TODO: 162 Language § 6.10
// TODO: N3096 working draft — April 1, 2023 ISO/IEC 9899:2023 (E)
// TODO: pp-tokens:
// TODO: preprocessing-token
// TODO: pp-tokens preprocessing-token
// TODO: new-line:
// TODO: the new-line character
// TODO: identifier-list:
// TODO: identifier
// TODO: identifier-list , identifier
// TODO: pp-parameter:
// TODO: pp-parameter-name pp-parameter-clauseopt
// TODO: pp-parameter-name:
// TODO: pp-standard-parameter
// TODO: pp-prefixed-parameter
// TODO: pp-standard-parameter:
// TODO: identifier
// TODO: pp-prefixed-parameter:
// TODO: identifier :: identifier
// TODO: pp-parameter-clause:
// TODO: ( pp-balanced-token-sequenceopt )
// TODO: pp-balanced-token-sequence:
// TODO: pp-balanced-token
// TODO: pp-balanced-token-sequence pp-balanced-token
// TODO: pp-balanced-token:
// TODO: ( pp-balanced-token-sequenceopt )
// TODO: [ pp-balanced-token-sequenceopt ]
// TODO: { pp-balanced-token-sequenceopt }
// TODO: any pp-token other than a parenthesis, a bracket, or a brace
// TODO: embed-parameter-sequence:
// TODO: pp-parameter
// TODO: embed-parameter-sequence pp-parameter
}
