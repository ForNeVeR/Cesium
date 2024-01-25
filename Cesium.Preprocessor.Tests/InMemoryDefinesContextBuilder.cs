using Yoakke.SynKit.Lexer;

namespace Cesium.Preprocessor.Tests;

internal sealed class InMemoryDefinesContextBuilder
{
    private readonly Dictionary<string, IList<IToken<CPreprocessorTokenType>>> _initialDefines = new();

    public InMemoryDefinesContextBuilder WithDefineMacro(
        string defineName,
        IList<IToken<CPreprocessorTokenType>> tokens)
    {
        _initialDefines[defineName] = tokens;
        return this;
    }

    public InMemoryDefinesContext Build()
    {
        return new InMemoryDefinesContext(_initialDefines.AsReadOnly());
    }
}
