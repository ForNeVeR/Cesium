using Cesium.CodeGen.Contexts;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Expressions.LValues;

internal interface ILValue
{
    void EmitGetValue(IDeclarationScope scope);
    void EmitGetAddress(IDeclarationScope scope);
    void EmitSetValue(IDeclarationScope scope, IExpression value);
    TypeReference GetValueType();
}
