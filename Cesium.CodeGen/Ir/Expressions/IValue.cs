using Cesium.CodeGen.Contexts;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Expressions;

internal interface IValue
{
    void EmitGetValue(IDeclarationScope scope);
    void EmitGetAddress(IDeclarationScope scope);
    TypeReference GetValueType();
}
