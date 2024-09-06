using System.Diagnostics;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using PointerType = Cesium.CodeGen.Ir.Types.PointerType;

namespace Cesium.CodeGen.Contexts;

public class TranslationUnitContext
{
    public AssemblyContext AssemblyContext { get; }
    public string Name { get; }

    public AssemblyDefinition Assembly => AssemblyContext.Assembly;
    public ModuleDefinition Module => AssemblyContext.Module;
    public TypeSystem TypeSystem => Module.TypeSystem;
    public TypeDefinition ModuleType => Module.GetType("<Module>");
    public TypeDefinition GlobalType => AssemblyContext.GlobalType;

    private TypeDefinition? _translationUnitLevelType;

    internal Dictionary<string, FunctionInfo> Functions => AssemblyContext.Functions;

    private GlobalConstructorScope? _initializerScope;

    public TranslationUnitContext(AssemblyContext assemblyContext, string name)
    {
        AssemblyContext = assemblyContext;
        Name = name;
    }

    /// <remarks>
    /// Architecturally, there's only one global initializer at the assembly level. But every translation unit may have
    /// its own set of definitions and thus its own initializer scope built around the same method body.
    /// </remarks>
    internal GlobalConstructorScope GetInitializerScope() =>
        _initializerScope ??= new GlobalConstructorScope(this);

    internal FunctionInfo? GetFunctionInfo(string identifier) =>
        Functions.GetValueOrDefault(identifier);

    internal void DeclareFunction(string identifier, FunctionInfo functionInfo)
    {
        var existingDeclaration = Functions.GetValueOrDefault(identifier);
        if (existingDeclaration is null)
        {
            if (functionInfo.CliImportMember is not null)
            {
                var method = this.MethodLookup(functionInfo.CliImportMember, functionInfo.Parameters!, functionInfo.ReturnType);
                functionInfo = ProcessCliImport(functionInfo, method);
            }
            Functions.Add(identifier, functionInfo);
        }
        else
        {
            existingDeclaration.VerifySignatureEquality(identifier, functionInfo.Parameters, functionInfo.ReturnType);
            if (functionInfo.CliImportMember is not null && existingDeclaration.CliImportMember is not null)
            {
                var method = this.MethodLookup(functionInfo.CliImportMember, functionInfo.Parameters!, functionInfo.ReturnType);
                var methodReference = existingDeclaration.MethodReference!;
                if (!method.FullName.Equals(methodReference.FullName))
                {
                    throw new CompilationException($"Function {identifier} already defined as as CLI-import with {methodReference.FullName}.");
                }
            }

            var mergedStorageClass = existingDeclaration.StorageClass != StorageClass.Auto
                ? existingDeclaration.StorageClass
                : functionInfo.StorageClass;
            var mergedIsDefined = existingDeclaration.IsDefined || functionInfo.IsDefined;
            existingDeclaration.Parameters = functionInfo.Parameters;
            existingDeclaration.IsDefined = mergedIsDefined;
            existingDeclaration.StorageClass = mergedStorageClass;
        }
    }

    internal MethodDefinition DefineMethod(
        string name,
        StorageClass storageClass,
        IType returnType,
        ParametersInfo? parameters)
    {
            var owningType = storageClass == StorageClass.Auto ? GlobalType : GetOrCreateTranslationUnitType();
            var method = owningType.DefineMethod(
                this,
                name,
                returnType.Resolve(this),
                parameters);
        var existingDeclaration = Functions.GetValueOrDefault(name);
        Debug.Assert(existingDeclaration is not null, $"Attempt to define method for undeclared function {name}");
        Functions[name] = existingDeclaration with { MethodReference = method };
            return method;
    }

    private readonly Dictionary<string, IType> _types = new();
    private readonly Dictionary<string, IType> _tags = new();

    internal void GenerateType(string name, IGeneratedType type)
    {
        AssemblyContext.GenerateType(this, name, (StructType)type);
    }

    internal void AddTypeDefinition(string name, IType type)
    {
        if (_types.ContainsKey(name))
            throw new CompilationException($"Type definition {name} was already defined.");

        _types.Add(name, type);
    }

