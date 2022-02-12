using Cesium.Ast;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

public class StructType : IType
{
    public StructType(IEnumerable<StructDeclaration> declarations)
    {
    }

    public TypeReference Resolve(TypeSystem typeSystem) =>
        throw new NotImplementedException();
}
