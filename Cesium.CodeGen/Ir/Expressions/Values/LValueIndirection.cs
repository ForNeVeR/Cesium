using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

internal class LValueIndirection : ILValue
{
    private readonly IExpression _pointerExpression;
    private readonly PointerType _pointerType;

    public LValueIndirection(IExpression expression, PointerType pointerType)
    {
        _pointerExpression = expression;
        _pointerType = pointerType;
    }

    public void EmitGetValue(IDeclarationScope scope)
    {
        _pointerExpression.EmitTo(scope);
        var (load, _) = GetOpcodes(_pointerType);
        scope.Method.Body.Instructions.Add(Instruction.Create(load));
    }

    public void EmitGetAddress(IDeclarationScope scope) => _pointerExpression.EmitTo(scope);

    public void EmitSetValue(IDeclarationScope scope, IExpression value)
    {
        _pointerExpression.EmitTo(scope);
        value.EmitTo(scope);
        var (_, store) = GetOpcodes(_pointerType);
        scope.Method.Body.Instructions.Add(Instruction.Create(store));
    }

    public IType GetValueType() => _pointerType;

    private static (OpCode load, OpCode store) GetOpcodes(PointerType pointerType) => pointerType.Base switch
    {
        PrimitiveType primitiveType => PrimitiveTypeInfo.Opcodes[primitiveType.Kind],
        _ => throw new WipException(256, $"Unsupported type for indirection operator: {pointerType}")
    };
}
