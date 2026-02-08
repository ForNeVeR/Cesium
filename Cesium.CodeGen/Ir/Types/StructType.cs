// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal sealed class StructType : IGeneratedType, IEquatable<StructType>, IEquatable<IGeneratedType>
{
    private const string _anonStructPrefix = "_Anon_";
    private const string _anonUnionPrefix = "_Union_";

    public StructType(IReadOnlyList<LocalDeclarationInfo> members, bool isUnion, string? identifier)
    {
        Members = members;
        IsUnion = isUnion;
        Identifier = identifier;
        IsAnon = identifier == null;
        if (IsAnon) _anonIdentifier = CreateAnonIdentifier(members, isUnion);
    }

    public bool IsAnon { get; private set; }

    public bool IsUnion { get; private set; }

    /// <inheritdoc />
    public TypeKind TypeKind => IsUnion ? TypeKind.Union : TypeKind.Struct;

    internal IReadOnlyList<LocalDeclarationInfo> Members { get; set; }
    public string? Identifier { get; }

    // We need a good name generator...
    private readonly string? _anonIdentifier;

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
                structType.IsExplicitLayout = true;
                break;
            case TargetArchitectureSet.Bit64:
            case TargetArchitectureSet.Wide:
                structType.PackingSize = 8;
                structType.IsExplicitLayout = true;
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
        int currentOffset = 0;
        foreach (var member in Members)
        {
            var (type, identifier, cliImportMemberName) = member;
            if (identifier == null)
            {
                if (type is StructType structType && structType.IsAnon)
                {
                    identifier = structType._anonIdentifier;
                }
                else
                    throw new NotImplementedException($"Unexpected field with null ident and with type: {type}");
            }

            if (cliImportMemberName != null)
                throw new CompilationException(
                    $"CLI imports inside struct members aren't supported: {cliImportMemberName}.");

            var field = type.CreateFieldOfType(context, definition, identifier!);

            if (IsUnion)
            {
                field.Offset = 0;
            }
            else
            {
                if (context.AssemblyContext.ArchitectureSet != TargetArchitectureSet.Dynamic)
                {
                    var fieldSize = type.GetSizeInBytes(context.AssemblyContext.ArchitectureSet) ?? throw new NotSupportedException($"For type {type} cannot calculate size in bytes");
                    field.Offset = currentOffset;
                    currentOffset += fieldSize;
                }
            }

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

        return resolved ?? throw new CompilationException($"Type {this} was not found.");
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
                _ => Members.Select(m => m.Type.GetSizeInBytes(arch) ?? throw new NotImplementedException($"Cannot determine size of a type {m.Type} for architecture set {arch}")).Sum()
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

    public bool Equals(IGeneratedType? other)
    {
        if (other is StructType type)
        {
            return Equals(type);
        }

        return false;
    }

    public override bool Equals(object? other)
    {
        if (other is StructType type)
        {
            return Equals(type);
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
        return (isUnion ? _anonUnionPrefix : _anonStructPrefix) + string.Join('_', members.SelectMany(_ => new string[] { _.Type is StructType st ? st.Identifier ?? string.Empty : _.Type is PrimitiveType pt ? pt.Kind.ToString() : _.Type.TypeKind.ToString(), _.Identifier ?? string.Empty }));
    }

    internal sealed class AnonStructFieldReference(FieldDefinition field, List<FieldDefinition> path) : FieldReference(field.Name, field.FieldType, field.DeclaringType)
    {
        internal void EmitPath(IEmitScope scope)
        {
            var start = path.Count - 1;
            for (int i = start; i >= 1; i--)
            {
                var field = path[i];
                scope.LdFldA(field);
            }
        }

        public override FieldDefinition Resolve() => field;
    }
}
