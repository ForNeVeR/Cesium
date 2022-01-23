using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.Expressions;

public interface IExpression
{
    void EmitTo(FunctionScope scope);
}
