using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.Values;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal class FunctionPointerValue : IValue
{
    private readonly MethodReference _methodReference;

    public FunctionPointerValue(MethodReference methodReference)
    {
        _methodReference = methodReference;
    }

    public void EmitGetValue(IDeclarationScope scope)
    {
        throw new NotImplementedException();
    }

    public void EmitGetAddress(IDeclarationScope scope)
    {
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldftn, _methodReference));
    }

    public TypeReference GetValueType()
    {
        return _methodReference.DeclaringType;
    }
}
