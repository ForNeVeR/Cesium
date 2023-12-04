using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal sealed class StructType : IGeneratedType, IEquatable<StructType>
{
    public StructType(IReadOnlyList<LocalDeclarationInfo> members, string? identifier)
    {
        Members = members;
        Identifier = identifier;
    }

    internal IReadOnlyList<LocalDeclarationInfo> Members { get; }
    public string? Identifier { get; }

    public TypeDefinition Emit(string name, TranslationUnitContext context)
    {
        var structType = new TypeDefinition(
            "",
            Identifier is null ? "<typedef>" + name : Identifier,
            TypeAttributes.Sealed,
            context.Module.ImportReference(context.AssemblyContext.MscorlibAssembly.GetType("System.ValueType")));
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
        context.Module.Types.Add(structType);

        foreach (var member in Members)
        {
            var (type, identifier, cliImportMemberName) = member;
            if (identifier == null)
                throw new WipException(
                    233,
                    $"Anonymous struct members for {name} aren't supported, yet: {type}.");

            if (cliImportMemberName != null)
                throw new CompilationException(
                    $"CLI imports inside struct members aren't supported: {cliImportMemberName}.");

            var field = type.CreateFieldOfType(context, structType, identifier);
            // TODO[#355]: for every field, calculate the explicit layout position.
            structType.Fields.Add(field);
        }

        return structType;
    }

    public TypeReference Resolve(TranslationUnitContext context) =>
        context.GetTypeReference(this) ?? throw new CompilationException($"Type {this} was not found.");

    public IExpression GetSizeInBytesExpression(TargetArchitectureSet arch)
    {
        var constSize = GetSizeInBytes(arch);
        if (constSize != null)
            return ConstantLiteralExpression.OfInt32(constSize.Value);

        return new SizeOfOperatorExpression(this);
    }

    public int? GetSizeInBytes(TargetArchitectureSet arch) => Members.Count switch
    {
        0 => throw new AssertException($"Invalid struct with no members: {this}."),
        1 => Members.Single().Type.GetSizeInBytes(arch),
        _ => arch switch
            {
                TargetArchitectureSet.Dynamic => null,
                _ => throw new WipException(355, $"Cannot determine size of a structure with {Members.Count} members for architecture set {arch}: this requires struct layout calculation that is not yet supported.")
            }
    };

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
}
