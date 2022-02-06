using Cesium.Test.Framework;
using Yoakke.C.Syntax;

namespace Cesium.Parser.Tests.ParserTests;

public class DeclarationParserTests : ParserTestBase
{
    private static Task DoDeclarationParserTest(string source)
    {
        var lexer = new CLexer(source);
        var parser = new CParser(lexer);

        var result = parser.ParseDeclaration();
        Assert.True(result.IsOk, result.GetErrorString());

        var serialized = JsonSerialize(result.Ok.Value);
        return Verify(serialized);
    }

    [Fact]
    public Task InitializerDeclarationTest() => DoDeclarationParserTest("int x = 0;");

    [Fact]
    public Task MultiDeclaration() => DoDeclarationParserTest("int x = 0, y = 2 + 2;");

    [Fact]
    public Task CliImport() => DoDeclarationParserTest(@"__cli_import(""System.Runtime.InteropServices.Marshal::AllocHGlobal"")
void *malloc(size_t);");
}
