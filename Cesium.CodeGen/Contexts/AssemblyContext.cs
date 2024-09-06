using System.Collections;
using System.Diagnostics;
using System.Text;
using Cesium.Ast;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Contexts.Utilities;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Emitting;
using Cesium.CodeGen.Ir.Lowering;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Cesium.CodeGen.Contexts;

public class AssemblyContext
{
    internal AssemblyDefinition Assembly { get; }
    public TargetArchitectureSet ArchitectureSet { get; }
    internal AssemblyDefinition MscorlibAssembly { get; }
    internal AssemblyDefinition CesiumRuntimeAssembly { get; }
    public ModuleDefinition Module { get; }
    public AssemblyDefinition[] ImportAssemblies { get; }
    public TypeDefinition GlobalType { get; }

    internal Dictionary<string, FunctionInfo> Functions { get; } = new();

    private readonly Dictionary<string, VariableInfo> _globalFields = new();

    public CompilationOptions CompilationOptions { get; }

    public static AssemblyContext Create(
        AssemblyNameDefinition name,
        CompilationOptions compilationOptions)
    {
        var assembly = AssemblyDefinition.CreateAssembly(name, "Primary", compilationOptions.ModuleKind);
        var module = assembly.MainModule;
        var assemblyContext = new AssemblyContext(assembly, module, compilationOptions);

        var targetRuntime = compilationOptions.TargetRuntime;
        assembly.CustomAttributes.Add(targetRuntime.GetTargetFrameworkAttribute(module));

        return assemblyContext;
    }

    public void EmitTranslationUnit(string name, TranslationUnit translationUnit)
    {
        var nodes = translationUnit.ToIntermediate();
        var context = new TranslationUnitContext(this, name);
        var scope = context.GetInitializerScope();
        nodes = nodes.Select(node => BlockItemLowering.LowerDeclaration(scope, node)).ToList();
        foreach (var node in nodes)
            BlockItemEmitting.EmitCode(scope, node);
    }

    /// <summary>Do final code generation tasks, analogous to linkage.</summary>
    /// <remarks>As we link code on the fly, here we only need to check there are no unlinked functions left.</remarks>
    public AssemblyDefinition VerifyAndGetAssembly()
    {
        foreach (var (name, function) in Functions)
        {
            if (!function.IsDefined)
            {
                if (function.DllLibraryName == null) throw new CompilationException($"Function {name} not defined.");
                var funcDef = function.MethodReference!.Resolve();
                funcDef.Attributes = MethodAttributes.Public | MethodAttributes.PInvokeImpl | MethodAttributes.Static | MethodAttributes.HideBySig;
                ModuleReference? dll = Module.ModuleReferences.FirstOrDefault(_ => _.Name == function.DllLibraryName);
                if (dll == null)
                {
                    dll = new ModuleReference(function.DllLibraryName);
                    Module.ModuleReferences.Add(dll);
                }
                string entryPoint = function.DllImportNameStrip != null ? name.Replace(function.DllImportNameStrip, null) : name;
                funcDef.PInvokeInfo = new PInvokeInfo(PInvokeAttributes.NoMangle | PInvokeAttributes.SupportsLastError, entryPoint, dll);
                function.IsDefined = true;
            }
        }

        FinishGlobalInitializer();
        return Assembly;
    }

    public const string ConstantPoolTypeName = "<ConstantPool>";

    private readonly Dictionary<int, TypeReference> _stubTypesPerSize = new();
    private readonly Dictionary<ByteArrayWrapper, FieldReference> _dataConstantHolders = new();
    private readonly Dictionary<IGeneratedType, TypeReference> _generatedTypes = new();

    private readonly Lazy<TypeDefinition> _constantPool;
    private MethodDefinition? _globalInitializer;

    private readonly TypeReference _runtimeCPtr;
    private readonly ConversionMethodCache _cPtrConverterCache;
    public MethodReference CPtrConverter(TypeReference argument) =>
        _cPtrConverterCache.GetOrImportMethod(argument);

    public TypeReference RuntimeVoidPtr { get; }
    private readonly Lazy<MethodReference> _voidPtrConverter;
    public MethodReference VoidPtrConverter => _voidPtrConverter.Value;

    private readonly TypeReference _runtimeFuncPtr;
    private readonly ConversionMethodCache _funcPtrConstructorCache;
    public MethodReference FuncPtrConstructor(TypeReference argument) =>
        _funcPtrConstructorCache.GetOrImportMethod(argument);

