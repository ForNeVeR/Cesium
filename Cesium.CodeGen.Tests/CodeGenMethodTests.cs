using Cesium.Core;
using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenMethodTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task EmptyMainTest() => DoTest("int main() {}");

    [Fact]
    public Task ArithmeticMainTest() => DoTest("int main() { return 2 + 2 * 2; }");

    [Fact]
    public Task SimpleVariableTest() => DoTest(@"int main()
{
    int x = 0;
    x = x + 1;
    return x + 1;
}");

    [Fact]
    public Task AssigmentLoweringTest() => DoTest(@"int main()
{
    int x = 0;
    x += 1;
    return x + 1;
}");

    [Fact]
    public Task PostfixIncrementTest() => DoTest(@"int main()
{
    int x = 0;
    ++x;
    return x + 1;
}");

    [Fact]
    public Task FunctionCallTest() => DoTest(@"int foo()
{
    return 42;
}

int main()
{
    return foo();
}");

    [Fact]
    public Task NegationExpressTest() => DoTest("int main() { return -42; }");

    [Fact] public Task ParameterlessMain() => DoTest("int main(){}");
    [Fact] public Task VoidParameterMain() => DoTest("int main(void){}");
    [Fact] public Task PointerReceivingFunction() => DoTest("void foo(int *ptr){}");
    [Fact] public Task StandardMain() => DoTest("int main(int argc, char *argv[]){}");
    [Fact] public void NonstandardMainDoesNotCompile1() => DoesNotCompile("void main(){}", "Invalid return type");
    [Fact] public void NonstandardMainDoesNotCompile2() => DoesNotCompile("int main(int c){}", "Invalid parameter");
    [Fact]
    public void VarArgMainDoesNotCompile2() => DoesNotCompile<WipException>(
        "int main(int argc, char *argv[], ...){}",
        "Variable arguments for the main function aren't supported.");

    [Fact] public Task Parameter1Get() => DoTest("int foo(int x){ return x + 1; }");
    [Fact] public Task Parameter5Get() => DoTest("int foo(int a, int b, int c, int d, int e){ return e + 1; }");
    [Fact] public Task CharConstTest() => DoTest("int main() { char x = '\\t'; return 42; }");
    [Fact] public Task DoubleConstTest() => DoTest("int main() { double x = 1.5; return 42; }");
    [Fact] public Task FloatConstTest() => DoTest("int main() { float x = 1.5f; return 42; }");
    [Fact] public Task FloatConstTest2() => DoTest("int main() { float x = 1.5; return 42; }");

    [Fact] public Task MultiDeclaration() => DoTest("int main() { int x = 0, y = 2 + 2; }");

    [Fact] public Task MultiDeclarationWithStruct() => DoTest(@"typedef struct { int x; } foo;
int main() { foo x,x2; x2.x=0; }");

    [Fact] public Task UninitializedVariable() => DoTest("int main() { int x; return 0; }");

    [Fact]
    public Task Arithmetic() => DoTest(@"int main(void)
{
    int y = -42;
    int x = 18;
    x += 1;
    ++x;
    x *= 2;
    return x + 2;
}
");

    [Fact]
    public Task ReturnWithoutArgument() => DoTest(@"void console_read()
{
    return;
}");

    [Fact]
    public void IncorrectReturnTypeDoesNotCompile() => DoesNotCompile(@"int foo(void);
void foo(void) {}", "Incorrect return type");

    [Fact]
    public void IncorrectParameterTypeDoesNotCompile() => DoesNotCompile(@"int foo(int bar);
int foo(char *x) {}", "Incorrect type for parameter x");

    [Fact]
    public void IncorrectParameterCountDoesNotCompile() => DoesNotCompile(@"int foo(int bar, int baz);
int foo(int bar) {}", "Incorrect parameter count");

    [Fact]
    public void IncorrectOverrideCliImport() => DoesNotCompile(@"__cli_import(""System.Console::Read"")
int console_read(void);
int console_read(void) { return 0; }", "Function console_read already defined as immutable.");

    [Fact]
    public void DifferentCliImport() => DoesNotCompile(@"__cli_import(""System.Console::Beep"")
void console_beep(void);
__cli_import(""System.Console::Clear"")
void console_beep(void);", "Function console_beep already defined as as CLI-import with System.Void System.Console::Beep().");

    [Fact]
    public Task CanHaveTwoCliImportDeclarations() => DoTest(@"__cli_import(""System.Console::Read"")
int console_read(void);
__cli_import(""System.Console::Read"")
int console_read(void);");

    [Fact]
    public Task VarargCall() => DoTest(@"void console_read(int arg, ...);

void console_read(int arg, ...)
{
}

void test()
{
    console_read(5, 32);
    console_read(5, 2.21f);
    console_read(5, 67.44);
}");

    [Fact]
    public Task ImplicitVarargDeclarationCanBeIgnored() => DoTest(@"void console_read();

void console_read()
{
}");

    [Fact]
    public Task ImplicitVarargDefinitionCanBeIgnored() => DoTest(@"void console_read(void);

void console_read()
{
}");

    [Fact]
    public void ExplicitVarargDeclarationShouldHaveExplicitDefinition() => DoesNotCompile(@"void console_read(int x, ...);

void console_read(int x)
{
}", "Function console_read declared with varargs but defined without varargs.");

    [Fact]
    public void ExplicitVarargDefinitionShouldHaveExplicitDeclaration() => DoesNotCompile(@"void console_read(int x);

void console_read(int x, ...)
{
}", "Function console_read declared without varargs but defined with varargs.");

    [Fact]
    public Task CanHaveTwoFunctionDeclarations() => DoTest(@"
int console_read(void);

int console_read(void);

int console_read(void) { return 0; }");

    [Fact]
    public void DoubleDefinition() => DoesNotCompile(@"int console_read() { return 1; }
int console_read() { return 2; }", "Double definition of function console_read.");

    [Fact]
    public void NoDefinition() => DoesNotCompile(@"int foo(void);
int main() { return foo(); }", "Function foo not defined.");

    [Fact]
    public Task PrimitiveTypes() => DoTest(@"int main(void)
{
    // basic
    char c = 0;
    short s = 0;
    signed s1 = 0;
    int i = 0;
    unsigned u = 0;
    long l = 0;
    float f = 0;
    double d = 0;

    // unsigned
    unsigned char uc = 0;
    unsigned short us = 0;
    unsigned short int usi = 0;
    unsigned int ui = 0;
    unsigned long ul = 0;
    unsigned long int uli = 0;
    unsigned long long ull = 0;
    unsigned long long int ulli = 0;

    // signed
    signed char sc = 0;
    signed short ss = 0;
    short int shi = 0;
    signed short int ssi = 0;
    signed int si = 0;
    signed long sl = 0;
    long int li = 0;
    signed long int sli = 0;
    long long ll = 0;
    signed long long sll = 0;
    long long int lli = 0;
    signed long long int slli = 0;
    long double ld = 0;

    return 0;
}");

    [Fact]
    public Task BitArithmetic() => DoTest(@"int main() { return ~1 << 2 >> 3 | 4 & 5 ^ 6; }");

    [Fact]
    public Task BitOrAssignmentLoweringTest() => DoTest(@"int main() {
    int x = 0;
    x |= 1;
    return x;
}");

    [Fact]
    public Task SimpleRelationalOperators() => DoTest(@"int main() { return 1 > 2 < 4; }");

    [Fact]
    public Task RelationalOperatorsWithLowering() => DoTest(@"int main() { return 1 >= 2 <= 4; }");

    [Fact]
    public Task EqualToOperator() => DoTest(@"int main() { return 1 == 2; }");

    [Fact]
    public Task EqualToOperatorTrueBoolToIntConversion() => DoTest(@"int main() { int x = 2 == 2; return x; }");

    [Fact]
    public Task EqualToOperatorFalseBoolToIntConversion() => DoTest(@"int main() { int x = 1 == 2; return x; }");

    [Fact]
    public Task IntToBoolConversion() => DoTest(@"int main() { int x = 5; if(x) return 1; return 0; }");

    [Fact]
    public Task NotEqualToOperator() => DoTest(@"int main() { return 1 != 2; }");

    [Fact]
    public Task LogicalAndOperator() => DoTest(@"int main() { return 1 && 2; }");

    [Fact]
    public Task LogicalOrOperator() => DoTest(@"int main() { return 1 || 2; }");

    [Fact]
    public Task AmbiguousCallTest() => DoTest(@"
int abs(int x) { return x; }
void exit(int x) { }

int main()
{
    int exitCode = abs(-42);
    exit(exitCode);
}");

    [Fact]
    public Task FunctionPtrTest() => DoTest(@"typedef void (*foo)(void);
int main()
{
    foo unused = 0;
    return 0;
}");

    [Fact]
    public Task FunctionPtrWithParamsTest() => DoTest(@"typedef int (*foo)(int x);
int main()
{
    foo unused = 0;
    return 0;
}");

    [Fact]
    public Task ImplicitReturnAllowedForMain() => DoTest(@"int main()
{
    int unused = 0;
}");

    [Fact]
    public void ImplicitReturnDisallowedNonMain() => DoesNotCompile(@"int foo()
{
    int unused;
}", "Function foo has no return statement.");

    [Fact]
    public Task ConsumeUnusedResult() => DoTest(@"
int test () { return 1; }
int main()
{
    test();
}");

    [Fact]
    public Task StructSubscriptionTest() => DoTest(@"typedef struct
{
    int fixedArr[4];
} foo;
int main()
{
    foo x;
    x.fixedArr[3] = 0;
    return x.fixedArr[3];
}
");

}
