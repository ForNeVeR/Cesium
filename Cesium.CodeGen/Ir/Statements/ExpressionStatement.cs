using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.Statements;

internal class ExpressionStatement : StatementBase
{
    private readonly IExpression? _expression;
    private ExpressionStatement(IExpression? expression)
    {
        _expression = expression;
    }

    public ExpressionStatement(Ast.ExpressionStatement statement) : this(statement.Expression?.ToIntermediate())
    {
    }

    protected override StatementBase Lower() => new ExpressionStatement(_expression?.Lower());
    protected override void DoEmitTo(FunctionScope scope) => _expression?.EmitTo(scope);
}
