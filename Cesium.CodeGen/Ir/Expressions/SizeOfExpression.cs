using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions;

internal record SizeOfExpression(IType Type) : IExpression
{
    public IExpression Lower(IDeclarationScope scope) => this;

    public void EmitTo(IEmitScope scope)
    {
        var type = Type.Resolve(scope.Context);
        scope.SizeOf(type);
    }

    public IType GetExpressionType(IDeclarationScope scope) => CTypeSystem.UnsignedInt;
}
