using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.Expressions;

internal class SizeOfOperatorExpression : IExpression
{
    private readonly IType _type;

    public SizeOfOperatorExpression(IType Type)
    {
        _type = Type;
    }

    public IExpression Lower(IDeclarationScope scope) => _type switch
    {
        InPlaceArrayType arrayType => arrayType.GetSizeInBytesExpression(scope.ArchitectureSet),
        StructType structType => throw new WipException(453, $"sizeof(StructType) not supported, yet: {structType.Identifier}."),
        _ => this
    };

    public void EmitTo(IEmitScope scope)
    {
        var type = _type.Resolve(scope.Context);
        scope.SizeOf(type);
    }

    public IType GetExpressionType(IDeclarationScope scope) => scope.CTypeSystem.UnsignedInt;
}
