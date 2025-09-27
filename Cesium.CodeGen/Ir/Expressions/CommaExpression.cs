// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class CommaExpression : IExpression
{
    private readonly IExpression _left;
    private readonly IExpression _right;

    internal IExpression Left => _left;

    internal IExpression Right => _right;

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

    public IExpression Lower(IDeclarationScope scope) => new CommaExpression(Left.Lower(scope), Right.Lower(scope));

    public void EmitTo(IEmitScope scope)
    {
        var bodyProcessor = scope.Method.Body.GetILProcessor();

        Left.EmitTo(scope);

        if (Left.GetExpressionType((IDeclarationScope)scope) is not PrimitiveType { Kind: PrimitiveTypeKind.Void })
        {
            bodyProcessor.Emit(OpCodes.Pop);
        }

        Right.EmitTo(scope);
    }

    public IType GetExpressionType(IDeclarationScope scope) => Right.GetExpressionType(scope);
}
