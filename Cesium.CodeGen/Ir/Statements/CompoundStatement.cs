using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Generators;

namespace Cesium.CodeGen.Ir.Statements;

internal class CompoundStatement : StatementBase
{
    private readonly Ast.CompoundStatement _ast;

    public CompoundStatement(Ast.CompoundStatement ast)
    {
        _ast = ast;
    }

    protected override StatementBase Lower() => new CompoundStatement(Lowering.LowerStatement(_ast));

    protected override void DoEmitTo(FunctionScope scope)
    {
        foreach (var blockItem in _ast.Block)
        {
            switch (blockItem)
            {
                case Declaration d:
                    Declarations.EmitLocalDeclaration(scope, d);
                    break;
                case Statement s:
                    s.ToIntermediate().EmitTo(scope);
                    break;
                default:
                    throw new NotImplementedException($"Block item not supported, yet: {blockItem}.");
            }
        }
    }
}
