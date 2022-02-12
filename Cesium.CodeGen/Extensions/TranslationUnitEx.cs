using Cesium.Ast;
using Cesium.CodeGen.Ir.TopLevel;
using FunctionDefinition = Cesium.CodeGen.Ir.TopLevel.FunctionDefinition;

namespace Cesium.CodeGen.Extensions;

public static class TranslationUnitEx
{
    public static IEnumerable<ITopLevelNode> ToIntermediate(this TranslationUnit translationUnit) =>
        translationUnit.Declarations.Select(x => (ITopLevelNode)(x switch
        {
            Ast.FunctionDefinition func => new FunctionDefinition(func),
            Ast.SymbolDeclaration sym => new TopLevelDeclaration(sym),
            _ => throw new NotImplementedException($"Declaration not supported: {x}.")
        }));
}
