using System.Diagnostics;
using System.Text;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.TopLevel;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core.Exceptions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Contexts;

public class AssemblyContext
{
    internal AssemblyDefinition Assembly { get; }
    internal AssemblyDefinition MscorlibAssembly { get; }
    internal AssemblyDefinition CesiumRuntimeAssembly { get; }
    public ModuleDefinition Module { get; }
    public AssemblyDefinition[] ImportAssemblies { get; }
    public TypeDefinition GlobalType { get; }

    internal Dictionary<string, FunctionInfo> Functions { get; } = new();

    private readonly Dictionary<string, IType> _globalFields = new();
    internal IReadOnlyDictionary<string, IType> GlobalFields => _globalFields;

    public static AssemblyContext Create(
        AssemblyNameDefinition name,
        ModuleKind kind,
        TargetRuntimeDescriptor? targetRuntime,
        string[] importAssemblies,
        string mscorlibAssemblyLocation,
        string cesiumRuntimeAssemblyLocation,
        string @namespace = "",
        string globalTypeFqn = "")
    {
        var assembly = AssemblyDefinition.CreateAssembly(name, "Primary", kind);
        var module = assembly.MainModule;
        var assemblyContext = new AssemblyContext(assembly, module, importAssemblies, mscorlibAssemblyLocation, cesiumRuntimeAssemblyLocation, @namespace, globalTypeFqn);

        targetRuntime ??= TargetRuntimeDescriptor.Net60;
        assembly.CustomAttributes.Add(targetRuntime.GetTargetFrameworkAttribute(module));
        module.AssemblyReferences.Add(targetRuntime.GetSystemAssemblyReference());

        return assemblyContext;
    }

    public void EmitTranslationUnit(IEnumerable<ITopLevelNode> nodes)
    {
        var context = new TranslationUnitContext(this);
        foreach (var node in nodes)
            node.EmitTo(context);
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
    private readonly Dictionary<string, FieldReference> _stringConstantHolders = new();

    private readonly Lazy<TypeDefinition> _constantPool;
    private MethodDefinition? _globalInitializer;

    private AssemblyContext(
        AssemblyDefinition assembly,
        ModuleDefinition module,
        string[] importAssemblies,
        string mscorlibAssemblyLocation,
        string cesiumRuntimeAssemblyLocation,
        string @namespace = "",
        string globalTypeFqn = "")
    {
        Assembly = assembly;
        Module = module;
        MscorlibAssembly = AssemblyDefinition.ReadAssembly(mscorlibAssemblyLocation);
        CesiumRuntimeAssembly = AssemblyDefinition.ReadAssembly(cesiumRuntimeAssemblyLocation);
        ImportAssemblies = importAssemblies.Select(AssemblyDefinition.ReadAssembly).Union(new[] { MscorlibAssembly, CesiumRuntimeAssembly }).ToArray();
        _constantPool = new(
            () =>
            {
                var type = new TypeDefinition(@namespace, ConstantPoolTypeName, TypeAttributes.Sealed, module.TypeSystem.Object);
                module.Types.Add(type);
                return type;
            });

        if (!string.IsNullOrWhiteSpace(globalTypeFqn))
        {
            var fqnComponents = globalTypeFqn.Split('.');
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

    internal void AddGlobalField(string name, IType type)
    {
        if (_globalFields.ContainsKey(name))
            throw new CompilationException($"Cannot add a duplicate global field named \"{name}\".");

        _globalFields.Add(name, type);
    }

    public FieldDefinition ResolveGlobalField(string name, TranslationUnitContext context)
    {
        if (!_globalFields.TryGetValue(name, out var type))
        {
            throw new CompilationException($"Cannot find a global field \"{name}\".");
        }

        var field = GlobalType.Fields.FirstOrDefault(f => f.Name == name);
        if (field == null)
        {
            field = new FieldDefinition(name, FieldAttributes.Public | FieldAttributes.Static, type.Resolve(context));
            GlobalType.Fields.Add(field);
        }

        return field;
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
        if (_stringConstantHolders.TryGetValue(stringConstant, out var field))
            return field;

        var encoding = Encoding.UTF8;
        var bufferSize = encoding.GetByteCount(stringConstant) + 1;
        var data = new byte[bufferSize];
        var writtenBytes = encoding.GetBytes(stringConstant, data);
        Debug.Assert(writtenBytes == bufferSize - 1);

        var type = GetStubType(bufferSize);
        field = GenerateFieldForStringConstant(type, data);
        _stringConstantHolders.Add(stringConstant, field);

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
            Module.ImportReference(MscorlibAssembly.GetType("System.ValueType")))
        {
            PackingSize = 1,
            ClassSize = size
        };

        _constantPool.Value.NestedTypes.Add(type);
        _stubTypesPerSize.Add(size, type);

        return type;
    }

    private FieldReference GenerateFieldForStringConstant(
        TypeReference stubStructType,
        byte[] contentWithTerminatingZero)
    {
        var number = _stringConstantHolders.Count;
        var fieldName = $"ConstStringBuffer{number}";

        var field = new FieldDefinition(fieldName, FieldAttributes.Static | FieldAttributes.InitOnly, stubStructType)
        {
            InitialValue = contentWithTerminatingZero
        };

        var constantPool = _constantPool.Value;
        constantPool.Fields.Add(field);
        return field;
    }
}
