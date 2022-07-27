namespace Cesium.CodeGen.Tests;

public class CliImportTests : CodeGenTestBase
{
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task CliImportTest() => DoTest(@"__cli_import(""System.Console::Read"")
int console_read();

int main()
{
    return console_read();
}");

    [Fact]
    public void CliImportReturnTypeMismatch() => DoesNotCompile(
        @"__cli_import(""System.Console::Read"")
        void console_read();",
        "Returns types for imported function console_read do not match"
    );

    [Fact]
    public void CliImportArgumentCountMismatch() => DoesNotCompile(
        @"__cli_import(""System.Console::Read"")
        int console_read(int a, int b);",
        "Cannot find CLI-imported member System.Console::Read(System.Int32, System.Int32)."
    );

    [Fact]
    public void CliImportArgumentTypesMismatch() => DoesNotCompile(
        @"__cli_import(""System.Console::SetCursorPosition"")
        void console_set_cursor_position(int a, char b);",
        "Type of argument #1 for imported function console_set_cursor_position does not match"
    );

    // todo: add tests for variadic arguments when it's supported by the parser
}
