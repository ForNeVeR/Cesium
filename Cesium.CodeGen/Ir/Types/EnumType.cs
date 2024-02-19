using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Declarations;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal sealed class EnumType : IType, IEquatable<EnumType>
{
    public EnumType(IReadOnlyList<InitializableDeclarationInfo> members, string? identifier)
    {
        Members = members;
        Identifier = identifier;
    }

    /// <inheritdoc />
    public TypeKind TypeKind => TypeKind.Enum;

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

    public bool Equals(EnumType? other)
    {
        if (other is null) return false;

        if (Identifier != other.Identifier) return false;

        if (Members.Count != other.Members.Count) return false;
        for (var i = 0; i < Members.Count; i++)
        {
            if (!Members[i].Equals(other.Members[i])) return false;
        }

        return true;
    }

    public override bool Equals(object? other)
    {
        if (other is EnumType)
        {
            return Equals((EnumType)other);
        }

        return false;
    }

    public override int GetHashCode()
    {
        var hash = (Identifier?.GetHashCode() ?? 0) ^ 0;
        foreach (var m in Members)
        {
            hash ^= m.GetHashCode();
        }

        return hash;
    }
}
