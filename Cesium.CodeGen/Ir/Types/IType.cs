using Cesium.CodeGen.Contexts;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

/// <summary>An interface representing a C type.</summary>
/// <remarks>
/// Can be of two flavors: an <see cref="IGeneratedType"/> or a plain type that doesn't require any byte code to be
/// generated (a basic type, a pointer or a function pointer.
/// </remarks>
internal interface IType
{
    TypeReference Resolve(TranslationUnitContext context);
    int SizeInBytes { get; }
}

/// <summary>A generated type, i.e. a type that has some bytecode to be generated once.</summary>
internal interface IGeneratedType : IType
{
    TypeDefinition Emit(string name, TranslationUnitContext context);
}
