using Cesium.Ast;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Cesium.Parser;
using JetBrains.Annotations;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.CodeGen.Tests;

public class CodeGenPrimitiveTypeTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Theory]
    [InlineData("char", PrimitiveTypeKind.Char)]
    [InlineData("int", PrimitiveTypeKind.Int)]
    [InlineData("void", PrimitiveTypeKind.Void)]
    [InlineData("unsigned char", PrimitiveTypeKind.UnsignedChar)]
    [InlineData("_Bool", PrimitiveTypeKind.Bool)]
    internal void Test(string typeString, PrimitiveTypeKind expectedKind)
    {
        var source = $"{typeString} x;";
        var parser = new CParser(new CLexer(source));
        var ast = parser.ParseDeclaration().Ok.Value;
        var item = (ScopedIdentifierDeclaration)IScopedDeclarationInfo.Of((Declaration)ast).Single();
        var type = (PrimitiveType)item.Declaration.Type;
        Assert.Equal(expectedKind, type.Kind);
        Assert.Equal(StorageClass.Auto, item.StorageClass);
    }

    [Fact]
    public Task PrimitiveInitializer() => DoTest(@"int main() {
    int a = { 10 };
    return 0;
 }");

    [Fact]
    public Task PrimitiveEmptyInitializer() => DoTest(@"int main() {
    int a = { };
    return 0;
 }");
}
