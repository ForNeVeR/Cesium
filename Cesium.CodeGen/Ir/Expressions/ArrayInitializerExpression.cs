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
        throw new NotImplementedException();
    }
}
