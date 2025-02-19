// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal class DiscardResultExpression : IExpression
{
    private IExpression _expression;

    public DiscardResultExpression(IExpression expression)
    {
        _expression = expression;
    }

    public IExpression Lower(IDeclarationScope scope) => new DiscardResultExpression(_expression.Lower(scope));

    public void EmitTo(IEmitScope scope)
    {
        if (_expression is SetValueExpression sv)
            sv.NoReturn().EmitTo(scope);
        else _expression.EmitTo(scope);

        var processor = scope.Method.Body.GetILProcessor();
        processor.Emit(OpCodes.Pop);
    }

    public IType GetExpressionType(IDeclarationScope scope) => CTypeSystem.Void;
}
