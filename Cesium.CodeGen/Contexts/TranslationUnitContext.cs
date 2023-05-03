using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using System.Diagnostics.CodeAnalysis;
using ParameterInfo = Cesium.CodeGen.Ir.ParameterInfo;
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
    /// <param name="resolvedType">A <see cref="IType"/> which fully resolves.</param>
    /// <param name="unresolvedType">A <see cref="IType"/> which blocks resolution resolves.</param>
    /// <returns>A <see cref="IType"/> which fully resolves.</returns>
    /// <exception cref="CompilationException">Throws a <see cref="CompilationException"/> if it's not possible to resolve some of the types.</exception>
    internal bool TryResolveType(IType type, [NotNullWhen(true)]out IType? resolvedType, [NotNullWhen(false)] out IType? unresolvedType)
    {
        if (type is NamedType namedType)
        {
            if (_types.TryGetValue(namedType.TypeName, out resolvedType))
            {
                unresolvedType = null;
                return true;
            }

            unresolvedType = namedType;
            return false;
        }

        if (type is PointerType pointerType)
        {
            if (TryResolveType(pointerType.Base, out resolvedType, out unresolvedType))
            {
                resolvedType = new PointerType(resolvedType);
                return true;
            }

            return false;
        }

        if (type is InPlaceArrayType arrayType)
        {
            if (TryResolveType(arrayType.Base, out resolvedType, out unresolvedType))
            {
                resolvedType = new InPlaceArrayType(resolvedType, arrayType.Size);
                return true;
            }

            return false;
        }

        if (type is StructType structType)
        {
            if (structType.Members.Count == 0 && structType.Identifier is not null)
            {
                if (_tags.TryGetValue(structType.Identifier, out var existingType))
                {
                    resolvedType = existingType;
                    unresolvedType = null;
                    return true;
                }
            }

            bool isStructResolved = true;
            var members = new List<LocalDeclarationInfo>();
            unresolvedType = null;
            foreach (var structMember in structType.Members)
            {
                if (TryResolveType(structMember.Type, out var memberResolvedType, out var memberUnresolvedType))
                {
                    members.Add(structMember with { Type = memberResolvedType });
                }
                else
                {
                    if (unresolvedType is null)
                    {
                        unresolvedType = memberResolvedType;
                    }

                    members.Add(structMember);
                }
            }

            resolvedType = new StructType(members, structType.Identifier);
            return isStructResolved;
        }

        if (type is FunctionType functionType)
        {
            bool isFunctionResolved = true;
            ParametersInfo? parametersInfo = null;
            if (functionType.Parameters is not null)
            {
                var functionParameters = functionType.Parameters;
                var parameters = new List<ParameterInfo>();
                unresolvedType = null;
                foreach (var parameterInfo in functionParameters.Parameters)
                {
                    if (TryResolveType(parameterInfo.Type, out var parameterResolvedType, out var memberUnresolvedType))
                    {
                        parameters.Add(parameterInfo with { Type = parameterResolvedType });
                    }
                    else
                    {
                        if (unresolvedType is null)
                        {
                            unresolvedType = parameterResolvedType;
                        }

                        parameters.Add(parameterInfo);
                    }
                }

                if (unresolvedType is not null)
                {
                    resolvedType = null;
                    return false;
                }

                parametersInfo = new ParametersInfo(parameters, functionParameters.IsVoid, functionParameters.IsVarArg);
            }

            if (!TryResolveType(functionType.ReturnType, out var returnResolvedType, out unresolvedType))
            {
                resolvedType = null;
                return false;
            }

            resolvedType = new FunctionType(parametersInfo, returnResolvedType);
            return true;
        }

        resolvedType = type;
        unresolvedType = null;
        return true;
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
