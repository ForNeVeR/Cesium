using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Generators;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class DeclarationBlockItem : IBlockItem
{
    private readonly Declaration _declaration;
    public DeclarationBlockItem(Declaration declaration)
    {
        _declaration = declaration;
    }

    public IBlockItem Lower() => this;
    public void EmitTo(FunctionScope scope) => Declarations.EmitLocalDeclaration(scope, _declaration);
}
