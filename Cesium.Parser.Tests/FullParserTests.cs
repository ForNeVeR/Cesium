using Cesium.Test.Framework;
using Yoakke.C.Syntax;

namespace Cesium.Parser.Tests;

public class FullParserTests : ParserTestBase
{
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

    [Fact]
    public Task SimpleVariableTest() => DoTest("void foo() { int x = 0; }");

    [Fact]
    public Task VariableTest() => DoTest("int main() { int x = 0; return x + 1; }");

    [Fact]
    public Task VariableArithmeticTest() => DoTest(@"int main()
{
    int x = 0;
    x = x + 1;
    return x + 1;
 }");
}
