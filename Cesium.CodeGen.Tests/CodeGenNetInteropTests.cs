namespace Cesium.CodeGen.Tests;

public class CodeGenNetInteropTests
{
    private static void DoTest(TargetArchitectureSet architecture, string cSharpCode, string cCode)
    {
        Assert.False(true,
            "TODO: Compile .NET Assembly, compile C assembly with reference to .NET, dump the byte code");
    }

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Wide)]
    public void PointerInterop(TargetArchitectureSet architecture) => DoTest(
        architecture,
        @"public static class Test
{
    public static int Func(int* ptr) => 1;
}
", """
__cli_import("Test::Func")
int Func(int *ptr);

int main(void)
{
    int x = 0;
    return Func(&x);
}
""");

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Wide)]
    public void CPtrInterop(TargetArchitectureSet architecture) => DoTest(
        architecture,
        @"using Cesium.Runtime;
public static class Test
{
    public static int Func(CPtr<int> ptr) => 1;
}
", """
   __cli_import("Test::Func")
   int Func(int *ptr);

   int main(void)
   {
       int x = 0;
       return Func(&x);
   }
   """);

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Wide)]
    public void VPtrInterop(TargetArchitectureSet architecture) => DoTest(
        architecture,
        @"using Cesium.Runtime;
public static class Test
{
    public static int Func(CPtr<int> ptr) => 1;
}
", """
   __cli_import("Test::Func")
   int Func(int *ptr);

   int main(void)
   {
       int x = 0;
       return Func(&x);
   }
   """);

    [Theory]
    [InlineData(TargetArchitectureSet.Dynamic)]
    [InlineData(TargetArchitectureSet.Wide)]
    public void FPtrInterop(TargetArchitectureSet architecture) => DoTest(
        architecture,
        @"using Cesium.Runtime;
public static class Test
{
    public static int Func(FPtr<Func<int>> ptr) => 1;
}
", """
   __cli_import("Test::Func")
   int Func(int (*ptr)());

   int myFunc()
   {
       return 0;
   }

   int main(void)
   {
       return Func(&myFunc);
   }
   """);
}
