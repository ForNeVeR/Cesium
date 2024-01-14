using Cesium.TestFramework;
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
        return Verify(serialized, GetSettings());
    }

    [Fact]
    public Task ReturnArithmetic() => DoTest("return 2 + 2 * 2;");

    [Fact]
    public Task ReturnVariableArithmetic() => DoTest("return x + 2 * 2;");

    [Fact]
    public Task CompoundStatementWithVariable() => DoTest("{ int x = 0; }");

    [Fact]
    public Task BitArithmetic() => DoTest("return ~1 << 2 >> 3 | 4 & 5 ^ 6;");

    [Fact]
    public Task IfStatement() => DoTest("if (1) { int x = 0; }");

    [Fact]
    public Task IfElseStatement() => DoTest("if (1) { int x = 0; } else { int y = 1; }");

    [Fact]
    public Task NestedIfs() => DoTest(@"
if (1)
    if (2) {
        int x = 0;
    } else {
        int y = 1;
    }
");

    [Fact]
    public Task RelationalOperators() => DoTest("return 1 > 2 < 4 <= 5 >= 6;");

    [Fact]
    public Task EqualityOperators() => DoTest("return 1 == 2 != 3;");

    [Fact]
    public Task LogicalAndOperator() => DoTest("return 1 && 2;");

    [Fact]
    public Task LogicalOrOperator() => DoTest("return 1 || 2;");

    [Fact]
    public Task ConditionalOperator() => DoTest("return 1 ? 2 ? 3 : 4 ? 5 : 6 : 7;");

    [Fact]
    public Task ForStatement_Full() => DoTest("for (i = 1; i < 0; ++i) ++i;");

    [Fact]
    public Task ForStatement_NoInit() => DoTest("for (; i < 0; ++i) ++i;");

    [Fact]
    public Task ForStatement_NoTest() => DoTest("for (i = 1;; ++i) ++i;");

    [Fact]
    public Task ForStatement_NoUpdate() => DoTest("for (i = 1; i < 0;) ++i;");

    [Fact]
    public Task ForStatement_OnlyInit() => DoTest("for (i = 1;;) ++i;");

    [Fact]
    public Task ForStatement_OnlyCondition() => DoTest("for (; i < 0;) ++i;");

    [Fact]
    public Task ForStatement_OnlyUpdate() => DoTest("for (;; ++i) ++i;");

    [Fact]
    public Task ForStatement_Empty() => DoTest("for (;;) ++i;");

    [Fact]
    public Task ForStatement_MultiLineBody() => DoTest(@"for (i = 1; i < 0; ++i) {
    i = i - 1;
    i = i + 2;
}");

    [Fact]
    public Task ArrayAssigment() => DoTest(@"{
    int a[1];
    a[0] = 0;
}");

    [Fact]
    public Task AddressOfInParens() => DoTest("(&x)->x = 42;");

    [Fact]
    public Task NotFuncCall() => DoTest("if (!test()) return 1;");

    [Fact]
    public Task IndirectionGet() => DoTest("x = *y;");

    [Fact]
    public Task IndirectionSet() => DoTest("*x = 42;");

    [Fact]
    public Task TypeCast() => DoTest("(int)42;");

    [Fact]
    public Task SwitchStatement_Empty() => DoTest(@"switch(x) { }");

    [Fact]
    public Task SwitchStatement_OneCase() => DoTest(@"switch(x) { case 0: break; }");

    [Fact]
    public Task SwitchStatement_MultiCases() => DoTest(@"switch(x) {
    case 0: break;
    case 1: break;
}");

    [Fact]
    public Task SwitchStatement_MultiCasesWithDefault() => DoTest(@"switch(x) {
    case 0: break;
    case 1: break;
    default: break;
}");

    [Fact]
    public Task SwitchStatement_FallthroughCase() => DoTest(@"switch(x) {
    case 0: break;
    case 1:
    default: break;
}");

    [Fact]
    public Task SizeOfVariable() => DoTest("{int size; return sizeof size;}");

    [Fact]
    public Task SizeOfIdentifier() => DoTest("{int size; return sizeof(size);}");

    [Fact]
    public Task SizeOfTypeName() => DoTest("return sizeof(int);");
}
