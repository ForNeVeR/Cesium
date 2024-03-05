using Cesium.Ast;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Cesium.Parser;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.CodeGen.Tests;

public class CodeGenPrimitiveTypeTests
{
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
        var declarationInfo = (ScopedIdentifierDeclaration)IScopedDeclarationInfo.Of((Declaration)ast);
        var item = declarationInfo.Items.Single();
        var type = (PrimitiveType)item.Declaration.Type;
        Assert.Equal(expectedKind, type.Kind);
    }
}
