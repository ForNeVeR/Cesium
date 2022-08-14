using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

/// <summary>This is a value representing a function type directly, not a function pointer.</summary>
internal class FunctionValue : IAddressableValue
{
    private readonly MethodReference _methodReference;

    public FunctionValue(MethodReference methodReference)
    {
        _methodReference = methodReference;
    }

    public void EmitGetValue(IDeclarationScope scope)
    {
        throw new NotImplementedException("Cannot directly get a value of a function, yet.");
    }

    public void EmitGetAddress(IDeclarationScope scope)
    {
        scope.LdFtn(_methodReference);
    }

    public TypeReference GetValueType()
    {
        return _methodReference.DeclaringType;
    }
}
