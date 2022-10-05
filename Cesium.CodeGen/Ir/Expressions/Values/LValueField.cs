using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

internal class LValueField : ILValue
{
    private readonly IExpression _expression;
    private readonly IType _fieldType;
    private readonly string _name;
    private FieldReference? _field;

    public LValueField(IExpression expression, IType fieldType, string name)
    {
        _expression = expression;
        _fieldType = fieldType;
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

    public IType GetValueType() => _fieldType;

    private FieldReference GetField(IEmitScope scope)
    {
        if (_field != null)
        {
            return _field;
        }

        var valueType = _fieldType;
        var valueTypeReference = valueType.Resolve(scope.Context);
        var valueTypeDef = valueTypeReference.Resolve();

        var field = valueTypeDef.Fields.FirstOrDefault(f => f?.Name == _name)
                ?? throw new CompilationException(
                    $"\"{valueTypeDef.Name}\" has no member named \"{_name}\"");
        _field = new FieldReference(field.Name, field.FieldType, field.DeclaringType);
        return _field;
    }
}
