using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;

namespace Cesium.CodeGen.Contexts;

public record TranslationUnitContext(AssemblyContext AssemblyContext)
{
    public AssemblyDefinition Assembly => AssemblyContext.Assembly;
    public ModuleDefinition Module => AssemblyContext.Module;
    public TypeSystem TypeSystem => Module.TypeSystem;
    internal CTypeSystem CTypeSystem { get; } = new CTypeSystem();
    public TypeDefinition ModuleType => Module.GetType("<Module>");
    public TypeDefinition GlobalType => AssemblyContext.GlobalType;

    internal Dictionary<string, FunctionInfo> Functions => AssemblyContext.Functions;

    private GlobalConstructorScope? _initializerScope;

    /// <remarks>
    /// Architecturally, there's only one global initializer at the assembly level. But every translation unit may have
    /// its own set of definitions and thus its own initializer scope built around the same method body.
    /// </remarks>
    internal GlobalConstructorScope GetInitializerScope() =>
        _initializerScope ??= new GlobalConstructorScope(this);

    private readonly Dictionary<IGeneratedType, TypeReference> _generatedTypes = new();
    private readonly Dictionary<string, IType> _types = new();

    internal void GenerateType(string name, IGeneratedType type)
    {
        var typeReference = type.Emit(name, this);
        _generatedTypes.Add(type, typeReference);
    }

    internal void AddTypeDefinition(string name, IType type) => _types.Add(name, type);

    internal IType? TryGetType(string name) => _types.GetValueOrDefault(name);

    internal IType DisambiguateType(IType type)
    {
        if (type is NamedType namedType)
        {
            return _types.GetValueOrDefault(namedType.TypeName) ?? throw new CompilationException($"Cannot resolve type {namedType.TypeName}");
        }

        if (type is Ir.Types.PointerType pointerType)
        {
            return new Ir.Types.PointerType(DisambiguateType(pointerType.Base));
        }

        if (type is InPlaceArrayType arrayType)
        {
            return new InPlaceArrayType(DisambiguateType(arrayType.Base), arrayType.Size);
        }

        if (type is StructType structType)
        {
            var members = structType.Members.Select(structMember => new LocalDeclarationInfo(DisambiguateType(structMember.Type), structMember.Identifier, structMember.CliImportMemberName)).ToList();
            return new StructType(members);
        }

        return type;
    }

    internal TypeReference? GetTypeReference(IGeneratedType type) => _generatedTypes.GetValueOrDefault(type);
    internal TypeReference? GetTypeReference(string typeName) => _types.GetValueOrDefault(typeName)?.Resolve(this);
}
