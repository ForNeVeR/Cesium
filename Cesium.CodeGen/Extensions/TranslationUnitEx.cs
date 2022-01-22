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
            SymbolDeclaration sym => new ObjectDeclaration(sym),
            _ => throw new NotImplementedException($"Declaration not supported: {x}.")
        }));
}
