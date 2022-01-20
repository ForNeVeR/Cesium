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
}
