using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenArrayTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task ArrayAssignment() => DoTest(@"int main() {
    int a[10];
    a[1] = 2;
    return a[1];
 }");

    [Fact]
    public Task GlobalArrayAssignment() => DoTest(@"
int a[10];
int main() {
    a[1] = 2;
    return a[1];
 }");

    [Fact]
    public Task ArrayAddressOf() => DoTest(@"int main() {
    int a[10];
    int *x = &a[2];
    return 0;
 }");

    [Fact]
    public Task MultidimensionalArrayAssignment() => DoTest(@"int main() {
    int a[10][4];
    a[1][2] = 2;
    return a[1][2];
 }");

    [Fact]
    public Task GlobalMultidimensionalArrayAssignment() => DoTest(@"
int a[10][4];

int main() {
    a[1][2] = 2;
    return a[1][2];
 }");

    [Fact(Skip = "Do not implemented yet. https://github.com/ForNeVeR/Cesium/issues/232")]
    public Task ComplextTypeAssignment() => DoTest(@"typedef struct { int x; } foo;

int main() {
    foo a[10];
    a[2 - 1].x = 42;
    return a[1].x;
 }");

    [Fact]
    public Task ArrayElementIndexInComparison() => DoTest(@"int main() {
    int a[10];
    if (a[1] != 13) {
        return -1;
    }
    return 0;
 }");

    [Fact]
    public Task ArrayElementIndexViaVariable() => DoTest(@"int main() {
    int a[10][1];
    int i = 0;
    a[i][0] = 13;
    return 0;
 }");
}
