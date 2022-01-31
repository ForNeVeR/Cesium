using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.LValues;

namespace Cesium.CodeGen.Ir.Expressions;

internal interface ILValueExpression
{
    ILValue Resolve(FunctionScope scope);
}
