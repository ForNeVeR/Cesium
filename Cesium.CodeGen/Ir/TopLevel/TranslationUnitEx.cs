using Cesium.Ast;

namespace Cesium.CodeGen.Ir.TopLevel;

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
