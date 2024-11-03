using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenBinaryExpressionTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task EqualityForBool() => DoTest(@"int main() { _Bool x = 1; _Bool y = 1; int r = x == y; }");

    [Fact]
    public Task SumIntAndBool() => DoTest(@"int main() { _Bool x = 1; _Bool y = 1; int r = 1 + (x == y); }");

    [Fact]
    public Task ConversionForConst() => DoTest(@"int main() { unsigned char x = 1; const unsigned char y = 1; int r = x + y; }");

    [Fact]
    public Task IdentifierInBrackets() => DoTest(
        """
        void test() {
          char s = 1;
          char t = (s) + 1;
        }        
        """
        );

    [Fact]
    public Task ArrayIndexerInBrackets() => DoTest(
        """
        void test() {
          char* s = "123";
          char t = (s[0]) + 1;
        }        
        """
        );

    // *1 here thought by compiler as indirection.
    [Fact]
    public Task ArrayIndexerInBracketsAfterMultiply() => DoTest(
        """
        void test() {
          char* s = "123";
          char t = (s[0]) * 1;
        }        
        """
        );
}
