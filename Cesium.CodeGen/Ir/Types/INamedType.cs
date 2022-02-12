using Cesium.CodeGen.Contexts;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal interface INamedType
{
    TypeDefinition Emit(string name, TranslationUnitContext context);
}
