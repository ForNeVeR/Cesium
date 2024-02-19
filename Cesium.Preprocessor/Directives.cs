using System.Collections.Immutable;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Text;

namespace Cesium.Preprocessor;

using ICPreprocessorToken = IToken<CPreprocessorTokenType>;
using Tokens = ImmutableArray<IToken<CPreprocessorTokenType>>;

internal record PreprocessingFile(ImmutableArray<IGroupPart> Group);

internal interface IGroupPart
{
    Location Location { get; }
    ICPreprocessorToken? Keyword { get; }
}

internal record NonDirective(Location Location) : IGroupPart
{
    public ICPreprocessorToken? Keyword => null;
}

internal record IfSection(GuardedGroup IfGroup, ImmutableArray<GuardedGroup> ElIfGroups, GuardedGroup? ElseGroup)
    : IGroupPart
{
    public ICPreprocessorToken Keyword => IfGroup.Keyword;
    public Location Location => Keyword.Location;
}

/// <param name="Clause">If <c>null</c> then this is an <c>else</c> clause.</param>
internal record GuardedGroup(
    ICPreprocessorToken Keyword,
    ImmutableArray<ICPreprocessorToken>? Clause,
    ImmutableArray<IGroupPart> Tokens
);

internal record IncludeDirective(Location Location, ICPreprocessorToken Keyword, Tokens Tokens) : IGroupPart;
internal record EmbedDirective(Location Location, ICPreprocessorToken Keyword, Tokens Tokens) : IGroupPart;

/// <remarks>
/// In most cases, this may be <c>null</c> which means the macro is defined without parameters, an object-like macro.
/// </remarks>
public record MacroParameters(
    Tokens Parameters,
    bool HasEllipsis
);

/// <param name="Parameters">
/// If <c>null</c> then the macro is not a function-like. If empty then it is function-like and requires parens to be
/// called.
/// </param>
internal record DefineDirective(
    Location Location,
    ICPreprocessorToken Keyword,
    ICPreprocessorToken Identifier,
    MacroParameters? Parameters,
    Tokens Replacement
) : IGroupPart;

internal record UnDefDirective(
    Location Location,
    ICPreprocessorToken Keyword,
    ICPreprocessorToken Identifier
) : IGroupPart;

// TODO[#77]: Support this directive
internal record LineDirective(Location Location, ICPreprocessorToken Keyword, Tokens LineNumber) : IGroupPart;

internal record ErrorDirective(Location Location, ICPreprocessorToken Keyword, Tokens? Tokens) : IGroupPart;
internal record WarningDirective(Location Location, ICPreprocessorToken Keyword, Tokens? Tokens) : IGroupPart;

internal record PragmaDirective(Location Location, ICPreprocessorToken Keyword, Tokens? Tokens) : IGroupPart;

internal record EmptyDirective(Location Location) : IGroupPart
{
    public ICPreprocessorToken? Keyword => null;
}

internal record TextLineBlock(Location Location, Tokens Tokens) : IGroupPart
{
    public ICPreprocessorToken? Keyword => null;
}
