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
    private static IGroupPart MakeGroupPart(ICPreprocessorToken _, NonDirective nonDirective) => nonDirective;

    [Rule("if_section: if_group elif_groups? else_group? endif_line")]
    private static IfSection MakeIfSection(GuardedGroup ifGroup, GuardedGroups? elIfGroups, GuardedGroup? elseGroup, EndIfLine _) => new(ifGroup, elIfGroups ?? [], elseGroup);

    [Rule("if_group: '#' 'if' expression new_line group?")]
    private static GuardedGroup MakeIfGroup(
        ICPreprocessorToken hash,
        ICPreprocessorToken @if,
        IPreprocessorExpression expression,
        ICPreprocessorToken newLine,
        Group? group) => new(@if,  expression, group ?? []);

    [Rule("if_group: '#' 'ifdef' identifier new_line group?")]
    private static GuardedGroup MakeIfDefGroup(
        ICPreprocessorToken _,
        ICPreprocessorToken ifDef,
        ICPreprocessorToken identifier,
        ICPreprocessorToken newLine,
        Group? group) => new(ifDef, new IdentifierExpression(identifier.Text), group ?? []);

    [Rule("if_group: '#' 'ifndef' identifier new_line group?")]
    private static GuardedGroup MakeIfNDefGroup(
        ICPreprocessorToken _,
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
        ICPreprocessorToken identifier,
        ICPreprocessorToken newLine,
        Group? group) => new(elIfDef, new IdentifierExpression(identifier.Text), group ?? []);

    [Rule("elifndef_group: '#' 'elifndef' identifier new_line group?")]
    private static GuardedGroup MakeElIfNDefGroup(
        ICPreprocessorToken hash,
        ICPreprocessorToken elIfNDef,
        ICPreprocessorToken identifier,
        ICPreprocessorToken newLine,
        Group? group) => new(elIfNDef, new IdentifierExpression(identifier.Text), group ?? []);

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
    private static IGroupPart MakeInclude(
        ICPreprocessorToken _,
        ICPreprocessorToken include,
        PreprocessorTokens tokens,
        ICPreprocessorToken __) => new IncludeDirective(tokens);

    [Rule("control_line: '#' 'embed' pp_tokens new_line")]
    private static IGroupPart MakeEmbed(
        ICPreprocessorToken _,
        ICPreprocessorToken embed,
        PreprocessorTokens tokens,
        ICPreprocessorToken __) => new EmbedDirective(tokens);

    [Rule("control_line: '#' 'define' identifier replacement_list new_line")]
    private static IGroupPart MakeDefine(
        ICPreprocessorToken _,
        ICPreprocessorToken define,
        ICPreprocessorToken identifier,
        PreprocessorTokens? replacementList,
        ICPreprocessorToken __) =>
        new DefineDirective(identifier, new MacroParameters([], HasEllipsis: false), replacementList ?? []);

    [Rule("control_line: '#' 'define' identifier lparen identifier_list? ')' replacement_list new_line")]
    private static IGroupPart MakeDefine(
        ICPreprocessorToken _,
        ICPreprocessorToken define,
        ICPreprocessorToken identifier,
        ICPreprocessorToken __,
        IdentifierList? parameters,
        ICPreprocessorToken ___,
        PreprocessorTokens? replacementList,
        ICPreprocessorToken ____) =>
        new DefineDirective(identifier, new MacroParameters([], HasEllipsis: false), replacementList ?? []);

    [Rule("control_line: '#' 'define' identifier lparen '...' ')' replacement_list new_line")]
    private static IGroupPart MakeDefine(
        ICPreprocessorToken _,
        ICPreprocessorToken define,
        ICPreprocessorToken identifier,
        ICPreprocessorToken __,
        ICPreprocessorToken ellipsis,
        ICPreprocessorToken ____,
        PreprocessorTokens? replacementList,
        ICPreprocessorToken _____) =>
        new DefineDirective(identifier, new MacroParameters([], HasEllipsis: true), replacementList ?? []);

    [Rule("control_line: '#' 'define' identifier lparen identifier_list ',' '...' ')' replacement_list new_line")]
    private static IGroupPart MakeDefine(
        ICPreprocessorToken _,
        ICPreprocessorToken define,
        ICPreprocessorToken identifier,
        ICPreprocessorToken __,
        IdentifierList parameters,
        ICPreprocessorToken ___,
        ICPreprocessorToken ellipsis,
        ICPreprocessorToken _____,
        PreprocessorTokens? replacementList,
        ICPreprocessorToken ______) =>
        new DefineDirective(identifier, new MacroParameters(parameters, HasEllipsis: true), replacementList ?? []);

    [Rule("control_line: '#' 'undef' identifier new_line")]
    private static IGroupPart MakeUndef(
        ICPreprocessorToken _,
        ICPreprocessorToken undef,
        ICPreprocessorToken identifier,
        ICPreprocessorToken __) => new UndefDirective(identifier);

    [Rule("control_line: '#' 'line' pp_tokens new_line")]
    private static IGroupPart MakeLine(
        ICPreprocessorToken _,
        ICPreprocessorToken line,
        PreprocessorTokens tokens,
        ICPreprocessorToken __) => new LineDirective(tokens);

    [Rule("control_line: '#' 'error' pp_tokens? new_line")]
    private static IGroupPart MakeError(
        ICPreprocessorToken _,
        ICPreprocessorToken error,
        PreprocessorTokens? tokens,
        ICPreprocessorToken __) => new ErrorDirective(tokens);

    [Rule("control_line: '#' 'warning' pp_tokens? new_line")]
    private static IGroupPart MakeWarning(
        ICPreprocessorToken _,
        ICPreprocessorToken warning,
        PreprocessorTokens? tokens,
        ICPreprocessorToken __) => new WarningDirective(tokens);

    [Rule("control_line: '#' 'pragma' pp_tokens? new_line")]
    private static IGroupPart MakePragma(
        ICPreprocessorToken _,
        ICPreprocessorToken pragma,
        PreprocessorTokens? tokens,
        ICPreprocessorToken __) => new PragmaDirective(tokens);

    [Rule("control_line: '#' new_line")]
    private static IGroupPart MakeEmptyDirective(ICPreprocessorToken hash, ICPreprocessorToken newLine) => new EmptyDirective();

    [Rule("text_line: pp_tokens? new_line")]
    private static TextLine MakeTextLine(PreprocessorTokens? tokens, ICPreprocessorToken _) => new(tokens);

    [Rule("non_directive: pp_tokens new_line")]
    private static NonDirective MakeNonDirective(PreprocessorTokens tokens, ICPreprocessorToken _) => new(tokens);

    [Rule("lparen: '('")]
    // TODO: Check that it is not immediately preceded by whitespace, according to the standard
    private static ICPreprocessorToken MakeLParen(ICPreprocessorToken token) => token;

    [Rule("replacement_list: pp_tokens?")]
    private static PreprocessorTokens? MakeReplacementList(PreprocessorTokens? tokens) => tokens;

    [Rule("pp_tokens: PreprocessingToken")]
    private static PreprocessorTokens MakePPTokens(ICPreprocessorToken token) => [token];

    [Rule("pp_tokens: pp_tokens PreprocessingToken")]
    private static PreprocessorTokens MakePPTokens(PreprocessorTokens tokens, ICPreprocessorToken token) => tokens.Add(token);

    [Rule("new_line: NewLine")]
    private static ICPreprocessorToken MakeNewLine(ICPreprocessorToken token) => token;

    [Rule("identifier_list: identifier")]
    private static IdentifierList MakeIdentifierList(ICPreprocessorToken identifier) => [identifier];

    [Rule("identifier_list: identifier_list ',' identifier")]
    private static IdentifierList MakeIdentifierList(
        IdentifierList identifiers,
        ICPreprocessorToken _,
        ICPreprocessorToken identifier) => identifiers.Add(identifier);

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
}
