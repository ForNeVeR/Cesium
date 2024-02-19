using Cesium.TestFramework;
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

    [Fact, NoVerify]
    public void PostfixIncrementCannotBeConstant() => DoesNotCompile(@"int main()
{
    int x;
    x = 5++;
    return x;
}", "'++' needs l-value");

    [Fact]
    public Task PrefixIncrementVariable() => DoTest(@"int main()
{
    int x;
    ++x;
    return x;
}");

    [Fact, NoVerify]
    public void PrefixIncrementCannotBeConstant() => DoesNotCompile(@"int main()
{
    int x;
    x = ++5;
    return x;
}", "'++' needs l-value");

    [Fact, NoVerify]
    public void PrefixDecrementCannotBeConstant() => DoesNotCompile(@"int main()
{
    int x;
    x = --5;
    return x;
}", "'--' needs l-value");
}
