using Cesium.TestFramework;
using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CliImportTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task CliImportTest() => DoTest(@"__cli_import(""System.Console::Read"")
int console_read(void);

int main()
{
    return console_read();
}");

    [Fact, NoVerify]
    public void CliImportReturnTypeMismatch() => DoesNotCompile(
        @"__cli_import(""System.Console::Read"")
        void console_read(void);",
        "Returns types do not match"
    );

    [Fact, NoVerify]
    public void CliImportArgumentCountMismatch() => DoesNotCompile(
        @"__cli_import(""System.Console::Read"")
        int console_read(int a, int b);",
        "Cannot find CLI-imported member System.Console::Read(System.Int32, System.Int32)."
    );

    [Fact, NoVerify]
    public void CliImportArgumentTypesMismatch() => DoesNotCompile(
        @"__cli_import(""System.Console::SetCursorPosition"")
        void console_set_cursor_position(int a, char b);",
        "Type of argument #1 does not match"
    );

    // TODO[#196]: add tests for variadic arguments when it's supported by the parser
}
