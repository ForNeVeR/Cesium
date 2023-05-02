using Cesium.Core;
using Cesium.Preprocessor;
using Cesium.Test.Framework;
using Yoakke.SynKit.Lexer;

namespace Cesium.Parser.Tests.PreprocessorTests;

public class PreprocessorTests : VerifyTestBase
{
    private static async Task DoTest(string source, Dictionary<string, string>? standardHeaders = null, Dictionary<string, IList<IToken<CPreprocessorTokenType>>>? defines = null)
    {
        string result = await DoPreprocess(source, standardHeaders, defines);
        await Verify(result, GetSettings());
    }

    private static async Task<string> DoPreprocess(string source, Dictionary<string, string>? standardHeaders = null, Dictionary<string, IList<IToken<CPreprocessorTokenType>>>? defines = null)
    {
        var lexer = new CPreprocessorLexer(source);
        var includeContext = new IncludeContextMock(standardHeaders ?? new Dictionary<string, string>());
        var definesContext = new InMemoryDefinesContext(defines ?? new Dictionary<string, IList<IToken<CPreprocessorTokenType>>>());
        var preprocessor = new CPreprocessor(source, lexer, includeContext, definesContext);
        var result = await preprocessor.ProcessSource();
        return result;
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
    public Task IncludeNoWhitespaces() => DoTest(@"#include<foo.h>
int test()
{
    foo();
}", new() { ["foo.h"] = "void foo() {}" });

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
    public Task PragmaOnce() => DoTest(@"
int test()
{
#include <foo.h>
#include <foo.h>
}", new() { ["foo.h"] = "#pragma once\nprintfn();" });

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
    public Task ReplaceWithParameter() => DoTest(
@"#define foo(x) x
int main() { return foo(0); }
");

    [Fact]
    public Task ReplaceWithMultipleParameters() => DoTest(
@"#define foo(x,y,z) (y,z,x)
int x,test;
int main() { return foo(11,x,test); }
");

    [Fact]
    public Task ReplaceWithHash() => DoTest(
@"#define foo(x) #x
int main() { char* x = foo(0); }
");

    [Fact]
    public Task ReplaceWithHashNested() => DoTest(
@"#define foo(x) #x
int main() { char* x = foo(int x; printf(""some string"")); }
");

    [Fact]
    public Task IfExpressionCannotConsumeNonInteger()
    {
        return Assert.ThrowsAsync<PreprocessorException>(() => DoPreprocess(
@"#define mycondition
#if mycondition
int foo() { return 0; }
#endif
"));
    }

    [Fact]
    public Task IfExpressionEqualsLiteral() => DoTest(
@"#define mycondition 1
#if mycondition == 1
int foo() { return 0; }
#endif
");

    [Fact]
    public Task IfExpressionEqualsSkippedIfNotMetLiteral() => DoTest(
@"#define mycondition 0
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

    [Fact]
    public Task IfExpressionNotEqualsSkippedIfNotMetLiteral() => DoTest(
@"#define mycondition 1
#if mycondition != 1
int foo() { return 0; }
#endif
");

    [Fact]
    public Task IfExpressionDefined() => DoTest(
@"#define mycondition
#if defined mycondition
int foo() { return 0; }
#endif
");

    [Fact]
    public Task IfExpressionNotDefinedFunction() => DoTest(
@"#if !defined(not_existing_condition)
int foo() { return 0; }
#endif
");

    [Fact]
    public Task IfExpressionNotDefined() => DoTest(
@"#define mycondition
#if (!defined mycondition)
int foo() { return 0; }
#endif
");

    [Fact]
    public Task IfExpressionMultiline() => DoTest(
@"#define mycondition
#if (!defined \
    mycondition)
int foo() { return 0; }
#endif
");

    [Fact]
    public Task IfExpressionOr() => DoTest(
@"#define mycondition 0
#define mycondition2 1
#if mycondition || mycondition2
int foo() { return 0; }
#endif
");

    [Fact]
    public Task IfExpressionAnd() => DoTest(
@"#define mycondition 0
#define mycondition2 1
#if mycondition && mycondition2
int foo() { return 0; }
#endif
");

    [Fact]
    public Task UndefMacro() => DoTest(
@"#define mycondition 1
#undef mycondition
#if !(defined mycondition)
int foo() { return 0; }
#endif
");
}
