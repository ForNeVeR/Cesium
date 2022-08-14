using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.Values;

namespace Cesium.CodeGen.Ir.Expressions;

internal interface IValueExpression
{
    IValue Resolve(IDeclarationScope scope);
}
