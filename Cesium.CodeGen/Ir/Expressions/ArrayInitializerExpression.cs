using System.Collections.Immutable;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class ArrayInitializerExpression : IExpression
{
    public ArrayInitializerExpression(ImmutableArray<IExpression?> initializers)
    {
        Initializers = initializers;
    }

    internal ImmutableArray<IExpression?> Initializers { get; }

    public void EmitTo(IEmitScope scope)
    {
        throw new NotSupportedException("Emit of array initializer cannot be expressed directly.");
    }

    public IType GetExpressionType(IDeclarationScope scope)
    {
        throw new NotImplementedException();
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        return InlineConstantExpressions(scope);
    }

    public ArrayInitializerExpression InlineConstantExpressions(IDeclarationScope scope)
    {
        List<IExpression?> expressions = new();
        foreach (var initializer in Initializers)
        {
            if (initializer is null)
            {
                expressions.Add(initializer);
                continue;
            }

            var (errorMessage, constant) = ConstantEvaluator.TryGetConstantValue(initializer);
            if (constant != null)
            {
                expressions.Add(new ConstantLiteralExpression(constant));
            }
            else
            {
                expressions.Add(initializer);
            }
        }

        return new ArrayInitializerExpression(expressions.ToImmutableArray());
    }
}
