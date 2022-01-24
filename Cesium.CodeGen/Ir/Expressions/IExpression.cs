using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.Expressions;

internal interface IExpression
{
    IExpression Lower();
    void EmitTo(FunctionScope scope);
}
