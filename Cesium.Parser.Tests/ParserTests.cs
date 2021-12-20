using System.Text;
using Cesium.Test.Framework;
using Yoakke.C.Syntax;
using Yoakke.Lexer;
using Yoakke.Parser;

namespace Cesium.Parser.Tests;

public class ParserTests : VerifyTestBase
{
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

        var serialized = JsonSerialize(result.Ok.Value);
        return Verify(serialized);
    }

    [Fact]
    public Task MinimalProgramTest() => DoTest("int main() {}");

    [Fact]
    public Task ReturnTest() => DoTest("int main() { return 0; }");

    [Fact]
    public Task ExpressionTest() => DoTest("int main() { return 2 + 2 * 2; }");
}
