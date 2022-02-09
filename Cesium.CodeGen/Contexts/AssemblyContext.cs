using System.Diagnostics;
using System.Reflection;
using System.Text;
using Cesium.CodeGen.Ir.TopLevel;
using Mono.Cecil;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Cesium.CodeGen.Contexts;

public record AssemblyContext(AssemblyDefinition Assembly, ModuleDefinition Module, Assembly[] ImportAssemblies)
{
    public static AssemblyContext Create(
        AssemblyNameDefinition name,
        ModuleKind kind,
        TargetRuntimeDescriptor? targetRuntime,
        Assembly[] importAssemblies)
    {
        var assembly = AssemblyDefinition.CreateAssembly(name, "Primary", kind);
        var module = assembly.MainModule;
        var assemblyContext = new AssemblyContext(assembly, module, importAssemblies);

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

        foreach (var (name, function) in context.Functions)
        {
            if (!function.IsDefined) throw new NotSupportedException($"Function {name} not defined.");
        }
    }

    public const string ConstantPoolTypeName = "<ConstantPool>";

    private readonly Dictionary<int, TypeReference> _stubTypesPerSize = new();
    private readonly Dictionary<string, FieldReference> _fields = new();

    private readonly Lazy<TypeDefinition> _constantPool = new(
        () =>
        {
            var type = new TypeDefinition("", ConstantPoolTypeName, TypeAttributes.Sealed, Module.TypeSystem.Object);
            Module.Types.Add(type);
            return type;
        });

    public FieldReference GetConstantPoolReference(string stringConstant)
    {
        if (_fields.TryGetValue(stringConstant, out var field))
            return field;

        var encoding = Encoding.UTF8;
        var bufferSize = encoding.GetByteCount(stringConstant) + 1;
        var data = new byte[bufferSize];
        var writtenBytes = encoding.GetBytes(stringConstant, data);
        Debug.Assert(writtenBytes == bufferSize - 1);

        var type = GetStubType(bufferSize);
        field = GenerateFieldForStringConstant(type, data);
        _fields.Add(stringConstant, field);

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
            Module.ImportReference(typeof(ValueType)))
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
        var number = _fields.Count;
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
