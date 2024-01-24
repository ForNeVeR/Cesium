using System.Collections.Immutable;
using Yoakke.SynKit.Lexer;

namespace Cesium.Preprocessor;

using ICPreprocessorToken = IToken<CPreprocessorTokenType>;
using Tokens = ImmutableArray<IToken<CPreprocessorTokenType>>;

internal record PreprocessingFile(ImmutableArray<IGroupPart> Groups);

internal interface IGroupPart;
internal record NonDirective(Tokens Tokens) : IGroupPart;

internal record IfSection(GuardedGroup IfGroup, ImmutableArray<GuardedGroup> ElIfGroups, GuardedGroup? ElseGroup) : IGroupPart;
/// <param name="Clause">If <c>null</c> then this is an <c>else</c> clause.</param>
internal record GuardedGroup(
    ICPreprocessorToken Keyword,
    ImmutableArray<ICPreprocessorToken>? Clause,
    ImmutableArray<IGroupPart> Group
);

internal record IncludeDirective(Tokens Tokens) : IGroupPart;
internal record EmbedDirective(Tokens Tokens) : IGroupPart;

internal record MacroParameters(
    Tokens Parameters,
    bool HasEllipsis
);

/// <param name="Parameters">
/// If <c>null</c> then the macro is not a function-like. If empty then it is function-like and requires parens to be
/// called.
/// </param>
internal record DefineDirective(
    ICPreprocessorToken Identifier,
    MacroParameters? Parameters,
    Tokens Replacement
) : IGroupPart;

internal record UndefDirective(ICPreprocessorToken Identifier) : IGroupPart;

internal record LineDirective(Tokens LineNumber) : IGroupPart;

internal record ErrorDirective(Tokens? Tokens) : IGroupPart;
internal record WarningDirective(Tokens? Tokens) : IGroupPart;

internal record PragmaDirective(Tokens? Tokens) : IGroupPart;

internal record EmptyDirective : IGroupPart;

internal record TextLine(Tokens? Tokens) : IGroupPart;
