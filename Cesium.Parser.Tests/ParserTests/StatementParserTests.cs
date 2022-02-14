using Cesium.Test.Framework;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.Parser.Tests.ParserTests;

public class StatementParserTests : ParserTestBase
{
    private static Task DoTest(string source)
    {
        var lexer = new CLexer(source);
        var parser = new CParser(lexer);

        var result = parser.ParseStatement();
        Assert.True(result.IsOk, result.GetErrorString());

        var serialized = JsonSerialize(result.Ok.Value);
        return Verify(serialized);
    }

    [Fact]
    public Task ReturnArithmetic() => DoTest("return 2 + 2 * 2;");

    [Fact]
    public Task ReturnVariableArithmetic() => DoTest("return x + 2 * 2;");

    [Fact]
    public Task CompoundStatementWithVariable() => DoTest("{ int x = 0; }");
}
