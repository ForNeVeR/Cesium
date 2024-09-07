using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenArrayTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest([StringSyntax("cpp")] string source)
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
    public Task SymmetricArrayAssignment() => DoTest(@"int main() {
    int a[10];
    1[a] = 2;
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

    [Fact]
    public Task MultidimensionalArrayComplexExpr() => DoTest("""
int main(void)
{
    int a[10][2];
    a[2 - 1][1] = 13;
}
""");

    [Fact]
    public Task ComplexTypeAssignment() => DoTest(@"typedef struct { int x; } foo;

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

    [Fact]
    public Task ArrayInitialization() => DoTest(@"int main() {
    int a[4] = { 1, 2, 3, 4, };
    a[1] = 2;
    return a[1];
 }");

    [Fact]
    public Task ArrayInitializationChar() => DoTest(@"int main() {
    int a[4] = { '1', '2', '3', '4', };
    a[1] = 2;
    return a[1];
 }");

    [Fact]
    public Task GlobalArrayInitialization() => DoTest(@"
int a[4] = { 1, 2, 3, 4, };

int main() {
    a[1] = 2;
    return a[1];
 }");

    [Fact]
    public Task GlobalArrayInitializationWithoutSize() => DoTest(@"
    int ints1[3] = { 1, 2, 3 };
    int ints2[] = { 1, 2, 1 };

    int main() {
    return ints1[0] + ints2[2];
}");

    [Fact]
    public Task ArrayInitializationWithoutSize() => DoTest(@"int main() {
    int ints1[3] = { 1, 2, 3 };
    int ints2[] = { 1, 2, 1 };
    return ints1[0] + ints2[2];
}");

    [Fact]
    public Task EmptyArrayInitialization() => DoTest(@"int main() {
    int a[4] = { };
    a[1] = 2;
    return a[1];
 }");

    [Fact]
    public Task ArrayParameterPassing() => DoTest(@"
int foo(int ints[]) { return ints[0]; }");

    [Fact]
    public Task ArrayOverPointer() => DoTest(@"int main(int argc, char** argv) {
    char c = argv[0][0];
    return argv[1];
 }");

    [Fact]
    public Task PointerArrayIndexing() => DoTest(@"
int f(char*** t) {
    char* c = t[2][3];
    return c[1];
}

int main() {
    return 42;
 }");

    [Fact]
    public Task ArrayArithmetic() => DoTest("""
int main() {
    int arr[2] = {0, 0};
    int *p = arr + 1;
    return *p;
}
""");

    [Fact]
    public Task SignedByteArrayTest() => DoTest(@"
int main() {
    signed char x[1] = { -1 };
    signed char y = x[0];
    int z = (int)y;
    return z;
}");

    [Fact]
    public Task UnSignedByteArrayTest2() => DoTest(@"
int main() {
    unsigned char a[1] = { 255 };
    unsigned char b = a[0];
    int c = (int)b;
    return c;
}");

    [Fact]
    public Task PointerAsArray() => DoTest("""
int main(void)
{
   int x[30];
   int *y = &x[5];
   (y + 5)[0] = 0;
}
""");

    [Fact]
    public Task ConstByteArrayTest() => DoTest(@"
int main() {
    const char a[1] = { 255 };
}");

    [Fact]
    public Task ArrayCharComparison() => DoTest(@"
int main() {
    const char a[1] = { 'A' };
    if (a[0] != 'D') {
        return 0;
    }

    return 1;
}");
}
