using System.Diagnostics;
using System.Text;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;

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

    private readonly Dictionary<string, IType> _globalFields = new();
    internal IReadOnlyDictionary<string, IType> GlobalFields => _globalFields;
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
        module.AssemblyReferences.Add(targetRuntime.GetSystemAssemblyReference());

        return assemblyContext;
    }

    public void EmitTranslationUnit(string name, Ast.TranslationUnit translationUnit)
    {
        var nodes = translationUnit.ToIntermediate();
        var context = new TranslationUnitContext(this, name);
        var scope = context.GetInitializerScope();
        nodes = nodes.Select(node => node.Lower(scope));
        foreach (var node in nodes)
            node.EmitTo(scope);
    }

    /// <summary>Do final code generation tasks, analogous to linkage.</summary>
    /// <remarks>As we link code on the fly, here we only need to check there are no unlinked functions left.</remarks>
    public AssemblyDefinition VerifyAndGetAssembly()
    {
        foreach (var (name, function) in Functions)
        {
            if (!function.IsDefined) throw new CompilationException($"Function {name} not defined.");
        }

        FinishGlobalInitializer();
        return Assembly;
    }

    public const string ConstantPoolTypeName = "<ConstantPool>";

    private readonly Dictionary<int, TypeReference> _stubTypesPerSize = new();
    private readonly Dictionary<string, FieldReference> _dataConstantHolders = new();

    private readonly Lazy<TypeDefinition> _constantPool;
    private MethodDefinition? _globalInitializer;

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
        ImportAssemblies = compilationOptions.ImportAssemblies.Select(AssemblyDefinition.ReadAssembly).Union(new[] { MscorlibAssembly, CesiumRuntimeAssembly }).ToArray();
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
    }

    internal void AddAssemblyLevelField(string name, IType type)
    {
        if (_globalFields.ContainsKey(name))
            throw new CompilationException($"Cannot add a duplicate global field named \"{name}\".");

        _globalFields.Add(name, type);
    }

    public FieldDefinition? ResolveAssemblyLevelField(string name, TranslationUnitContext context)
    {
        if (!_globalFields.TryGetValue(name, out var type))
        {
            return null;
        }

        return GlobalType.GetOrAddField(context, type, name);
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
        if (_dataConstantHolders.TryGetValue(stringConstant, out var field))
            return field;

        var encoding = Encoding.UTF8;
        var bufferSize = encoding.GetByteCount(stringConstant) + 1;
        var data = new byte[bufferSize];
        var writtenBytes = encoding.GetBytes(stringConstant, data);
        Debug.Assert(writtenBytes == bufferSize - 1);

        var type = GetStubType(bufferSize);
        field = GenerateFieldForDataConstant(type, data);
        _dataConstantHolders.Add(stringConstant, field);

        return field;
    }

    public FieldReference GetConstantPoolReference(byte[] dataConstant)
    {
        if (dataConstant.Length >= 10)
        {
            // Do not attempt to put large arrays in cache.
            // Most likely they are single use in the application.
            // No chance that they would be cached by values.
            var bufferSize = dataConstant.Length;
            var type = GetStubType(bufferSize);
            return GenerateFieldForDataConstant(type, dataConstant);
        }
        else
        {
            var hash = string.Join("", dataConstant.Select(_ => _.ToString("X2")));
            if (_dataConstantHolders.TryGetValue(hash, out var field))
                return field;

            var bufferSize = dataConstant.Length;
            var type = GetStubType(bufferSize);
            field = GenerateFieldForDataConstant(type, dataConstant);
            _dataConstantHolders.Add(hash, field);

            return field;
        }
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
            Module.ImportReference(MscorlibAssembly.GetType("System.ValueType")))
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
}
