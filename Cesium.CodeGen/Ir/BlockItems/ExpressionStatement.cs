using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class ExpressionStatement : IBlockItem
{
    public IExpression? Expression { get; }

    internal ExpressionStatement(IExpression? expression)
    {
        Expression = expression;
    }

    public ExpressionStatement(Ast.ExpressionStatement statement) : this(statement.Expression?.ToIntermediate())
    {
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        var loweredExpression = Expression?.Lower(scope);

        if (loweredExpression is SetValueExpression setValue)
            return new ExpressionStatement(setValue.NoReturn());

        if (loweredExpression is not null
            && !loweredExpression.GetExpressionType(scope).IsEqualTo(scope.CTypeSystem.Void))
        {
            loweredExpression = new ConsumeExpression(loweredExpression);
        }

        return new ExpressionStatement(loweredExpression);
    }

    public void EmitTo(IEmitScope scope) => Expression?.EmitTo(scope);
}
