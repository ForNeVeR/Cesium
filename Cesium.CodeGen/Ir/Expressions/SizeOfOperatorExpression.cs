using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
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
        var (specifiers, abstractDeclarator) = sizeOfOperator.TypeName;
        _type = (specifiers, abstractDeclarator) switch
        {
            ({ }, { }) => LocalDeclarationInfo.Of(specifiers, abstractDeclarator).Type,
            ({ }, null) => LocalDeclarationInfo.Of(specifiers, (Declarator?)null).Type
        };
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        if (_type is NamedType namedType)
        {
            var typeResolved = new IdentifierExpression(namedType.TypeName).Resolve(scope).GetValueType();
            if (typeResolved is InPlaceArrayType typeArray)
            {
                return typeArray.GetSizeInBytesExpression(scope.ArchitectureSet);
            }
            return new SizeOfOperatorExpression(typeResolved);
        }
        else if (_type is InPlaceArrayType typeArray)
        {
            return typeArray.GetSizeInBytesExpression(scope.ArchitectureSet);
        }
        // TODO [#453]: If a struct is declared locally, it won't be resolved later, resulting in a failure.
        return this;
    } 

    public void EmitTo(IEmitScope scope)
    {
        var type = _type.Resolve(scope.Context);
        scope.SizeOf(type);
    }

    public IType GetExpressionType(IDeclarationScope scope) => scope.CTypeSystem.UnsignedInt;
}