    internal void AddTagDefinition(string name, IType type)
    {
        if (_tags.TryGetValue(name, out var existingType))
        {
            if (type == existingType) return;
            if (existingType.TypeKind != type.TypeKind)
            {
                throw new CompilationException($"Tag kind {GetTypeKind(type.TypeKind)} type {name} was already defined as {GetTypeKind(existingType.TypeKind)}");
            }
        }

        _tags.Add(name, type);

        string GetTypeKind(TypeKind type) => type switch
        {
            TypeKind.Struct => "struct",
            TypeKind.Enum => "enum",
            _ => throw new InvalidOperationException($"Unsupported type {type} used."),
        };
    }

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

        if (type is ConstType constType)
        {
            return new ConstType(ResolveType(constType.Base));
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
            if (structType.Members.Count != 0 && structType.Identifier is not null)
            {
                if (_types.TryGetValue(structType.Identifier, out var existingType))
                {
                    if (existingType is StructType existingStructType && existingStructType.Members.Count == 0)
                    {
                        existingStructType.Members = members;
                        return existingType;
                    }
                }
            }

            return new StructType(members, structType.IsUnion, structType.Identifier);
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

    internal TypeReference? GetTypeReference(IGeneratedType type) => AssemblyContext.GetTypeReference(type);

    private readonly Dictionary<string, IType> _translationUnitLevelFieldTypes = new();
    internal void AddTranslationUnitLevelField(StorageClass storageClass, string identifier, IType type)
    {
        switch (storageClass)
        {
            case StorageClass.Static: // file-level
                _translationUnitLevelFieldTypes.Add(identifier, type);
                break;
            case StorageClass.Auto: // assembly-level
            case StorageClass.Extern: // assembly-level
                AssemblyContext.AddAssemblyLevelField(identifier, storageClass, type);
                break;
            default:
                throw new CompilationException($"Global variable of storage class {storageClass} is not supported.");
        }
    }

    internal FieldReference? ResolveTranslationUnitField(string name)
    {
        var type = _translationUnitLevelFieldTypes.GetValueOrDefault(name);
        if (type == null) return null;

        var containingType = GetOrCreateTranslationUnitType();
        return containingType.GetOrAddField(this, type, name);
    }

    private TypeDefinition GetOrCreateTranslationUnitType()
    {
        _translationUnitLevelType ??= CreateTranslationUnitLevelType();
        return _translationUnitLevelType;
    }

    private TypeDefinition CreateTranslationUnitLevelType()
    {
        var type = new TypeDefinition(
            "",
            $"{Name}<Statics>",
            TypeAttributes.Abstract | TypeAttributes.Sealed,
            Module.TypeSystem.Object);
        Module.Types.Add(type);
        return type;
    }

    private FunctionInfo ProcessCliImport(FunctionInfo declaration, MethodReference implementation)
    {
        return declaration with
        {
            MethodReference = implementation,
            Parameters = declaration.Parameters is null ? null : declaration.Parameters with
            {
                Parameters = ProcessParameters(declaration.Parameters.Parameters)
            }
        };

        List<ParameterInfo> ProcessParameters(ICollection<ParameterInfo> parameters)
        {
            var areParametersValid =
                implementation.Parameters.Count == parameters.Count
                || declaration.Parameters.IsVarArg; // TODO[#487]: A better check for interop functions + vararg.
            if (!areParametersValid)
            {
                throw new CompilationException(
                    $"Parameter count for function {declaration.CliImportMember} " +
                    $"doesn't match the parameter count of imported CLI method {implementation.FullName}.");
            }

            return parameters.Zip(implementation.Parameters)
                .Select(pair =>
                {
                    var (declared, actual) = pair;
                    var type = WrapInteropType(actual.ParameterType);
                    if (type == null) return declared;
                    return declared with { Type = type };
                }).ToList();
        }

        InteropType? WrapInteropType(TypeReference actual)
        {
            if (actual.FullName == TypeSystemEx.VoidPtrFullTypeName)
                return new InteropType(actual);

            if (actual.IsGenericInstance)
            {
                var parent = actual.GetElementType();
                if (parent.FullName == TypeSystemEx.CPtrFullTypeName
                    || parent.FullName == TypeSystemEx.FuncPtrFullTypeName)
                {
                    return new InteropType(actual);
                }
            }

            return null;
        }
    }
}
