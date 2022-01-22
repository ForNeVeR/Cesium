using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Generators;

namespace Cesium.CodeGen.Ir.TopLevel;

internal class FunctionDefinition : ITopLevelNode
{
    private readonly Ast.FunctionDefinition _ast;

    public FunctionDefinition(Ast.FunctionDefinition ast)
    {
        _ast = ast;
    }

    public void Emit(TranslationUnitContext context) => Functions.EmitFunction(context, _ast);
}
