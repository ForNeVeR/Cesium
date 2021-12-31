using System.Text;
using Mono.Cecil;

namespace Cesium.CodeGen.Tests;

public class CodeGenTests : CodeGenTestBase
{
    private static Task VerifyMethods(TypeDefinition type)
    {
        var result = new StringBuilder();
        var first = true;
        foreach (var method in type.Methods)
        {
            if (!first)
                result.AppendLine();
            first = false;

            result.AppendLine(method.ToString());
            var variables = method.Body.Variables;
            if (variables.Count > 0)
            {
                result.AppendLine("  Locals:");
                foreach (var local in variables)
                    result.AppendLine($"    {local.VariableType} {local}");
            }


            foreach (var instruction in method.Body.Instructions)
                result.AppendLine($"  {instruction}");
        }

        return Verify(result);
    }

    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(source, default);

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
    public Task ConstCharLiteralTest() => DoTest(@"int main()
{
    const char *test = ""hellow"";
}");
}
