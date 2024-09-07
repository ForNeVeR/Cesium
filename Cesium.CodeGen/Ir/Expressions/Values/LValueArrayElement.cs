using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
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
        var (_, maybeStore) = GetElementOpcodes();
        if (maybeStore is not {} storeOp)
            throw new CompilationException($"Type {_array} doesn't support the array store operation.");

        scope.Method.Body.GetILProcessor().Emit(storeOp);
    }

    private (OpCode, OpCode?) GetElementOpcodes()
    {
        var elementType = GetValueType().EraseConstType();
        return elementType switch
        {
            PrimitiveType primitiveType => PrimitiveTypeInfo.Opcodes[primitiveType.Kind],
            PointerType => (OpCodes.Ldind_I, OpCodes.Stind_I),
            _ => throw new WipException(256, $"Unsupported type for array access: {elementType}.")
        };
    }

    public IType GetValueType()
    {
        var arrayType = _array.GetValueType();
        return arrayType switch
        {
            InPlaceArrayType inPlaceArrayType => inPlaceArrayType.Base,
            PointerType pointerType => pointerType.Base,
            _ => throw new CompilationException($"Cannot get element of array of type {arrayType}.")
        };
    }

    private void EmitPointerMoveToElement(IEmitScope scope)
    {
        // Nested array addressing mode:
        if (_array is LValueArrayElement baseArray)
        {
            baseArray.EmitPointerMoveToElement(scope);
        }
        else
        {
            switch (_array.GetValueType())
            {
                case InPlaceArrayType:
                    ((IAddressableValue)_array).EmitGetAddress(scope);
                    break;
                case PointerType:
                    _array.EmitGetValue(scope);
                    break;
                case var other:
                    throw new CompilationException($"Cannot get element of type {other}.");
            }

        }

        _index.EmitTo(scope);
        var method = scope.Method.Body.GetILProcessor();
        method.Emit(OpCodes.Conv_I);
        var valueType = GetValueType();
        var constSize = valueType.GetSizeInBytes(scope.AssemblyContext.ArchitectureSet);
        if (constSize.HasValue)
        {
            method.Emit(OpCodes.Ldc_I4, constSize.Value);
        }
        else
        {
            valueType.GetSizeInBytesExpression(scope.AssemblyContext.ArchitectureSet).EmitTo(scope);
        }

        method.Emit(OpCodes.Mul);
        method.Emit(OpCodes.Add);
    }
}
