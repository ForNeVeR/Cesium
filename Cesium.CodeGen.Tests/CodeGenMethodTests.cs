namespace Cesium.CodeGen.Tests;

public class CodeGenMethodTests : CodeGenTestBase
{
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(source, default);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    protected static void DoesNotCompile(string source, string expectedMessage)
    {
        var ex = Assert.Throws<NotSupportedException>(() => GenerateAssembly(source, default));
        Assert.Contains(expectedMessage, ex.Message);
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
    [Fact] public Task StandardMain() => DoTest("int main(int argc, char *argv[]){}");
    [Fact] public void NonstandardMainDoesNotCompile1() => DoesNotCompile("void main(){}", "Invalid return type");
    [Fact] public void NonstandardMainDoesNotCompile2() => DoesNotCompile("int main(int c){}", "Invalid parameter");
}
