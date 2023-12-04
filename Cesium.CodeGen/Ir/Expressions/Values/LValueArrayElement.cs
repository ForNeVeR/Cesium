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

        var (loadOp, _) = GetElementOpcodes();
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
        var (_, storeOp) = GetElementOpcodes();
        scope.Method.Body.GetILProcessor().Emit(storeOp);
    }

    private (OpCode, OpCode) GetElementOpcodes()
    {
        var elementType = GetElementType();
        if (elementType is PrimitiveType primitive)
        {
            return PrimitiveTypeInfo.Opcodes[primitive.Kind];
        }

        if (elementType is PointerType)
        {
            return (OpCodes.Ldind_I, OpCodes.Stind_I);
        }

        throw new InvalidOperationException("Arrays of structs are not supported");
    }

    public IType GetValueType()
        => _array.GetValueType();

    private IType GetElementType()
    {
        var valueType = GetValueType();
        var primitiveType = GetBaseType(valueType);
        if (primitiveType is InPlaceArrayType or PointerType)
        {
            return GetBaseType(primitiveType);
        }

        return primitiveType;
    }

    private void EmitPointerMoveToElement(IEmitScope scope)
    {
        _array.EmitGetValue(scope);
        _index.EmitTo(scope);
        var method = scope.Method.Body.GetILProcessor();
        method.Emit(OpCodes.Conv_I);
        var elementSize = GetElementType().GetSizeInBytes(scope.AssemblyContext.ArchitectureSet);
        if (elementSize.HasValue)
        {
            method.Emit(OpCodes.Ldc_I4, elementSize.Value);
        }
        else
        {
            method.Emit(OpCodes.Sizeof, scope.Module.TypeSystem.IntPtr);
        }

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

        return baseType;
    }
}
