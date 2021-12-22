using Cesium.Test.Framework;
using Yoakke.C.Syntax;

namespace Cesium.Parser.Tests.ParserTests;

public class DeclarationParserTests : ParserTestBase
{
    [Fact]
    public Task InitializerDeclarationTest()
    {
        const string source = "int x = 0;";
        var lexer = new CLexer(source);
        var parser = new CParser(lexer);

        var result = parser.ParseDeclaration();
        Assert.True(result.IsOk, GetErrorString(result));

        var serialized = JsonSerialize(result.Ok.Value);
        return Verify(serialized);
    }
}
