using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

internal class LValueArrayElement : ILValue
{
    private readonly IValue _array;
    private readonly IExpression _index;

    public LValueArrayElement(IValue array, IExpression index)
    {
        _array = array;
        _index = index;
    }

    public void EmitGetValue(IEmitScope scope)
    {
        EmitPointerMoveToElement(scope);

        var (loadOp, _) = PrimitiveTypeInfo.Opcodes[GetElementType().Kind];
        scope.Method.Body.GetILProcessor().Emit(loadOp);
    }

    public void EmitGetAddress(IEmitScope scope)
    {
        EmitPointerMoveToElement(scope);
    }

    public void EmitSetValue(IEmitScope scope, IExpression value)
    {
        EmitPointerMoveToElement(scope);
        value.EmitTo(scope);
        var (_, storeOp) = PrimitiveTypeInfo.Opcodes[GetElementType().Kind];
        scope.Method.Body.GetILProcessor().Emit(storeOp);
    }

    public IType GetValueType()
        => _array.GetValueType();

    private PrimitiveType GetElementType()
    {
        InPlaceArrayType? type = GetValueType() as InPlaceArrayType;
        if (type == null)
        {
            throw new AssertException("Array type expected.");
        }

        var primitiveType = (PrimitiveType)GetBaseType(type);
        return primitiveType;
    }

    private void EmitPointerMoveToElement(IEmitScope scope)
    {
        _array.EmitGetValue(scope);
        _index.EmitTo(scope);
        var method = scope.Method.Body.GetILProcessor();
        method.Emit(OpCodes.Conv_I);
        var elementSize = PrimitiveTypeInfo.Size[GetElementType().Kind];
        method.Emit(OpCodes.Ldc_I4, elementSize);
        method.Emit(OpCodes.Mul);
        method.Emit(OpCodes.Add);
    }

    private static IType GetBaseType(InPlaceArrayType valueType)
    {
        if (valueType.Base is InPlaceArrayType inPlaceArrayType)
        {
            return GetBaseType(inPlaceArrayType);
        }

        return valueType.Base;
    }
}
