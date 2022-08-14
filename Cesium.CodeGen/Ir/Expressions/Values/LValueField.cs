using Cesium.CodeGen.Contexts;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

internal class LValueField : ILValue
{
    private readonly IExpression _expression;
    private readonly FieldReference _field;

    public LValueField(IExpression expression, FieldReference field)
    {
        _expression = expression;
        _field = field;
    }

    public void EmitGetValue(IDeclarationScope scope)
    {
        _expression.EmitTo(scope);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, _field));
    }

    public void EmitGetAddress(IDeclarationScope scope)
    {
        _expression.EmitTo(scope);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldflda, _field));
    }

    public void EmitSetValue(IDeclarationScope scope, IExpression value)
    {
        _expression.EmitTo(scope);
        value.EmitTo(scope);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, _field));
    }

    public TypeReference GetValueType() => _field.FieldType;
}
