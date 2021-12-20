using System.Text;
using Yoakke.C.Syntax;
using Yoakke.Lexer;
using Yoakke.Parser;

namespace Cesium.Parser.Tests;

[UsesVerify]
public class ParserTests
{
    static ParserTests()
    {
        // To disable Visual Studio popping up on every test execution.
        Environment.SetEnvironmentVariable("DiffEngine_Disabled", "true");
    }

    private static string? GetErrorString<T>(ParseResult<T> result)
    {
        if (!result.IsError) return null;

        var errorMessage = new StringBuilder();
        var err = result.Error;
        foreach (var element in err.Elements.Values)
        {
            errorMessage.AppendLine($"expected {string.Join(" or ", element.Expected)} while parsing {element.Context}");
        }

        var got = err.Got switch
        {
            null => "end of input",
            IToken<CTokenType> t => $"{t.Kind}: {t.Text}",
            var o => o.ToString()
        };
        errorMessage.AppendLine($" but got {got}");

        return errorMessage.ToString();
    }

    private static Task DoTest(string source)
    {
        var parser = new CParser(new CLexer(source));

        var result = parser.ParseTranslationUnit();
        Assert.True(result.IsOk, GetErrorString(result));

        return Verify(result.Ok.Value);
    }

    [Fact]
    public Task MinimalProgramTest() => DoTest("int main() {}");

    [Fact]
    public Task ReturnTest() => DoTest("int main() { return 0; }");

    [Fact]
    public Task ExpressionTest() => DoTest("int main() { return 2 + 2 * 2; }");
}
