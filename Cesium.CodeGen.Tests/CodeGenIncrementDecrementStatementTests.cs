using Cesium.Core;
using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenIncrementDecrementStatementTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task PostfixIncrementVariable() => DoTest(@"int main()
{
    int x;
    x++;
    return x;
}");

    [Fact]
    public Task PostfixIncrementConstant() => DoTest(@"int main()
{
    int x;
    x = 5++;
    return x;
}");

    [Fact]
    public Task PrefixIncrementVariable() => DoTest(@"int main()
{
    int x;
    ++x;
    return x;
}");

    [Fact]
    public Task PrefixIncrementConstant() => DoTest(@"int main()
{
    int x;
    x = ++5;
    return x;
}");
}
