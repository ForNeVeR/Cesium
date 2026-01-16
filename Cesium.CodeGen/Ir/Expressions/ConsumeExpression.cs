// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class ConsumeExpression(IExpression expression) : IExpression
{
    public IExpression Expression { get; } = expression;

    public IExpression Lower(IDeclarationScope scope) => new ConsumeExpression(Expression.Lower(scope));

    public IType GetExpressionType(IDeclarationScope scope)
    {
        return CTypeSystem.Void;
    }

    public void EmitTo(IEmitScope scope)
    {
        if (Expression is SetValueExpression sv)
        {
            sv.NoReturn().EmitTo(scope);

            return;
        }

        Expression.EmitTo(scope);
        var processor = scope.Method.Body.GetILProcessor();
        processor.Emit(OpCodes.Pop);
    }
}
