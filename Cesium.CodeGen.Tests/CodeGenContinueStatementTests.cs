using Cesium.Core;
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

    [Fact]
    public Task ContinueNotInFor() => Assert.ThrowsAsync<CompilationException>(
        () => DoTest(@"int main()
{
    continue;
}"));

    [Fact]
    public void ContinueInSwitch() => DoesNotCompile(@"int main()
{
    switch (1)
    {
    default:
        continue;
    }
}", "Can't break not from loop statement");

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
