using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class ExpressionStatement : IBlockItem
{
    private readonly IExpression? _expression;
    internal ExpressionStatement(IExpression? expression)
    {
        _expression = expression;
    }

    public ExpressionStatement(Ast.ExpressionStatement statement) : this(statement.Expression?.ToIntermediate())
    {
    }

    public IBlockItem Lower() => new ExpressionStatement(_expression?.Lower());
    public void EmitTo(IDeclarationScope scope) => _expression?.EmitTo(scope);
}
