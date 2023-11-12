using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

internal sealed class LValueArrayElement : ILValue
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
        var valueType = GetValueType();
        var primitiveType = (PrimitiveType)GetBaseType(valueType);
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

    private static IType GetBaseType(IType valueType)
    {
        var baseType = valueType switch
        {
            InPlaceArrayType arrayType => arrayType.Base,
            PointerType pointerType => pointerType.Base,
            _ => throw new AssertException("Array or pointer type expected.")
        };
        if (baseType is InPlaceArrayType or PointerType)
        {
            return GetBaseType(baseType);
        }

        return baseType;
    }

    private static IType GetBaseType(PointerType valueType)
    {
        if (valueType.Base is InPlaceArrayType inPlaceArrayType)
        {
            return GetBaseType(inPlaceArrayType);
        }

        if (valueType.Base is PointerType pointerType)
        {
            return GetBaseType(pointerType);
        }

        return valueType.Base;
    }
}
