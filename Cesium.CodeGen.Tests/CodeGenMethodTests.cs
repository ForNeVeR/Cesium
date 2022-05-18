namespace Cesium.CodeGen.Tests;

public class CodeGenMethodTests : CodeGenTestBase
{
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
    public Task CliImportTest() => DoTest(@"__cli_import(""System.Console::Read"")
int console_read();

int main()
{
    return console_read();
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
    public void VarArgMainDoesNotCompile2() => DoesNotCompile(
        "int main(int argc, char *argv[], ...){}",
        "Variable arguments for the main function aren't supported.");

    [Fact] public Task ParameterGet() => DoTest("int foo(int x){ return x + 1; }");
    [Fact]
    public Task CharConstTest() => DoTest("int main() { char x = '\\t'; return 42; }");

    [Fact] public Task MultiDeclaration() => DoTest("int main() { int x = 0, y = 2 + 2; }");

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
int console_read();
int console_read() { return 0; }", "Function console_read already defined as immutable.");

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
    char c;
    short s;
    signed s1;
    int i;
    unsigned u;
    long l;
    float f;
    double d;

    // unsigned
    unsigned char uc;
    unsigned short us;
    unsigned short int usi;
    unsigned int ui;
    unsigned long ul;
    unsigned long int uli;
    unsigned long long ull;
    unsigned long long int ulli;

    // signed
    signed char sc;
    signed short ss;
    short int shi;
    signed short int ssi;
    signed int si;
    signed long sl;
    long int li;
    signed long int sli;
    long long ll;
    signed long long sll;
    long long int lli;
    signed long long int slli;
    long double ld;

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
    public Task NotEqualToOperator() => DoTest(@"int main() { return 1 != 2; }");

    [Fact]
    public Task LogicalAndOperator() => DoTest(@"int main() { return 1 && 2; }");

    [Fact]
    public Task LogicalOrOperator() => DoTest(@"int main() { return 1 || 2; }");

    [Fact]
    public Task ArrayAssignment() => DoTest(@"int main() {
    int a[10];
    a[1] = 2;
    return a[1];
 }");
}
