using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class ExpressionStatement : IBlockItem
{
    public List<IBlockItem>? NextNodes { get; set; }
    public IBlockItem? Parent { get; set; }

    private readonly IExpression? _expression;
    internal ExpressionStatement(IExpression? expression)
    {
        _expression = expression;
    }

    public ExpressionStatement(Ast.ExpressionStatement statement) : this(statement.Expression?.ToIntermediate())
    {
    }

    public void ResolveNextNodes(IBlockItem root, IBlockItem parent)
    {
        NextNodes = new List<IBlockItem> { parent.NextNode(this) };
    }

    public IEnumerable<IBlockItem> GetChildren(IBlockItem root)
    {
        yield break;
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        IExpression? loweredExpression = _expression?.Lower(scope);
        if (loweredExpression is not null
            && loweredExpression is not SetValueExpression
            && !loweredExpression.GetExpressionType(scope).IsEqualTo(scope.CTypeSystem.Void))
        {
            loweredExpression = new ConsumeExpression(loweredExpression);
        }

        return new ExpressionStatement(loweredExpression);
    }

    public void EmitTo(IEmitScope scope) => _expression?.EmitTo(scope);
}
