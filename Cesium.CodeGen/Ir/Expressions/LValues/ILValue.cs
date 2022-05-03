using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.Expressions.LValues;

internal interface ILValue
{
    void EmitGetValue(IDeclarationScope scope);
    void EmitSetValue(IDeclarationScope scope);
}
