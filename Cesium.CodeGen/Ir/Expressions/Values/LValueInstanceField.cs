using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using System.Runtime.CompilerServices;

namespace Cesium.CodeGen.Ir.Expressions.Values;

internal sealed class LValueInstanceField : LValueField
{
    private readonly IExpression _expression;
    private readonly StructType _structType;
    private readonly string _name;
    private FieldReference? _field;

    public LValueInstanceField(IExpression expression, Types.PointerType structPointerType, string name)
    {
        _expression = expression;
        if (structPointerType.Base is ConstType constType)
        {
            _structType = (StructType)constType.Base;
        }
        else
        {
            _structType = (StructType)structPointerType.Base;
        }

        _name = name;
    }

    public override IType GetValueType()
    {
        var type = _structType.Members.FirstOrDefault(_ => _.Identifier == _name)?.Type;
        if (type != null) return type;

        // oh, maybe its from union?
        type = _structType.Members.Where(_ => _.Type.TypeKind == TypeKind.Union)
            .SelectMany(_ => ((UnionType)_.Type).Members)
            .SingleOrDefault(_ => _.Identifier == _name)?.Type;

        var unionFields = _structType.Members.Where(_ => _.Type.TypeKind == TypeKind.Union);
        if (unionFields.FirstOrDefault() != null)
            foreach (var field in unionFields)
                type = RecursiveSearch(field.Type, _name);

        if (type != null) return type;

        var structName = _structType.Identifier == null ? "Struct" : $"\"{_structType.Identifier}\"";
        throw new CompilationException(
            $"{structName} has no member named \"{_name}\".");

        static IType? RecursiveSearch(IType type, string fieldName)
        {
            if (type.TypeKind != TypeKind.Union) return null;
            var union = (UnionType)type;

            foreach (var field in union.Members)
            {
                if (field.Identifier == fieldName)
                    return field.Type;

                var result = RecursiveSearch(field.Type, fieldName);
                if (result != null) return result;
            }

            return null;
        }
    }

    protected override void EmitGetFieldOwner(IEmitScope scope)
    {
        _expression.EmitTo(scope);
    }

    protected override FieldReference GetField(IEmitScope scope)
    {
        if (_field != null)
        {
            return _field;
        }

        List<FieldReference>? path = null;

        var valueTypeReference = _structType.Resolve(scope.Context);
        var valueTypeDef = valueTypeReference.Resolve();

        var field = valueTypeDef.Fields.FirstOrDefault(f => f?.Name == _name);

        if (field == null)
        {
            path = new(1);

            foreach (var f in valueTypeDef.Fields)
                if (RecursiveBuildPath(_name, f, path))
                {
                    path.Add(new FieldReference(f.Name, f.FieldType, f.DeclaringType));
                    return new UnionType.UnionFieldReference(path[0], path);
                }

            throw new CompilationException(
                    $"\"{valueTypeDef.Name.Replace("<typedef>", string.Empty)}\" has no member named \"{_name}\"");
        }
                
        _field = new FieldReference(field.Name, field.FieldType, field.DeclaringType);
        return _field;

        static bool RecursiveBuildPath(string fieldName, FieldDefinition type, List<FieldReference> list)
        {
            var fieldType = type.FieldType.Resolve();
            if (fieldType == null) return false;

            foreach(var field in fieldType.Fields)
            {
                if (field.Name == fieldName)
                {
                    list.Add(new FieldReference(field.Name, field.FieldType, field.DeclaringType));
                    return true;
                }

                var resolved = field.FieldType.Resolve();
                if (resolved == null) return false;
                if (resolved.Name.StartsWith("_Union_") && RecursiveBuildPath(fieldName, field, list))
                {
                    list.Add(new FieldReference(field.Name, field.FieldType, field.DeclaringType));
                    return true;
                }
            }
            return false;
        }
    }
}
