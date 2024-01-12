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

    public override IType GetValueType() =>
        _structType.Members.FirstOrDefault(_ => _.Identifier == _name)?.Type
        ?? throw new CompilationException($"Member named \"{_name}\" not found");

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

        var valueTypeReference = _structType.Resolve(scope.Context);
        var valueTypeDef = valueTypeReference.Resolve();

        var field = valueTypeDef.Fields.FirstOrDefault(f => f?.Name == _name)
                ?? throw new CompilationException(
                    $"\"{valueTypeDef.Name.Replace("<typedef>", string.Empty)}\" has no member named \"{_name}\"");
        _field = new FieldReference(field.Name, field.FieldType, field.DeclaringType);
        return _field;
    }
}
