using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using PointerType = Cesium.CodeGen.Ir.Types.PointerType;

namespace Cesium.CodeGen.Contexts;

public record TranslationUnitContext(AssemblyContext AssemblyContext, string Name)
{
    public AssemblyDefinition Assembly => AssemblyContext.Assembly;
    public ModuleDefinition Module => AssemblyContext.Module;
    public TypeSystem TypeSystem => Module.TypeSystem;
    internal CTypeSystem CTypeSystem { get; } = new();
    public TypeDefinition ModuleType => Module.GetType("<Module>");
    public TypeDefinition GlobalType => AssemblyContext.GlobalType;

    private TypeDefinition? _translationUnitLevelType;

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
    private readonly Dictionary<string, IType> _tags = new();

    internal void GenerateType(string name, IGeneratedType type)
    {
        var typeReference = type.Emit(name, this);
        _generatedTypes.Add(type, typeReference);
    }

    internal void AddTypeDefinition(string name, IType type) => _types.Add(name, type);

    internal void AddTagDefinition(string name, IType type) => _tags.Add(name, type);

    internal IType? TryGetType(string name) => _types.GetValueOrDefault(name);

    /// <summary>
    /// Recursively resolve the passed type and all its members, replacing `NamedType` in any points with their actual instantiations in the current context.
    /// </summary>
    /// <param name="type">Type which should be resolved.</param>
    /// <returns>A <see cref="IType"/> which fully resolves.</returns>
    /// <exception cref="CompilationException">Throws a <see cref="CompilationException"/> if it's not possible to resolve some of the types.</exception>
    internal IType ResolveType(IType type)
    {
        if (type is NamedType namedType)
        {
            return _types.GetValueOrDefault(namedType.TypeName) ?? throw new CompilationException($"Cannot resolve type {namedType.TypeName}");
        }

        if (type is PointerType pointerType)
        {
            return new PointerType(ResolveType(pointerType.Base));
        }

        if (type is InPlaceArrayType arrayType)
        {
            return new InPlaceArrayType(ResolveType(arrayType.Base), arrayType.Size);
        }

        if (type is StructType structType)
        {
            if (structType.Members.Count == 0 && structType.Identifier is not null)
            {
                if (_tags.TryGetValue(structType.Identifier, out var existingType))
                {
                    return existingType;
                }
            }

            var members = structType.Members
                .Select(structMember => structMember with { Type = ResolveType(structMember.Type) })
                .ToList();
            return new StructType(members, structType.Identifier);
        }

        if (type is FunctionType functionType)
        {
            ParametersInfo? parametersInfo = null;
            if (functionType.Parameters is not null)
            {
                var functionParameters = functionType.Parameters;
                var parameters = functionParameters.Parameters.Select(parameterInfo => parameterInfo with { Type = ResolveType(parameterInfo.Type) }).ToArray();
                parametersInfo = new ParametersInfo(parameters, functionParameters.IsVoid, functionParameters.IsVarArg);
            }

            return new FunctionType(parametersInfo, ResolveType(functionType.ReturnType));
        }

        return type;
    }

    internal TypeReference? GetTypeReference(IGeneratedType type) => _generatedTypes.GetValueOrDefault(type);

    private readonly Dictionary<string, IType> _translationUnitLevelFieldTypes = new();
    internal void AddTranslationUnitLevelField(string identifier, IType type)
    {
        _translationUnitLevelFieldTypes.Add(identifier, type);
    }

    internal FieldReference? ResolveTranslationUnitField(string name)
    {
        var type = _translationUnitLevelFieldTypes.GetValueOrDefault(name);
        if (type == null) return null;

        var containingType = _translationUnitLevelType ??= CreateTranslationUnitLevelType();
        return containingType.GetOrAddField(this, type, name);
    }

    private TypeDefinition CreateTranslationUnitLevelType()
    {
        var type = new TypeDefinition("", $"{Name}<Statics>", TypeAttributes.Abstract | TypeAttributes.Sealed);
        Module.Types.Add(type);
        return type;
    }
}
