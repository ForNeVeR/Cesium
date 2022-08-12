using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.Expressions;

internal interface ILValueExpression
{
    IValue Resolve(IDeclarationScope scope);
}
