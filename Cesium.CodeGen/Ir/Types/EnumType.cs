using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Declarations;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal class EnumType : IType
{
    public EnumType(IReadOnlyList<InitializableDeclarationInfo> members, string? identifier)
    {
        Members = members;
        Identifier = identifier;
    }

    internal IReadOnlyList<InitializableDeclarationInfo> Members { get; }
    public string? Identifier { get; }

    public TypeReference Resolve(TranslationUnitContext context)
    {
        return context.TypeSystem.Int32;
    }

    public int? GetSizeInBytes(TargetArchitectureSet arch)
    {
        return 4;
    }
}
