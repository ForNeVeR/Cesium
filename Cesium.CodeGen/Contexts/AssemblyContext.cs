using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    private readonly Dictionary<ByteArrayWrapper, FieldReference> _dataConstantHolders = new(new ByteArrayEqualityComparer());

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

    private class ByteArrayEqualityComparer : IEqualityComparer<ByteArrayWrapper>
    {
        public bool Equals(ByteArrayWrapper? x, ByteArrayWrapper? y)
        {
            if (x is null && y is null)
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (x.Value.Length != y.Value.Length)
            {
                return false;
            }

            for (int i = 0; i < x.Value.Length; i++)
            {
                if (x.Value[i] != y.Value[i]) return false;
            }

            return true;
        }

        public int GetHashCode([DisallowNull] ByteArrayWrapper obj)
        {
            return obj.GetHashCode();
        }
    }

    private class ByteArrayWrapper
    {
        public ByteArrayWrapper(byte[] value)
        {
            Value = value;
        }
        public byte[] Value { get; }

        private int? hash;

        public override int GetHashCode()
        {
            // Borrowed from https://github.com/jitbit/MurmurHash.net/blob/master/MurmurHash.cs
            int length = Value.Length;
            if (length == 0)
                return 0;

            if (hash.HasValue)
                return hash.Value;

            uint seed = 0xc58f1a7a;
            const uint m = 0x5bd1e995;
            const int r = 24;

            uint h = seed ^ (uint)length;
            int currentIndex = 0;
            while (length >= 4)
            {
                uint k = (uint)(Value[currentIndex++] | Value[currentIndex++] << 8 | Value[currentIndex++] << 16 | Value[currentIndex++] << 24);
                k *= m;
                k ^= k >> r;
                k *= m;

                h *= m;
                h ^= k;
                length -= 4;
            }
            switch (length)
            {
                case 3:
                    h ^= (ushort)(Value[currentIndex++] | Value[currentIndex++] << 8);
                    h ^= (uint)(Value[currentIndex] << 16);
                    h *= m;
                    break;
                case 2:
                    h ^= (ushort)(Value[currentIndex++] | Value[currentIndex] << 8);
                    h *= m;
                    break;
                case 1:
                    h ^= Value[currentIndex];
                    h *= m;
                    break;
                default:
                    break;
            }

            h ^= h >> 13;
            h *= m;
            h ^= h >> 15;

            hash = unchecked((int)h);
            return hash.Value;
        }
    }
}