    private AssemblyContext(
        AssemblyDefinition assembly,
        ModuleDefinition module,
        CompilationOptions compilationOptions)
    {
        Assembly = assembly;
        ArchitectureSet = compilationOptions.TargetArchitectureSet;
        Module = module;
        CompilationOptions = compilationOptions;

        MscorlibAssembly = AssemblyDefinition.ReadAssembly(compilationOptions.CorelibAssembly);
        CesiumRuntimeAssembly = AssemblyDefinition.ReadAssembly(compilationOptions.CesiumRuntime);
        ImportAssemblies = compilationOptions.ImportAssemblies.Select(AssemblyDefinition.ReadAssembly).Union(new[] { MscorlibAssembly, CesiumRuntimeAssembly }).Distinct().ToArray();
        _constantPool = new(
            () =>
            {
                var type = new TypeDefinition(compilationOptions.Namespace, ConstantPoolTypeName, TypeAttributes.Sealed, module.TypeSystem.Object);
                module.Types.Add(type);
                return type;
            });

        if (!string.IsNullOrWhiteSpace(compilationOptions.GlobalClassFqn))
        {
            var fqnComponents = compilationOptions.GlobalClassFqn.Split('.');
            var typeName = fqnComponents[^1];
            var typeNamespace = string.Join('.', fqnComponents.SkipLast(1));

            GlobalType = new TypeDefinition(
                typeNamespace,
                typeName,
                TypeAttributes.Class
                | TypeAttributes.Public
                | TypeAttributes.Abstract
                | TypeAttributes.Sealed,
                module.TypeSystem.Object);
            module.Types.Add(GlobalType);
        }
        else
        {
            GlobalType = Module.GetType("<Module>");
        }

        TypeDefinition GetRuntimeType(string typeName) =>
            CesiumRuntimeAssembly.GetType(typeName) ??
            throw new AssertException($"Could not find type {typeName} in the runtime assembly.");

        _runtimeCPtr = Module.ImportReference(GetRuntimeType(TypeSystemEx.CPtrFullTypeName));
        _cPtrConverterCache = new ConversionMethodCache(
            _runtimeCPtr,
            ReturnType: _runtimeCPtr.MakeGenericInstanceType(_runtimeCPtr.GenericParameters.Single()),
            "op_Implicit",
            Module);

        RuntimeVoidPtr = Module.ImportReference(GetRuntimeType(TypeSystemEx.VoidPtrFullTypeName));
        _voidPtrConverter = new(() => GetImplicitCastOperator(TypeSystemEx.VoidPtrFullTypeName));

        _runtimeFuncPtr = Module.ImportReference(GetRuntimeType(TypeSystemEx.FuncPtrFullTypeName));
        _funcPtrConstructorCache = new ConversionMethodCache(
            _runtimeFuncPtr,
            ReturnType: null,
            ".ctor",
            Module);

        _importedActionDelegates = new("System", "Action", Module);
        _importedFuncDelegates = new("System", "Func", Module);

        MethodReference GetImplicitCastOperator(string typeName)
        {
            var type = GetRuntimeType(typeName);
            return Module.ImportReference(type.Methods.Single(m => m.Name == "op_Implicit"));
        }
    }

    public TypeReference RuntimeCPtr(TypeReference typeReference)
    {
        return _runtimeCPtr.MakeGenericInstanceType(typeReference);
    }

    public TypeReference RuntimeFuncPtr(TypeReference delegateTypeReference)
    {
        return _runtimeFuncPtr.MakeGenericInstanceType(delegateTypeReference);
    }

    private readonly GenericDelegateTypeCache _importedActionDelegates;
    private readonly GenericDelegateTypeCache _importedFuncDelegates;

    /// <summary>
    /// Resolves a standard delegate type (i.e. an <see cref="Action"/> or a <see cref="Func{TResult}"/>), depending on
    /// the return type.
    /// </summary>
    public TypeReference StandardDelegateType(TypeReference returnType, IEnumerable<TypeReference> arguments)
    {
        var isAction = returnType == Module.TypeSystem.Void;
        var typeArguments = (isAction ? arguments : arguments.Append(returnType)).ToArray();
        var typeArgumentCount = typeArguments.Length;
        if (typeArgumentCount > 16)
        {
            throw new WipException(
                493,
                $"Mapping of function for argument count {typeArgumentCount} is not supported.");
        }

        var delegateCache = isAction ? _importedActionDelegates : _importedFuncDelegates;
        var delegateType = delegateCache.GetDelegateType(typeArguments.Length);
        return typeArguments.Length == 0
            ? delegateType
            : delegateType.MakeGenericInstanceType(typeArguments);
    }

    internal VariableInfo? GetGlobalField(string identifier)
    {
        if (_globalFields.TryGetValue(identifier, out var value)) return value;

        return null;
    }

