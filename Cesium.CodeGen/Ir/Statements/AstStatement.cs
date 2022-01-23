using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Generators;

namespace Cesium.CodeGen.Ir.Statements;

internal class AstStatement : StatementBase // TODO: Remove this class
{
    private readonly Statement _ast;
    public AstStatement(Statement ast)
    {
        _ast = ast;
    }

    protected override StatementBase Lower() => new AstStatement(Lowering.LowerStatement(_ast));
    protected override void DoEmitTo(FunctionScope scope) => Generators.Statements.EmitStatement(scope, _ast);
}
