using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

internal interface IValue
{
    void EmitGetValue(IDeclarationScope scope);
    IType GetValueType();
}

/// <remarks>
/// This is different from <see cref="ILValue"/> because functions are not lvalues but still can be addressed.
/// </remarks>
internal interface IAddressableValue : IValue
{
    void EmitGetAddress(IDeclarationScope scope);
}

internal interface ILValue : IAddressableValue
{
    void EmitSetValue(IDeclarationScope scope, IExpression value);
}
