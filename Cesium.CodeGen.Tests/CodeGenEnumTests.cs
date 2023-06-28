using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenEnumTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task EnumDeclarationTest() => DoTest(@"
enum Colour { Red, Green, Blue };

void test()
{
    enum Colour x = Green;
}");

    [Fact]
    public Task EnumDeclarationIntTest() => DoTest(@"
enum Colour { Red, Green, Blue };

void test()
{
    enum Colour x = 42;
}");
}
