using Cesium.CodeGen.Contexts;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.LValues;

internal class LValueField : ILValue
{
    private readonly ILValue _lvalue;
    private readonly FieldReference _field;

    public LValueField(ILValue lvalue, FieldReference field)
    {
        _lvalue = lvalue;
        _field = field;
    }

    public void EmitGetValue(IDeclarationScope scope)
    {
        _lvalue.EmitGetValue(scope);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, _field));
    }

    public void EmitGetAddress(IDeclarationScope scope)
    {
        _lvalue.EmitGetValue(scope);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldflda, _field));
    }

    public void EmitSetValue(IDeclarationScope scope, IExpression value)
    {
        _lvalue.EmitGetValue(scope);
        value.EmitTo(scope);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, _field));
    }

    public TypeReference GetValueType() => _field.FieldType;
}
