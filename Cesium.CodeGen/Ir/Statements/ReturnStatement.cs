using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Statements;

internal class ReturnStatement : StatementBase
{
    private readonly IExpression _expression;

    public ReturnStatement(Ast.ReturnStatement statement)
    {
        _expression = statement.Expression.ToIntermediate();
    }

    protected override StatementBase Lower() => this;

    protected override void DoEmitTo(FunctionScope scope)
    {
        _expression.EmitTo(scope);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    }
}
