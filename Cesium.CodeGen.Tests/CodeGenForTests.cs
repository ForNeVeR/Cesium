using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenForTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task SimpleFor() => DoTest(
        @"int main()
{
    int i;
    for(i = 0; i < 10; ++i) ++i;
}");

    [Fact]
    public Task ForTest_NoInit() => DoTest(
        @"int main()
{
    int i = 0;
    for(; i < 10; ++i) ++i;
}");


    [Fact]
    public Task For_NoTest() => DoTest(
        @"int main()
{
    int i;
    for(i = 0; ; ++i) ++i;
}");

    [Fact]
    public Task For_NoUpdate() => DoTest(
        @"int main()
{
    int i;
    for(i = 0; i < 10; ) ++i;
}");

    [Fact]
    public Task For_Empty() => DoTest(
        @"int main()
{
    int i;
    for(;;) ++i;
}");

    [Fact]
    public Task For_WithDeclaration() => DoTest(
        @"int main()
{
    for(int i = 0; i < 10; ++i) ++i;
}");

    [Fact]
    public Task For_TwoLoopsWithSameCounterName() => DoTest(
        @"int main()
{
    for(int i = 0; i < 10; ++i) ++i;

    for(int i = 0; i < 10; ++i) ++i;
}");
}
