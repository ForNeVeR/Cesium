using Cesium.Core;
using Cesium.Preprocessor;
using Cesium.Test.Framework;

namespace Cesium.Parser.Tests.PreprocessorTests;

public class PreprocessorTests : VerifyTestBase
{
    private static async Task DoTest(string source, Dictionary<string, string>? standardHeaders = null, Dictionary<string, string?>? defines = null)
    {
        var lexer = new CPreprocessorLexer(source);
        var includeContext = new IncludeContextMock(standardHeaders ?? new Dictionary<string, string>());
        var definesContext = new InMemoryDefinesContext(defines ?? new Dictionary<string, string?>());
        var preprocessor = new CPreprocessor(lexer, includeContext, definesContext);
        var result = await preprocessor.ProcessSource();
        await Verify(result, GetSettings());
    }

    [Fact]
    public Task IdentityTest() => DoTest(@"int main(void)
{
    return 2 + 2;
}");

    [Fact]
    public Task Include() => DoTest(@"#include <foo.h>
int test()
{
    #include <bar.h>
}", new() { ["foo.h"] = "void foo() {}", ["bar.h"] = "int bar = 0;" });

    [Fact]
    public Task Include2() => DoTest(@"#include <foo.h>
#include <bar.h>
int test()
{
}", new() { ["foo.h"] = "void foo() {}", ["bar.h"] = "int bar = 0;" });

    [Fact]
    public Task IncludeTrailingWhiltespacesIgnored() => DoTest($@"#include <foo.h>{"\t\t\t" /*Make whitespaces visible here */}
#include <bar.h>
int test()
{{
}}", new() { ["foo.h"] = "void foo() {}", ["bar.h"] = "int bar = 0;" });

    [Fact]
    public Task NestedIncludes() => DoTest(@"#include <foo.h>

int test()
{
}", new() { ["foo.h"] = "#include <bar.h>", ["bar.h"] = "int bar = 0;" });

    [Fact]
    public Task NestedIncludes2() => DoTest(@"#include <foo.h>

int test()
{
}", new() { ["foo.h"] = "#include <bar.h>", ["bar.h"] = "#include <baz.h>", ["baz.h"] = "int bar = 0;" });

    [Fact]
    public async Task ErrorMsg()
    {
        PreprocessorException err = await Assert.ThrowsAsync<PreprocessorException>(async () => await DoTest(
@"#error ""Error message"" test
int test()
{}"
        ));
        Assert.Equal(@"Error: ""Error message"" test", err.Message);
    }

    [Fact]
    public Task IfDefinedLiteral() => DoTest(
@"#define foo main
#ifdef foo
int foo() { return 0; }
#endif
");

    [Fact]
    public Task IfElseLiteral() => DoTest(
@"#define foo main
#ifndef foo
int myfoo() { return 0; }
#else
int foo() { return 0; }
#endif
");

    [Fact]
    public Task NestedIfDefined() => DoTest(
@"#define foo main
#ifdef foo
int foo() { return 0; }
#ifdef xfoo
int foo() { return 0; }
#endif
#endif
");

    [Fact]
    public Task IfNotDefinedLiteral() => DoTest(
@"#define foo main
#ifndef foo
int foo() { return 0; }
#endif
");

    [Fact]
    public Task ReplaceSeparatedIdentifier() => DoTest(
@"#define foo int
foo main() { return 0; }
");

    [Fact]
    public Task ReplaceNumber() => DoTest(
@"#define foo 0
int main() { return foo; }
");

    [Fact]
    public Task ReplaceFunctionParameter() => DoTest(
@"#define foo 0
int main() { return abs(foo); }
");

    [Fact]
    public Task IfExpressionDefinedLiteral() => DoTest(
@"#define mycondition
#if mycondition
int foo() { return 0; }
#endif
");

    [Fact]
    public Task IfExpressionEqualsLiteral() => DoTest(
@"#define mycondition 1
#if mycondition == 1
int foo() { return 0; }
#endif
");

    [Fact]
    public Task IfExpressionNotEqualsLiteral() => DoTest(
@"#define mycondition 2
#if mycondition != 1
int foo() { return 0; }
#endif
");
}
