using Cesium.Core;
using Cesium.TestFramework;
using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenBreakStatementTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task BreakInFor() => DoTest(@"int main()
{
    for(;;) break;
}");

    [Fact, NoVerify]
    public Task BreakNotInFor() => Assert.ThrowsAsync<CompilationException>(
        () => DoTest(@"int main()
{
    break;
}"));
}
