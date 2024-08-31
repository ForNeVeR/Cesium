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

    [Fact]
    public Task EnumUsageInIf() => DoTest(@"
enum Colour { Red, Green, Blue };

void test()
{
    enum Colour x = 42;
    if (x == Green) ;
}");

    [Fact]
    public Task EnumViaTypeDef() => DoTest(@"
typedef enum { Red, Green, Blue } Colour;

void test()
{
    Colour x = 42;
    if (x == Green) ;
}");

    [Fact]
    public Task EnumInFuncionCall() => DoTest(@"
enum Colour { Red, Green, Blue };

void work(enum Colour){}
void test()
{
    work(Green);
}");

    [Fact]
    public Task EnumInCase() => DoTest(@"
enum Colour { Red, Green, Blue };

void test()
{
    enum Colour x = 42;
    switch (x) {
        case Blue:
            break;
    }
}");
}
