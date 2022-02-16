using Cesium.CodeGen.Contexts;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal interface IGeneratedType
{
    TypeDefinition Emit(string name, TranslationUnitContext context);
}
