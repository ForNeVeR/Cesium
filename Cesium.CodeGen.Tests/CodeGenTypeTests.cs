using Cesium.TestFramework;
using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenTypeTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source, string @namespace = "", string globalTypeFqn = "")
    {
        var assembly = GenerateAssembly(default, @namespace: @namespace, globalTypeFqn: globalTypeFqn, sources: source);
        return VerifyTypes(assembly);
    }

    [Fact]
    public Task GlobalVariableTest() => DoTest(@"int x = 50;

int main()
{
    x = x + 1;
    return x;
}",
        "", "TestClass");
    [Fact]
    public Task GlobalVariableModuleTest() => DoTest(@"int x = 50;

int main()
{
    x = x + 1;
    return x;
}");

    [Fact]
    public Task NamespaceTest() => DoTest(@"int foo()
{
    return 42;
}

int main()
{
    return foo();
}",
        "TestNameSpace", "TestClass");

    [Fact]
    public Task GlobalClassTest() => DoTest(@"int foo()
{
    return 42;
}

int main()
{
    return foo();
}",
    "",
    "TestClass");

    [Fact]
    public Task GlobalClassFqnTest() => DoTest(@"int foo()
{
    return 42;
}

int main()
{
    return foo();
}",
    "",
    "MyNameSpace.TestClass");

    [Fact]
    public Task ConstCharLiteralTest() => DoTest(@"int main()
{
    const char *test = ""hellow"";
}");

    [Fact]
    public Task ConstIntSmallLiteralTest() => DoTest(@"int main()
{
    return 42;
}");

    [Fact]
    public Task TypeConversionTest() => DoTest(@"int main()
{
    return (int)42;
}");

    [Fact]
    public Task ConstIntLargeLiteralTest() => DoTest(@"int main()
{
    return 1337;
}");

    [Fact]
    public Task ConstIntMinLiteralTest() => DoTest(@"int main()
{
    return -2147483648;
}");

    [Fact]
    public Task ConstIntHexLiteralTest() => DoTest(@"int main()
{
    return 0x42;
}");

    [Fact]
    public Task ConstIntOctalLiteralTest() => DoTest(@"int main()
{
    return 042;
}");

    [Fact]
    public Task ConstCharLiteralDeduplication() => DoTest(@"int main()
{
    const char *test1 = ""hellow"";
    const char *test2 = ""hellow1"";
    const char *test3 = ""hellow"";
}");

    [Fact, NoVerify]
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
int main(void) { foo x; x.x = 0; return 0; }");

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
    public Task StructUsageWithMemberAccessGet() => DoTest(@"typedef struct { int x; } foo;
int main(void) { foo x; return x.x; }");

    [Fact]
    public Task StructUsageWithMemberAccessSet() => DoTest(@"typedef struct { int x; } foo;
int main(void) { foo x; x.x = 42; return 0; }");

    [Fact]
    public Task StructAndTypeDefHasSeparateNamespaces() => DoTest(@"struct tagFoo { int A; };
typedef struct { int B; } Foo;
int main(void) {
  struct tagFoo a;
  Foo b;
  a.A = 0;
  b.B = 0;
}
");

    [Fact]
    public Task SingleFieldStructWithUnionDefinition() => DoTest(@"typedef struct { union { int x; float f; }; } foo;
int main ()
{
    foo bar;
    bar.f = 5.2f;
    return bar.x;
}");

    [Fact]
    public void BadStructWithUnionDefinition() => Assert.Throws<InvalidOperationException>(() =>
    {
        DoTest(@"typedef struct { union { int x; float f; }; union { int x; float f; }; } foo;
int main ()
{
    foo bar;
    bar.f = 5.2f;
    return bar.x;
}").Wait();
    });

    [Fact]
    public Task MegaUnionDefinition() => DoTest(@"typedef struct { union { union { int x1; float x2; union { int x2; float f2; union { int x3; float f3; union { int x4; float f4; };};};}; }; } foo;
int main ()
{
    foo bar;
    bar.f4 = 5.2f;
    return bar.x2;
}");


    [Fact]
    public Task MultipleFieldStructWithUnionDefinition() => DoTest("typedef struct { long l; union { int x; float f; }; } foo;");

    [Fact]
    public Task ArrayDeclaration() => DoTest(@"int main()
{
    int i;
    int x[1];
    return 0;
}");

    [Fact]
    public Task StructFunctionMemberDeclaration() => DoTest(@"typedef struct { void (*bar)(int unused); } foo;
int main(void) {}");

    [Fact]
    public Task BasicTypeDef() => DoTest(@"typedef int foo;
int main(void) { foo x = 0; return 0; }");

    [Fact]
    public Task StructWithArray() => DoTest(@"typedef struct {
    int x[4];
} foo;");

    [Fact]
    public Task NamedStruct() => DoTest(@"struct foo {
    int x[4];
};");

    [Fact]
    public Task FunctionPointer() => DoTest(@"void foo(int) {}
typedef int (*foo_t)(int);
int main(void) {
    foo_t x = &foo;
}");

    [Fact, NoVerify]
    public void NonExistingStructMember() => DoesNotCompile(@"typedef struct { int x; } foo;
int main(void) {
    foo x;
    return x.nonExisting;
}", "has no member named \"nonExisting\"");

    [Fact]
    public Task ComplexStructDefinition() => DoTest(@"typedef void(*function)(int, const int*, const int*);
typedef struct {
	int a;
	int b[5];
	unsigned char c[64];
	function func;

	int array[80][5];
} foo;");

    [Fact]
    public Task StaticFileScopedVariable() => DoTest(@"static int x = 123;");

    [Fact]
    public Task StaticStruct() => DoTest(@"struct _foo {
    int x[4];
};

static struct _foo foo;");

    [Fact, NoVerify]
    public void StructAndEnumSameName() => DoesNotCompile(@"enum Token { T };

struct Token {
    int x;
};
", "Tag kind struct type Token was already defined as enum");

    [Fact, NoVerify]
    public void EnumAndStructSameName() => DoesNotCompile(@"
struct Token {
    int x;
};

enum Token { T };
", "Tag kind enum type Token was already defined as struct");

    [Fact]
    public Task StructAndTypeDefWithSameName() => DoTest(@"typedef struct Token Token;

struct Token {
    int x;
};
");

    [Fact]
    public Task StructAndTypeDefWithSameNameSingleDecl() => DoTest(@"
typedef struct Token {
    int x;
} Token;
");

    [Fact]
    public Task StructAndNestedPointer() => DoTest(@"typedef struct Token Token;

struct Token {
    Token* x;
};
");
}
