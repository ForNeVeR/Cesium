using Cesium.CodeGen.Contexts;

namespace Cesium.CodeGen.Ir.Statements;

internal abstract class StatementBase : IStatement
{
    protected abstract StatementBase Lower();
    protected abstract void DoEmitTo(FunctionScope scope);

    public void EmitTo(FunctionScope scope)
    {
        var realStatement = Lower();
        realStatement.DoEmitTo(scope);
    }
}
