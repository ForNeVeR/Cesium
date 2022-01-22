using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Generators;

namespace Cesium.CodeGen.Ir.TopLevel;

internal class ObjectDeclaration : ITopLevelNode
{
    private readonly SymbolDeclaration _ast;
    public ObjectDeclaration(SymbolDeclaration ast)
    {
        _ast = ast;
    }

    public void EmitTo(TranslationUnitContext context) => Declarations.EmitSymbol(context, _ast);
}
