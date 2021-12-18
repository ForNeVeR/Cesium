using Cesium.Lexer;
using Xunit;
using Xunit.Abstractions;
using Yoakke.Lexer;

namespace Cesium.Parser.Tests;

public class LexerTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public LexerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void SimpleTest()
    {
        var source = "int main() {}";
        var lexer = new CLexer(source);
        var stream = lexer.ToStream();
        while (stream.TryConsume(out var token) && token.Kind != TokenType.End)
        {
            _testOutputHelper.WriteLine(token.ToString());
        }
    }
}
