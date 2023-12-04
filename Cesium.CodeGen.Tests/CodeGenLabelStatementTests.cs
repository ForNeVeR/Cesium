using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenLabelStatementTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task LabelDeclaration() => DoTest(@"int main()
{
    int i;
    test_label:
        ++i;
}");

    [Fact]
    public Task GotoBackwardsDeclaration() => DoTest(@"int main()
{
    int i;
    test_label:
        ++i;
    goto test_label;
}");

    [Fact]
    public Task GotoForwardsDeclaration() => DoTest(@"int main()
{
    int i;
    goto test_label;
    test_label:
        ++i;
}");

    [Fact]
    public Task GotoInsideIf() => DoTest(@"void test()
{
    int n = 1;
label:
    ++n;
    if (n <= 10)
        goto label;
}");
}
