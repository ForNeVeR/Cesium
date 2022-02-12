using Cesium.CodeGen.Contexts;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal interface IType
{
    TypeReference Resolve(TranslationUnitContext context);
}
