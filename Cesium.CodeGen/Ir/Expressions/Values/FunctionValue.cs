using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

/// <summary>This is a value representing a function type directly, not a function pointer.</summary>
internal sealed class FunctionValue : AddressableValue
{
    private readonly MethodReference _methodReference;
    private readonly Contexts.Meta.FunctionInfo _functionInfo;

    public FunctionValue(Contexts.Meta.FunctionInfo functionInfo, MethodReference methodReference)
    {
        _functionInfo = functionInfo;
        _methodReference = methodReference;
    }

    public override void EmitGetValue(IEmitScope scope)
    {
        throw new WipException(227, "Cannot directly get a value of a function, yet.");
    }

    protected override void EmitGetAddressUnchecked(IEmitScope scope)
    {
        scope.LdFtn(_methodReference);
    }

    public override IType GetValueType()
    {
        return new FunctionType(_functionInfo.Parameters, _functionInfo.ReturnType);
    }
}
