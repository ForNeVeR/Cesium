using Cesium.CodeGen.Ir.Statements;

namespace Cesium.CodeGen.Extensions;

internal static class StatementEx
{
    public static CompoundStatement ToIntermediate(this Ast.CompoundStatement statement) => new(statement);

    public static IStatement ToIntermediate(this Ast.Statement statement) => statement switch
    {
        Ast.CompoundStatement s => s.ToIntermediate(),
        Ast.ReturnStatement s => new ReturnStatement(s),
        Ast.ExpressionStatement s => new ExpressionStatement(s),
        _ => throw new NotImplementedException($"Statement not supported, yet: {statement}.")
    };
}
