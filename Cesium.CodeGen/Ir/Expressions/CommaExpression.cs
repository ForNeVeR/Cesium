using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class CommaExpression : IExpression
{
    private readonly IExpression _left;
    private readonly IExpression _right;

    internal CommaExpression(IExpression left, IExpression right)
    {
        _left = left;
        _right = right;
    }

    public CommaExpression(Ast.CommaExpression expression, IDeclarationScope scope)
    {
        _left = expression.Left.ToIntermediate(scope);
        _right = expression.Right.ToIntermediate(scope);
    }

    public IExpression Lower(IDeclarationScope scope) => new CommaExpression(_left.Lower(scope), _right.Lower(scope));

    public void EmitTo(IEmitScope scope)
    {
        var bodyProcessor = scope.Method.Body.GetILProcessor();

        _left.EmitTo(scope);

        if (_left.GetExpressionType((IDeclarationScope)scope) is not PrimitiveType { Kind: PrimitiveTypeKind.Void })
        {
            bodyProcessor.Emit(OpCodes.Pop);
        }

        _right.EmitTo(scope);
    }

    public IType GetExpressionType(IDeclarationScope scope) => _right.GetExpressionType(scope);
}
