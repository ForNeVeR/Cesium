using Cesium.CodeGen.Ir.Statements;

namespace Cesium.CodeGen.Extensions;

internal static class StatementEx
{
    public static IStatement ToIntermediate(this Ast.Statement statement) => statement switch
    {
        Ast.CompoundStatement s => new CompoundStatement(s),
        _ => new AstStatement(statement)
    };
}
