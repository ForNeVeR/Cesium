using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Declarations;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal class StructType : IType, IGeneratedType
{
    private readonly IEnumerable<LocalDeclarationInfo> _members;
    public StructType(IEnumerable<LocalDeclarationInfo> members)
    {
        _members = members;
    }

    public TypeDefinition Emit(string name, TranslationUnitContext context)
    {
        var structType = new TypeDefinition(
            "",
            name,
            TypeAttributes.Sealed,
            context.Module.ImportReference(typeof(ValueType)));
        context.Module.Types.Add(structType);

        foreach (var member in _members)
        {
            var (type, identifier, cliImportMemberName) = member;
            if (identifier == null)
                throw new NotImplementedException(
                    $"Anonymous struct members for {name} aren't supported, yet: {type}.");

            if (cliImportMemberName != null)
                throw new NotSupportedException(
                    $"CLI imports inside struct members aren't supported: {cliImportMemberName}.");

            structType.Fields.Add(
                new FieldDefinition(
                    identifier,
                    FieldAttributes.Public,
                    type.Resolve(context)));
        }

        return structType;
    }

    public TypeReference Resolve(TranslationUnitContext context) =>
        context.GetTypeReference(this) ?? throw new NotSupportedException($"Type {this} was not found.");

    public int SizeInBytes => throw new NotImplementedException($"Could not calculate size for {this} yet.");
}
