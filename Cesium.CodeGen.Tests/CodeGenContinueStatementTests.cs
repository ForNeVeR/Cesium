using Cesium.Core;
using Cesium.TestFramework;
using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenContinueStatementTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task ContinueInFor() => DoTest(@"int main()
{
    int i;
    for(i = 0; i < 10; ++i) continue;
}");

    [Fact]
    public Task ContinueInWhile() => DoTest(@"int main()
{
    int i = 0;
    while (i < 10) {
        ++i;
        if (i == 0) continue;
    }
}");

    [Fact]
    public Task ContinueInDoWhile() => DoTest(@"int main()
{
    int i = 0;
    do {
        ++i;
        if (i == 0) continue;
    }
    while (i < 10);
}");

    [Fact, NoVerify]
    public Task ContinueNotInFor() => Assert.ThrowsAsync<CompilationException>(
        () => DoTest(@"int main()
{
    continue;
}"));

    [Fact, NoVerify]
    public void ContinueInSwitch() => DoesNotCompile(@"int main()
{
    switch (1)
    {
    default:
        continue;
    }
}", "Can't use continue outside of a loop construct.");

    [Fact]
    public Task ContinueInSwitchEnclosedWithLoop() => DoTest(@"int main(int argc, char *argv[])
{
    for (;;)
    {
        switch (1)
        {
        case 42:
            break;
        default:
            continue;
        }

        break;
    }
}");
}
