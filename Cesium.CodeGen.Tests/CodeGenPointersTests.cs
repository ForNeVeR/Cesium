using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenPointersTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task AddressOfTest() => DoTest("int main() { int x; int *y = &x; }");

    [Fact]
    public Task IndirectionGetTest() => DoTest("int foo (int *x) { return *x; }");

    [Fact]
    public Task AddToPointerFromRight() => DoTest("void foo (int *x) { x = x+1; }");

    [Fact]
    public Task AddToPointerFromLeft() => DoTest("void foo (int *x) { x = 1+x; }");

    [Fact]
    public void CannotMultiplyPointerTypes() => DoesNotCompile(
        "void foo (int *x) { x = 1*x; }",
        "Operator '*' does not suported on pointer types");

    [Fact]
    public void CannotAddPointerTypes() => DoesNotCompile(
        "void foo (int *x) { x = x +x; }",
        "Operator '+': cannot add two pointers");
}
