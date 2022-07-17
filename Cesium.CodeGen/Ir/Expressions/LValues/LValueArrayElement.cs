using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.LValues;

internal class LValueArrayElement : ILValue
{
    private readonly ILValue _array;
    private readonly IExpression _index;

    public LValueArrayElement(ILValue array, IExpression index)
    {
        _array = array;
        _index = index;
    }

    public void EmitGetValue(IDeclarationScope scope)
    {
        EmitPointerMoveToElement(scope);

        var (loadOp, _) = PrimitiveTypeInfo.Opcodes[GetElementType().Name];
        scope.Method.Body.GetILProcessor().Emit(loadOp);
    }

    public void EmitGetAddress(IDeclarationScope scope)
    {
        // TODO[#133]: Implement '&' operator for array elements
        throw new NotImplementedException("Unary operator '&' is not supported for array elements");
    }

    public void EmitSetValue(IDeclarationScope scope, IExpression value)
    {
        EmitPointerMoveToElement(scope);
        value.EmitTo(scope);
        var (_, storeOp) = PrimitiveTypeInfo.Opcodes[GetElementType().Name];
        scope.Method.Body.GetILProcessor().Emit(storeOp);
    }

    public TypeReference GetValueType()
        => _array.GetValueType();

    private TypeReference GetElementType()
        => GetValueType().GetElementType();

    private void EmitPointerMoveToElement(IDeclarationScope scope)
    {
        _array.EmitGetValue(scope);
        _index.EmitTo(scope);
        var method = scope.Method.Body.GetILProcessor();
        method.Emit(OpCodes.Conv_I);
        var elementSize = PrimitiveTypeInfo.Size[GetElementType().Name];
        method.Emit(OpCodes.Ldc_I4, elementSize);
        method.Emit(OpCodes.Mul);
        method.Emit(OpCodes.Add);
    }
}
