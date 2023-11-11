using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

internal sealed class LValueField : ILValue
{
    private readonly IExpression _expression;
    private readonly StructType _structType;
    private readonly string _name;
    private FieldReference? _field;

    public LValueField(IExpression expression, Types.PointerType structPointerType, string name)
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

    public void EmitGetValue(IEmitScope scope)
    {
        var field = GetField(scope);
        _expression.EmitTo(scope);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, field));
    }

    public void EmitGetAddress(IEmitScope scope)
    {
        var field = GetField(scope);
        _expression.EmitTo(scope);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldflda, field));
    }

    public void EmitSetValue(IEmitScope scope, IExpression value)
    {
        var field = GetField(scope);
        _expression.EmitTo(scope);
        value.EmitTo(scope);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, field));
    }

    public IType GetValueType() => _structType.Members.FirstOrDefault(_ => _.Identifier == _name)?.Type
        ?? throw new CompilationException($"Member named \"{_name}\" not found");

    private FieldReference GetField(IEmitScope scope)
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
