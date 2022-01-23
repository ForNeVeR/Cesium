using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.Statements;

internal interface IStatement
{
    void EmitTo(FunctionScope scope);
}
