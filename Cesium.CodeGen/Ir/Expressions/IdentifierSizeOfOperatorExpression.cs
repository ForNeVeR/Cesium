using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.Expressions;

internal class IdentifierSizeOfOperatorExpression : IExpression
{
    private readonly IdentifierExpression _identifier;

    public IdentifierSizeOfOperatorExpression(Ast.IdentifierSizeOfOperatorExpression astExpression)
    {
        var expression = astExpression.Identifier.ToIntermediate();

        if (expression is not IdentifierExpression identifierExpression)
            throw new CompilationException($"\"{astExpression.Identifier.Identifier}\" is not a valid identifier");
        _identifier = identifierExpression;
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        var typeResolved = _identifier.Resolve(scope).GetValueType();
        var sizeOfExpression = new SizeOfOperatorExpression(typeResolved);
        return sizeOfExpression.Lower(scope);
    }

    public void EmitTo(IEmitScope scope) => throw new AssertException("Should be lowered");

    public IType GetExpressionType(IDeclarationScope scope) => throw new AssertException("Should be lowered");
}
