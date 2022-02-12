using Cesium.CodeGen.Contexts;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal record ConstType(IType Base) : IType
{
    public TypeReference Resolve(TranslationUnitContext context) => Base.Resolve(context);
}
