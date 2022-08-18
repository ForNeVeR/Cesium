using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Microsoft.VisualBasic.FileIO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

internal class LValueField : ILValue
{
    private readonly IExpression _expression;
    private readonly FieldReference _field;
    private readonly IType _fieldType;

    public LValueField(IExpression expression, IType fieldType, FieldReference field)
    {
        _expression = expression;
        _fieldType = fieldType;
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

    public IType GetValueType() => _fieldType;
}
