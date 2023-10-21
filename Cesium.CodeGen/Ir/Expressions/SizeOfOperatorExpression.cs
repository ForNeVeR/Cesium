using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions;

internal class SizeOfOperatorExpression : IExpression
{
    private readonly IType _type;

    public SizeOfOperatorExpression(IType Type)
    {
        _type = Type;
    }

    public SizeOfOperatorExpression(Ast.SizeOfOperatorExpression sizeOfOperator)
    {
        _type = Declarations.LocalDeclarationInfo.Of(sizeOfOperator.TypeName.SpecifierQualifierList, (Declarator?)null).Type;
    }

    public IExpression Lower(IDeclarationScope scope) => _type switch
    {
        NamedType e => new SizeOfOperatorExpression(new IdentifierExpression(e.TypeName).Resolve(scope).GetValueType()),
        _ => this
    };

    public void EmitTo(IEmitScope scope)
    {
        var type = _type.Resolve(scope.Context);
        scope.SizeOf(type);
    }

    public IType GetExpressionType(IDeclarationScope scope) => scope.CTypeSystem.UnsignedInt;
}
