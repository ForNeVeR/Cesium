using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Text;
using Range = Yoakke.SynKit.Text.Range;

namespace Cesium.Preprocessor.Tests;

internal sealed class TokenBuilder
{
    private Range? _range;
    private Location? _location;
    private string? _text;
    private CPreprocessorTokenType? _kind;

    public TokenBuilder WithRange(Range range = new())
    {
        _range = range;
        return this;
    }

    public TokenBuilder WithLocation(Location location = new())
    {
        _location = location;
        return this;
    }

    public TokenBuilder WithText(string text = "")
    {
        _text = text;
        return this;
    }

    public TokenBuilder WithKind(CPreprocessorTokenType kind)
    {
        _kind = kind;
        return this;
    }

    public Token<CPreprocessorTokenType> Build()
    {
        return new Token<CPreprocessorTokenType>(
            _range ?? throw new ArgumentException(),
            _location ?? throw new ArgumentException(),
            _text ?? throw new ArgumentException(),
            _kind ?? throw new ArgumentException());
    }
}
