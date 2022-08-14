using Cesium.CodeGen.Contexts;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

internal interface IValue
{
    void EmitGetValue(IDeclarationScope scope);
    TypeReference GetValueType();
}

internal interface ILValue : IValue
{
    void EmitGetAddress(IDeclarationScope scope);
    void EmitSetValue(IDeclarationScope scope, IExpression value);
}
