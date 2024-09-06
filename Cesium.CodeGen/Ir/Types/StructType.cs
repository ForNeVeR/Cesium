using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal sealed class StructType : IGeneratedType, IEquatable<StructType>
{
    private const string AnonStructPrefix = "_Anon_";
    private const string AnonUnionPrefix = "_Union_";

    private TypeReference? AnonType;

    public StructType(IReadOnlyList<LocalDeclarationInfo> members, bool isUnion, string? identifier)
    {
        Members = members;
        IsUnion = isUnion;
        Identifier = identifier;
        IsAnon = identifier == null;
        if (IsAnon) AnonIdentifier = CreateAnonIdentifier(members, isUnion);
    }

    public bool IsAnon { get; private set; }

    public bool IsUnion { get; private set; }

    /// <inheritdoc />
    public TypeKind TypeKind => IsUnion ? TypeKind.Union : TypeKind.Struct;

    internal IReadOnlyList<LocalDeclarationInfo> Members { get; set; }
    public string? Identifier { get; }

    // We need a good name generator...
    private string? AnonIdentifier;

    public TypeDefinition StartEmit(string name, TranslationUnitContext context)
    {
        var structType = new TypeDefinition(
            "",
            Identifier is null ? "<typedef>" + name : Identifier,
            TypeAttributes.Sealed,
            context.Module.ImportReference(new TypeReference("System", "ValueType", context.AssemblyContext.MscorlibAssembly.MainModule, context.AssemblyContext.MscorlibAssembly.MainModule.TypeSystem.CoreLibrary)));
        switch (context.AssemblyContext.ArchitectureSet)
        {
            case TargetArchitectureSet.Dynamic:
                // Nothing, default layout is okay for dynamic architecture.
                break;
            case TargetArchitectureSet.Bit32:
                structType.PackingSize = 4;
                // TODO[#355]: enable explicit layout.
                break;
            case TargetArchitectureSet.Bit64:
            case TargetArchitectureSet.Wide:
                structType.PackingSize = 8;
                // TODO[#355]: enable explicit layout.
                break;
            default:
                throw new AssertException($"Unknown architecture set: {context.AssemblyContext.ArchitectureSet}.");
        }
        if (IsUnion)
        {
            structType.ClassSize = -1;
            structType.PackingSize = -1;
            structType.IsExplicitLayout = true;
        }

        context.Module.Types.Add(structType);
        return structType;
    }

    public void FinishEmit(TypeDefinition definition, string name, TranslationUnitContext context)
    {
        foreach (var member in Members)
        {
            var (type, identifier, cliImportMemberName) = member;
            if (identifier == null)
            {
                if (type is StructType structType && structType.IsAnon)
                {
                    identifier = structType.AnonIdentifier;
                }
                else
                    throw new NotImplementedException($"Unexpected field with null ident and with type: {type}");
            }

            if (cliImportMemberName != null)
                throw new CompilationException(
                    $"CLI imports inside struct members aren't supported: {cliImportMemberName}.");

            var field = type.CreateFieldOfType(context, definition, identifier!);

            if (IsUnion) field.Offset = 0;

            // TODO[#355]: for every field, calculate the explicit layout position.
            definition.Fields.Add(field);
        }
    }

    public void EmitType(TranslationUnitContext context)
    {
        var name = IsAnon ? CreateAnonIdentifier(Members, IsUnion) : Identifier ?? CreateAnonIdentifier(Members, IsUnion);
        context.GenerateType(name, this);
    }

    public bool IsAlreadyEmitted(TranslationUnitContext context) => context.GetTypeReference(this) != null;

    public TypeReference Resolve(TranslationUnitContext context)
    {
        var resolved = context.GetTypeReference(this);

        if (resolved == null)
        {
            throw new CompilationException($"Type {this} was not found.");
        }

        return resolved;
    }

    public IExpression GetSizeInBytesExpression(TargetArchitectureSet arch)
    {
        var constSize = GetSizeInBytes(arch);
        if (constSize != null)
            return ConstantLiteralExpression.OfInt32(constSize.Value);

        return new SizeOfOperatorExpression(this);
    }

    public int? GetSizeInBytes(TargetArchitectureSet arch)
    {
        if (IsUnion)
        {
            int max = 1;

            foreach (var member in Members)
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
        else return Members.Count switch
        {
            0 => throw new AssertException($"Invalid struct with no members: {this}."),
            1 => Members.Single().Type.GetSizeInBytes(arch),
            _ => arch switch
            {
                TargetArchitectureSet.Dynamic => null,
                _ => throw new WipException(355, $"Cannot determine size of a structure with {Members.Count} members for architecture set {arch}: this requires struct layout calculation that is not yet supported.")
            }
        };
    }

    public bool Equals(StructType? other)
    {
        if (other is null) return false;

        if (Identifier != other.Identifier) return false;

        if (Members.Count != other.Members.Count) return false;
        for (var i =0;i< Members.Count;i++)
        {
            if (!Members[i].Equals(other.Members[i])) return false;
        }

        return true;
    }

    public override bool Equals(object? other)
    {
        if (other is StructType)
        {
            return Equals((StructType)other);
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

    private static string CreateAnonIdentifier(IReadOnlyList<LocalDeclarationInfo> members, bool isUnion)
    {
        return (isUnion ? AnonUnionPrefix : AnonStructPrefix) + string.Join('_', members.SelectMany(_ => new string[] { _.Type is StructType st ? st.Identifier ?? string.Empty : _.Type is PrimitiveType pt ? pt.Kind.ToString() : _.Type.TypeKind.ToString(), _.Identifier ?? string.Empty }));
    }

    internal sealed class AnonStructFieldReference : FieldReference
    {
        private List<FieldDefinition> Path;
        private FieldDefinition Field;

        public AnonStructFieldReference(FieldDefinition field, List<FieldDefinition> path) : base(field.Name, field.FieldType, field.DeclaringType)
        {
            Field = field;
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

        public override FieldDefinition Resolve() => Field;
    }
}
