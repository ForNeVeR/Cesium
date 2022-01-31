using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.Expressions.LValues;

internal interface ILValue
{
    void EmitGetValue(FunctionScope scope);
    void EmitSetValue(FunctionScope scope);
}
