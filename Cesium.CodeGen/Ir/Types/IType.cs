using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

public interface IType
{
    TypeReference Resolve(TypeSystem typeSystem);
}
