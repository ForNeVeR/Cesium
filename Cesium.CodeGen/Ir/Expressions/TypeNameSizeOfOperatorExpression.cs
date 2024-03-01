using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class TypeNameSizeOfOperatorExpression : IExpression
{
    private readonly IType _type;

    public TypeNameSizeOfOperatorExpression(Ast.TypeNameSizeOfOperatorExpression expression)
    {
        var (specifiers, abstractDeclarator) = expression.TypeName;
        _type = (specifiers, abstractDeclarator) switch
        {
            ({ }, { }) => LocalDeclarationInfo.Of(specifiers, abstractDeclarator).Type,
            ({ }, null) => LocalDeclarationInfo.Of(specifiers, (Declarator?)null).Type
        };
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        var type = scope.ResolveType(_type);
        if (type is StructType st && st.Members.Count == 0) // check for local types
            type = scope.GetVariable(st.Identifier ?? string.Empty)?.Type ?? _type; // idk how, but its works +_+
        var sizeOfExpression = new SizeOfOperatorExpression(type);
        return sizeOfExpression.Lower(scope);
    }

    public void EmitTo(IEmitScope scope) => throw new AssertException("Should be lowered");

    public IType GetExpressionType(IDeclarationScope scope) => throw new AssertException("Should be lowered");
}
