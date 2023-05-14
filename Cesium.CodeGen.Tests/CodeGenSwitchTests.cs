using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenSwitchTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task Empty() => DoTest(@"int main()
{
    int x = 0;
    switch(x) { };
    return 1;
}");

    [Fact]
    public Task OneCase() => DoTest(@"int main()
{
    int x = 0;
    switch(x) { case 0: break; };
    return 1;
}");

    [Fact]
    public Task MultiCases() => DoTest(@"int main()
{
    int x = 0;
    switch(x) {
        case 0: break;
        case 1: break;
    };
}");

    [Fact]
    public Task MultiCasesWithDefault() => DoTest(@"int main()
{
    int x = 0;
    switch(x) {
        case 0: break;
        case 1: break;
        default: break;
    }
}");

    [Fact]
    public Task FallthroughCase() => DoTest(@"int main()
{
    int x = 0;
    switch(x) {
        case 0: break;
        case 1:
        default: break;
    }
}");

    [Fact]
    public Task Blockless() => DoTest(@"int main()
{
    int x = 0;
    switch(x) default: break;
}");

    [Fact]
    public Task DeepCase() => DoTest(@"int main()
{
    int x = 0;
    switch(x) while (0) { default: break; }
}");
}
