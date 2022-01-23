using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal interface IType
{
    TypeReference Resolve(TypeSystem typeSystem);
}
