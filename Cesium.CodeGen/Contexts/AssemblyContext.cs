using System.Diagnostics;
using System.Text;
using Mono.Cecil;

namespace Cesium.CodeGen.Contexts;

public record AssemblyContext(AssemblyDefinition Assembly, ModuleDefinition Module)
{
    private readonly Dictionary<int, TypeReference> _stubTypesPerSize = new();
    private readonly Dictionary<string, FieldReference> _fields = new();
    private readonly Lazy<TypeDefinition> _constantPool = new(
        () =>
        {
            var type = new TypeDefinition("", "<ConstantPool>", TypeAttributes.Sealed);
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

        return GenerateFieldForStringConstant(type, data);
    }

    private TypeReference GetStubType(int size)
    {
        if (_stubTypesPerSize.TryGetValue(size, out var typeRef))
            return typeRef;

        var stubStructTypeName = $"<ConstantPoolItemType{size}>";

        var type = new TypeDefinition(
            "",
            stubStructTypeName,
            TypeAttributes.Sealed,
            Module.ImportReference(typeof(ValueType)))
        {
            PackingSize = 1,
            ClassSize = size
        };
        Module.Types.Add(type);
        _stubTypesPerSize.Add(size, type);

        return type;
    }

    private FieldReference GenerateFieldForStringConstant(TypeReference stubStructType, byte[] contentWithTerminatingZero)
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