    internal void AddAssemblyLevelField(string name, StorageClass storageClass, IType type)
    {
        if (_globalFields.TryGetValue(name, out var globalField))
        {
            if (globalField.StorageClass != StorageClass.Extern && storageClass != StorageClass.Extern)
                throw new CompilationException($"Cannot add a duplicate global field named \"{name}\".");

            return;
        }

        _globalFields.Add(name, new (storageClass, type, null));
    }

    public FieldDefinition? ResolveAssemblyLevelField(string name, TranslationUnitContext context)
    {
        if (!_globalFields.TryGetValue(name, out var type))
        {
            return null;
        }

        return GlobalType.GetOrAddField(context, type.Type, name);
    }

    /// <summary>Returns either a module static constructor or a static constructor of the global type.</summary>
    public MethodDefinition GetGlobalInitializer()
    {
        if (_globalInitializer != null) return _globalInitializer;
        var methodAttributes =
            MethodAttributes.Private
            | MethodAttributes.HideBySig
            | MethodAttributes.SpecialName
            | MethodAttributes.RTSpecialName
            | MethodAttributes.Static;
        _globalInitializer = new MethodDefinition(".cctor", methodAttributes, Module.TypeSystem.Void);
        GlobalType.Methods.Add(_globalInitializer);
        return _globalInitializer;
    }

    private void FinishGlobalInitializer()
    {
        if (_globalInitializer != null)
            _globalInitializer.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    }

    public FieldReference GetConstantPoolReference(string stringConstant)
    {
        var encoding = Encoding.UTF8;
        var bufferSize = encoding.GetByteCount(stringConstant) + 1;
        var data = new byte[bufferSize];
        var writtenBytes = encoding.GetBytes(stringConstant, data);
        Debug.Assert(writtenBytes == bufferSize - 1);
        return GetConstantPoolReference(data);
    }

    public FieldReference GetConstantPoolReference(byte[] dataConstant)
    {
        var wrapper = new ByteArrayWrapper(dataConstant);
        if (_dataConstantHolders.TryGetValue(wrapper, out var field))
            return field;

        var bufferSize = dataConstant.Length;
        var type = GetStubType(bufferSize);
        field = GenerateFieldForDataConstant(type, dataConstant);
        _dataConstantHolders.Add(wrapper, field);

        return field;
    }

    private TypeReference GetStubType(int size)
    {
        if (_stubTypesPerSize.TryGetValue(size, out var typeRef))
            return typeRef;

        var stubStructTypeName = $"<ConstantPoolItemType{size}>";

        var type = new TypeDefinition(
            "",
            stubStructTypeName,
            TypeAttributes.Sealed | TypeAttributes.ExplicitLayout | TypeAttributes.NestedPrivate,
            Module.ImportReference(new TypeReference("System", "ValueType", MscorlibAssembly.MainModule, MscorlibAssembly.MainModule.TypeSystem.CoreLibrary)))
        {
            PackingSize = 1,
            ClassSize = size
        };

        _constantPool.Value.NestedTypes.Add(type);
        _stubTypesPerSize.Add(size, type);

        return type;
    }

    private FieldReference GenerateFieldForDataConstant(
        TypeReference stubStructType,
        byte[] contentWithTerminatingZero)
    {
        var number = _dataConstantHolders.Count;
        var fieldName = $"ConstDataBuffer{number}";

        var field = new FieldDefinition(fieldName, FieldAttributes.Static | FieldAttributes.InitOnly, stubStructType)
        {
            InitialValue = contentWithTerminatingZero
        };

        var constantPool = _constantPool.Value;
        constantPool.Fields.Add(field);
        return field;
    }

    internal void GenerateType(TranslationUnitContext context, string name, StructType type)
    {
        if (!_generatedTypes.ContainsKey(type))
        {
            var typeReference = type.StartEmit(name, context);
            _generatedTypes.Add(type, typeReference);
            foreach (var member in type.Members)
            {
                if (member.Type is StructType structType)
                {
                    structType.EmitType(context);
                }

                if (member.Type is Ir.Types.PointerType { Base: StructType structTypePtr })
                {
                    structTypePtr.EmitType(context);
                }
            }

            type.FinishEmit(typeReference, name, context);
        }
    }

    internal TypeReference? GetTypeReference(IGeneratedType type)
    {
        return _generatedTypes.GetValueOrDefault(type);
    }

    private struct ByteArrayWrapper
    {
        private readonly byte[] _value;
        private int? _hash;

        public ByteArrayWrapper(byte[] value)
        {
            _value = value;
        }

        public override bool Equals(object? obj)
        {
            return obj is ByteArrayWrapper other && _value.AsSpan().SequenceEqual(other._value);
        }

        public override int GetHashCode()
        {
            return _hash ??= StructuralComparisons.StructuralEqualityComparer.GetHashCode(_value);
        }
    }
}
