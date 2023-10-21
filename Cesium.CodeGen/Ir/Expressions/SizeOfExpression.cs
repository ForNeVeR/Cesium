using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions;

internal class SizeOfExpression : IExpression
{
    private readonly IType _type;

    public SizeOfExpression(IType Type)
    {
        _type = Type;
    }

    public SizeOfExpression(SizeOfOperator sizeOfOperator)
    {
        _type = Declarations.LocalDeclarationInfo.Of(sizeOfOperator.TypeName.SpecifierQualifierList, (Declarator?)null).Type;
    }

    public IExpression Lower(IDeclarationScope scope) => _type switch
    {
        NamedType e => new SizeOfExpression(new IdentifierExpression(e.TypeName).Resolve(scope).GetValueType()),
        _ => this
    };

    public void EmitTo(IEmitScope scope)
    {
        var type = _type.Resolve(scope.Context);
        scope.SizeOf(type);
    }

    public IType GetExpressionType(IDeclarationScope scope) => scope.CTypeSystem.UnsignedInt;
}
