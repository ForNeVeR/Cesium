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

    public IBlockItem Lower(IDeclarationScope scope)
    {
        var loweredExpression = _expression?.Lower(scope);

        if (loweredExpression is SetValueExpression setValue)
            return new ExpressionStatement(setValue.NoReturn());

        if (loweredExpression is not null
            && !loweredExpression.GetExpressionType(scope).IsEqualTo(scope.CTypeSystem.Void))
        {
            loweredExpression = new ConsumeExpression(loweredExpression);
        }

        return new ExpressionStatement(loweredExpression);
    }

    public void EmitTo(IEmitScope scope) => _expression?.EmitTo(scope);

    public bool TryUnsafeSubstitute(IBlockItem original, IBlockItem replacement) => false;
}
