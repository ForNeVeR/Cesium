using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class SizeOfOperatorExpression : IExpression
{
    private readonly IType _type;

    public SizeOfOperatorExpression(IType Type)
    {
        _type = Type;
    }

    public IExpression Lower(IDeclarationScope scope) => _type switch
    {
        InPlaceArrayType arrayType => arrayType.GetSizeInBytesExpression(scope.ArchitectureSet),
        StructType structType => this,
        _ => this
    };

    public void EmitTo(IEmitScope scope)
    {
        var context = scope.Context;

        // As sizeof is a type operator, it may need to emit anonymous types right here.
        if (_type is IGeneratedType generatedType && !generatedType.IsAlreadyEmitted(context))
        {
            generatedType.EmitType(context);
        }

        var type = _type.Resolve(context);
        scope.SizeOf(type);
    }

    public IType GetExpressionType(IDeclarationScope scope) => CTypeSystem.UnsignedInt;
}
