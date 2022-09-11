using Cesium.Ast;
using Cesium.CodeGen.Ir.TopLevel;
using Cesium.Core;
using FunctionDefinition = Cesium.CodeGen.Ir.TopLevel.FunctionDefinition;

namespace Cesium.CodeGen.Extensions;

internal static class TranslationUnitEx
{
    public static IEnumerable<ITopLevelNode> ToIntermediate(this TranslationUnit translationUnit) =>
        translationUnit.Declarations.Select(x => (ITopLevelNode)(x switch
        {
            Ast.FunctionDefinition func => new FunctionDefinition(func),
            Ast.SymbolDeclaration sym => new TopLevelDeclaration(sym),
            _ => throw new WipException(212, $"Declaration not supported: {x}.")
        }));
}
