using Cesium.TestFramework;
using Yoakke.SynKit.C.Syntax;

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
        return Verify(serialized, GetSettings());
    }

    [Fact]
    public Task InitializerDeclarationTest() => DoDeclarationParserTest("int x = 0;");

    [Fact]
    public Task InitializerHexDeclarationTest() => DoDeclarationParserTest("int x = 0x22;");

    [Fact]
    public Task MultiDeclaration() => DoDeclarationParserTest("int x = 0, y = 2 + 2;");

    [Fact]
    public Task ArrayDeclaration() => DoDeclarationParserTest("int x[100];");

    [Fact]
    public Task ArrayDeclarationWithInitializer() => DoDeclarationParserTest("int x[4] = { 1, 2, 3, 4 };");

    [Fact]
    public Task ArrayDeclarationWithInitializerWithTrailingComma() => DoDeclarationParserTest("int x[4] = { 1, 2, 3, 4, };");

    [Fact]
    public Task EmptyStructDeclarationWithInitializer() => DoDeclarationParserTest("Token head = {};");

    [Fact]
    public Task StructTypeVariableDeclaration() => DoDeclarationParserTest("struct Foo x;");

    [Fact]
    public Task CliImport() => DoDeclarationParserTest(@"__cli_import(""System.Runtime.InteropServices.Marshal::AllocHGlobal"")
void *malloc(size_t);");

    [Fact]
    public Task FunctionTypeDef() => DoDeclarationParserTest("typedef void foo(int);");

    [Fact]
    public Task EnumTypeDef() => DoDeclarationParserTest(@"typedef enum {
  TK_PUNCT,
  TK_NUM,
  TK_EOF
} TokenKind;");

    [Fact]
    public Task EnumTypeDefTrailingComma() => DoDeclarationParserTest(@"typedef enum {
  TK_PUNCT,
  TK_NUM,
  TK_EOF,
} TokenKind;");

    [Fact]
    public Task StructShortTypeDeclaration() => DoDeclarationParserTest("typedef struct Foo Foo;");

    [Fact]
    public Task FunctionPointerTypeDef() => DoDeclarationParserTest("typedef void (*foo)(int);");

    [Fact]
    public Task ComplexFunctionPointerTypeDef() => DoDeclarationParserTest("typedef void (*foo)(uint64_t, const uint32_t*);");

    [Fact]
    public Task StructWithArray() => DoDeclarationParserTest(@"typedef struct {
    int x[4];
} foo;");

    [Fact]
    public Task StructTypeDeclaration() => DoDeclarationParserTest(@"struct Foo { int A; };");

    [Fact]
    public Task EnumDeclaration() => DoDeclarationParserTest(@"enum Colour { Red, Green, Blue };");

    [Fact]
    public Task EnumVariableDeclaration() => DoDeclarationParserTest(@"enum Colour x;");
}
