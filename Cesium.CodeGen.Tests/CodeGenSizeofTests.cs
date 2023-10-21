using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenSizeofTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task PrimitiveTypeSizeof() => DoTest(@"
int main() {
    return sizeof(int);
}");

    [Fact]
    public Task NamedTypeSizeof() => DoTest(@"
int main() {
    int a = 1;
    return sizeof(a);
}");

    [Fact]
    public Task GlobalStructSizeof() => DoTest(@"
struct foo {
    int x;
    int y;
};
int main() {
    return sizeof(struct foo);
 }");
}
