// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

internal sealed class LValueArrayElementAddress : ILValue
{
    internal IValue Array { get; }

    internal IExpression Index { get; }

    public LValueArrayElementAddress(IValue array, IExpression index)
    {
        Array = array;
        Index = index;
    }

    public void EmitGetValue(IEmitScope scope)
    {
        EmitPointerMoveToElement(scope);
    }

    public void EmitGetAddress(IEmitScope scope)
    {
        EmitPointerMoveToElement(scope);
    }

    public void EmitSetValue(IEmitScope scope, IExpression value)
    {
        throw new CompilationException($"The array adressingdoesn't support the store operation.");
    }

    public IType GetValueType()
    {
        var arrayType = Array.GetValueType();
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
        if (Array is LValueArrayElementAddress baseArray)
        {
            baseArray.EmitPointerMoveToElement(scope);
        }
        else
        {
            switch (Array.GetValueType())
            {
                case InPlaceArrayType:
                    ((IAddressableValue)Array).EmitGetAddress(scope);
                    break;
                case PointerType:
                    Array.EmitGetValue(scope);
                    break;
                case var other:
                    throw new CompilationException($"Cannot get element of type {other}.");
            }

        }

        Index.EmitTo(scope);
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
