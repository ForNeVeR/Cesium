using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.Expressions.Constants;

public interface IConstant
{
    void EmitTo(FunctionScope scope);
}
