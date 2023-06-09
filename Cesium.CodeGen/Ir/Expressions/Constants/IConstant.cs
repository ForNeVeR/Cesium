using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions.Constants;

internal interface IConstant
{
    void EmitTo(IEmitScope scope);

    IType GetConstantType();
}
