using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

internal sealed class LValueIndirection : ILValue
{
    private readonly IExpression _pointerExpression;
    private readonly IPointerLikeType _pointerType;

    public LValueIndirection(IExpression expression, IPointerLikeType pointerType)
    {
        _pointerExpression = expression;
        _pointerType = pointerType;
    }

    public void EmitGetValue(IEmitScope scope)
    {
        _pointerExpression.EmitTo(scope);
        var (load, _) = GetOpcodes(_pointerType);
        scope.Method.Body.Instructions.Add(Instruction.Create(load));
    }

    public void EmitGetAddress(IEmitScope scope) => _pointerExpression.EmitTo(scope);

    public void EmitSetValue(IEmitScope scope, IExpression value)
    {
        _pointerExpression.EmitTo(scope);
        value.EmitTo(scope);
        var (_, store) = GetOpcodes(_pointerType);
        scope.Method.Body.Instructions.Add(Instruction.Create(store));
    }

    public IType GetValueType() => _pointerType.Base;

    private static (OpCode load, OpCode store) GetOpcodes(IPointerLikeType pointerType) => SimplifyBaseType(pointerType.Base) switch
    {
        PrimitiveType primitiveType => PrimitiveTypeInfo.Opcodes[primitiveType.Kind],
        IPointerLikeType => (OpCodes.Ldind_I, OpCodes.Stind_I),
        _ => throw new WipException(256, $"Unsupported type for indirection operator: {pointerType}")
    };

    private static IType SimplifyBaseType(IType type) => type switch
    {
        ConstType constType => constType.Base,
        _ => type
    };
}
