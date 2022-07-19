using Cesium.CodeGen.Contexts;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Expressions.Constants;

internal interface IConstant
{
    void EmitTo(IDeclarationScope scope);

    TypeReference GetConstantType(IDeclarationScope scope);
}
