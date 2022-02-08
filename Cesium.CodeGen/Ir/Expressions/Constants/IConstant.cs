using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.Expressions.Constants;

internal interface IConstant
{
    void EmitTo(FunctionScope scope);
}
