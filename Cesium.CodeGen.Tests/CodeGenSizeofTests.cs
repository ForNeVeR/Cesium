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
    public Task IdentifierSizeof() => DoTest(@"
int main() {
    int a = 1;
    return sizeof(a);
}");

    [Fact]
    public Task GlobalIdentifierSizeof() => DoTest(@"
int a = 1;
int main() {
    return sizeof(a);
}");

    [Fact]
    public Task ArraySizeof() => DoTest(@"
int main() {
    int x[] = { 1,2,3,4,5 };
    return sizeof(x);
}");

    [Fact]
    public Task EnumSizeof() => DoTest(@"
int main() {
    enum foo {
        bar
    };
    return sizeof(enum foo);
}");

    /// TODO[#453]: Struct can't be resolved
    //    [Fact]
    //    public Task GlobalStructSizeof() => DoTest(@"
    //typedef struct {
    //    int x;
    //    int y;
    //} foo;
    //int main() {
    //    return sizeof(foo);
    //}");
}
