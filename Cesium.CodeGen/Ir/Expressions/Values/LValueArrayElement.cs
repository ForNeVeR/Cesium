using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
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

    public void EmitGetValue(IDeclarationScope scope)
    {
        EmitPointerMoveToElement(scope);

        var (loadOp, _) = PrimitiveTypeInfo.Opcodes[GetElementType().Kind];
        scope.Method.Body.GetILProcessor().Emit(loadOp);
    }

    public void EmitGetAddress(IDeclarationScope scope)
    {
        EmitPointerMoveToElement(scope);
    }

    public void EmitSetValue(IDeclarationScope scope, IExpression value)
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

        return (PrimitiveType)type.Base;
    }

    private void EmitPointerMoveToElement(IDeclarationScope scope)
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
}
