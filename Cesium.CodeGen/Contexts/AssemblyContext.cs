using System.Diagnostics;
using System.Reflection;
using System.Text;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir.TopLevel;
using Mono.Cecil;
using Mono.Cecil.Cil;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Cesium.CodeGen.Contexts;

public class AssemblyContext
{
    internal AssemblyDefinition Assembly { get; }
    public ModuleDefinition Module { get; }
    public Assembly[] ImportAssemblies { get; }
    public TypeDefinition GlobalType { get; }
    public MethodDefinition GlobalTypeStaticCtor { get; }


    internal Dictionary<string, FunctionInfo> Functions { get; } = new();

    public static AssemblyContext Create(
        AssemblyNameDefinition name,
        ModuleKind kind,
        TargetRuntimeDescriptor? targetRuntime,
        Assembly[] importAssemblies,
        string @namespace = "",
        string globalTypeFqn = "")
    {
        var assembly = AssemblyDefinition.CreateAssembly(name, "Primary", kind);
        var module = assembly.MainModule;
        var assemblyContext = new AssemblyContext(assembly, module, importAssemblies, @namespace, globalTypeFqn);

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
            if (!function.IsDefined) throw new NotSupportedException($"Function {name} not defined.");
        }

        return Assembly;
    }

    public const string ConstantPoolTypeName = "<ConstantPool>";

    private readonly Dictionary<int, TypeReference> _stubTypesPerSize = new();
    private readonly Dictionary<string, FieldReference> _fields = new();

    private readonly Lazy<TypeDefinition> _constantPool;

    private AssemblyContext(
        AssemblyDefinition assembly,
        ModuleDefinition module,
        Assembly[] importAssemblies,
        string @namespace = "",
        string globalTypeFqn = "")
    {
        Assembly = assembly;
        Module = module;
        ImportAssemblies = importAssemblies;
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

        var ctor = BuildStaticCtor();
        GlobalType.Methods.Add(ctor);
        GlobalTypeStaticCtor = ctor;
    }

    private MethodDefinition BuildStaticCtor()
    {
        var methodAttributes = MethodAttributes.Private
                               | MethodAttributes.HideBySig
                               | MethodAttributes.SpecialName
                               | MethodAttributes.RTSpecialName
                               | MethodAttributes.Static;
        MethodDefinition method = new MethodDefinition(".cctor", methodAttributes, Module.TypeSystem.Void);
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        return method;
    }

    internal void AddFieldInitialization(FieldDefinition field, Ir.Expressions.IExpression value)
    {
        var lval = new Ir.Expressions.LValues.LValueGlobalVariable(field);
        var context = new TranslationUnitContext(this);
        var scope = new FunctionScope(context, GlobalTypeStaticCtor);
        var lastOpcode = GlobalTypeStaticCtor.Body.Instructions.Last();
        if (lastOpcode.OpCode == OpCodes.Ret)
        {
            GlobalTypeStaticCtor.Body.Instructions.Remove(lastOpcode);
            lval.EmitSetValue(scope, value);
            GlobalTypeStaticCtor.Body.Instructions.Add(lastOpcode);
        }
        else
            lval.EmitSetValue(scope, value);
    }
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
