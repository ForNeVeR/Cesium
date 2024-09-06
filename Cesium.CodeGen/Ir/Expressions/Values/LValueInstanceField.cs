using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;

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

        var structName = _structType.Identifier == null ? "Struct" : $"\"{_structType.Identifier}\"";

        // oh, maybe its from anon type?
        var members = _structType.Members
            .Where(x => x.Identifier == null && x.Type is StructType) // get all struct & union fields in target struct
            .SelectMany(x => ((StructType)x.Type).Members) // get all fields from them
            .Where(x => x.Identifier == _name)
            .ToList();
        switch (members.Count)
        {
            case 1: return members.Single().Type;
            case 0: break;
            default: throw new CompilationException(
                $"{structName} has multiple suitable members named \"{_name}\".");
        }

        // go deeper

        var anonFields = _structType.Members.Where(_ => _.Identifier == null);
        if (anonFields.FirstOrDefault() != null)
            foreach (var field in anonFields)
            {
                type = RecursiveSearch(field.Type, _name);
                if (type != null)
                    break;
            }

        if (type != null) return type;
        throw new CompilationException(
            $"{structName} has no member named \"{_name}\".");

        static IType? RecursiveSearch(IType type, string fieldName)
        {
            if (type is not StructType structType) return null; // skip primitives and etc
            if (!structType.IsAnon) return null; // skip non-anon
            var members = structType.Members;

            foreach (var field in members)
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

        List<FieldDefinition>? path = null;

        var valueTypeReference = _structType.Resolve(scope.Context);
        var valueTypeDef = valueTypeReference.Resolve();

        var field = valueTypeDef.Fields.FirstOrDefault(f => f?.Name == _name);

        if (field == null)
        {
            path = new(1);

            foreach (var f in valueTypeDef.Fields)
                if (RecursiveBuildPath(_name, f, path))
                {
                    path.Add(f);
                    return new StructType.AnonStructFieldReference(path[0], path);
                }

            throw new CompilationException(
                    $"\"{valueTypeDef.Name.Replace("<typedef>", string.Empty)}\" has no member named \"{_name}\"");
        }

        _field = new FieldReference(field.Name, field.FieldType, field.DeclaringType);
        return _field;

        static bool RecursiveBuildPath(string fieldName, FieldDefinition type, List<FieldDefinition> list)
        {
            var fieldType = type.FieldType.Resolve();
            if (fieldType == null) return false;
            if (fieldType.IsPrimitive) return false; // skip primitives (int, long and etc)

            foreach(var field in fieldType.Fields)
            {
                if (field.Name == fieldName)
                {
                    list.Add(field);
                    return true;
                }

                var resolved = field.FieldType.Resolve();
                if (resolved == null) continue;
                if (resolved.IsPrimitive) continue; // They don't have fields, so skip them
                if ((resolved.Name.StartsWith("<typedef>_Union_") || resolved.Name.StartsWith("<typedef>_Anon_") || resolved.Name.StartsWith("_Union_") || resolved.Name.StartsWith("_Anon_"))
                    && RecursiveBuildPath(fieldName, field, list))
                {
                    list.Add(field);
                    return true;
                }
            }
            return false;
        }
    }
}
