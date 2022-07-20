namespace Cesium.CodeGen.Tests;

public class CodeGenTypeTests : CodeGenTestBase
{
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);
        return VerifyTypes(assembly);
    }

    [Fact]
    public Task ConstCharLiteralTest() => DoTest(@"int main()
{
    const char *test = ""hellow"";
}");

    [Fact]
    public Task ConstCharLiteralDeduplication() => DoTest(@"int main()
{
    const char *test1 = ""hellow"";
    const char *test2 = ""hellow1"";
    const char *test3 = ""hellow"";
}");

    [Fact]
    public void AbsentForwardDeclaration() => DoesNotCompile(@"int foo()
{
    return bar();
}

int bar()
{
    return 0;
}", "Function \"bar\" was not found.");

    [Fact]
    public Task FunctionForwardDeclaration() => DoTest(@"int bar(void);

int foo(void)
{
    return bar();
}

int bar(void)
{
    return 0;
}");

    [Fact]
    public Task EmptyFunctionDeclaration() => DoTest(@"
void foo(void)
{
}");

    [Fact]
    public Task SingleFieldStructDefinition() => DoTest("typedef struct { int x; } foo;");

    [Fact]
    public Task TypeDefStructUsage() => DoTest(@"typedef struct { int x; } foo;
int main(void) { foo x; return 0; }");

    [Fact]
    public Task StructUsageWithPointerMemberAccessGet() => DoTest(@"typedef struct { int x; } foo;
int main(void) { foo *x; return x->x; }");

    [Fact]
    public Task StructUsageWithPointerMemberAccessSet() => DoTest(@"typedef struct { int x; } foo;
int main(void) { foo *x; x->x = 42; return 0; }");

    [Fact]
    public Task StructAddressWithPointerMemberAccessGet() => DoTest(@"typedef struct { int x; } foo;
int main(void) { foo x; return (&x)->x; }");

    [Fact]
    public Task StructAddressWithPointerMemberAccessSet() => DoTest(@"typedef struct { int x; } foo;
int main(void) { foo x; (&x)->x = 42; return 0; }");

    [Fact]
    public Task ArrayDeclaration() => DoTest(@"int main()
{
    int i;
    int x[1];
    return 0;
}");
}
