using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.Expressions.LValues;

internal interface ILValue : IValue
{
    void EmitSetValue(IDeclarationScope scope, IExpression value);
}
