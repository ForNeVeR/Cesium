using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;
using Mono.Cecil;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace Cesium.CodeGen.Ir.Types;
internal sealed class UnionType : IGeneratedType, IEquatable<UnionType>
{
    private string UnionName;
    private TypeDefinition? CachedType;

    public UnionType(IReadOnlyList<LocalDeclarationInfo> members)
    {
        Members = members;
        UnionName = "Union_" + string.Join('_', Members.Select(m => m.Type is IGeneratedType generated ? generated.Identifier
            : m.Type is PrimitiveType primitive ? primitive.Kind.ToString() : "Unk")); // very bad, but better than nothing
    }

    public string? Identifier => UnionName;

    internal IReadOnlyList<LocalDeclarationInfo> Members { get; set; }

    public TypeKind TypeKind => TypeKind.Union;

    public TypeDefinition StartEmit(string name, TranslationUnitContext context) => EmitOrGetType(context); // never called +_+

    public TypeDefinition EmitOrGetType(TranslationUnitContext context)
    {
        if (CachedType != null)
            return CachedType;

        CachedType = new TypeDefinition(string.Empty,
            UnionName,
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.ExplicitLayout,
            context.Module.ImportReference(context.AssemblyContext.MscorlibAssembly.GetType("System.ValueType")));

        CachedType.PackingSize = 1;

        foreach(var member in Members)
        {
            var (type, identifier, cliImportMemberName) = member;
            if (identifier == null && type is UnionType u) identifier = u.Identifier;
            var field = type.CreateFieldOfType(context, CachedType, identifier!); // unsafe
            field.Offset = 0;
            CachedType.Fields.Add(field);
        }

        //CachedType.ClassSize = GetSizeInBytes(TargetArchitectureSet.Bit64)!.Value;

        context.Module.Types.Add(CachedType);
        return CachedType;
    }

    public void FinishEmit(TypeDefinition definition, string name, TranslationUnitContext context)
    {
        throw new NotImplementedException(); // never called
    }

    public int? GetSizeInBytes(TargetArchitectureSet arch)
    {
        int max = 1;

        foreach(var member in Members)
        {
            var maybeSize = member.Type.GetSizeInBytes(arch);
            if (maybeSize.HasValue)
            {
                var size = maybeSize.Value;
                if (max < size)
                    max = size;
            }
        }

        return max;
    }

    public TypeReference Resolve(TranslationUnitContext context)
    {
        var resolved = context.GetTypeReference(this);
        if (resolved == null)
            return new TypeReference(string.Empty, UnionName, context.Module, EmitOrGetType(context).Scope, true);
        return resolved;
    }

    public bool Equals(UnionType? other)
    {
        if (other is null) return false;

        if (Members.Count != other.Members.Count) return false;
        for (var i = 0; i < Members.Count; i++)
        {
            if (!Members[i].Equals(other.Members[i])) return false;
        }

        return true;
    }

    public override bool Equals(object? other)
    {
        if (other is UnionType)
        {
            return Equals((UnionType)other);
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

    internal sealed class UnionFieldReference : FieldReference
    {
        private List<FieldReference> Path;

        public UnionFieldReference(FieldReference field, List<FieldReference> path) : base(field.Name, field.FieldType, field.DeclaringType)
        {
            Path = path;
        }

        internal void EmitPath(IEmitScope scope)
        {
            var start = Path.Count - 1;
            for (int i = start; i >= 1; i--)
            {
                var field = Path[i];
                scope.LdFldA(field);
            }
        }
    }
}
