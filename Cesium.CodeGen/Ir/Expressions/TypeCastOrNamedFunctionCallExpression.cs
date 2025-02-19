// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class TypeCastOrNamedFunctionCallExpression : IExpression
{
    private readonly string _typeOrFunctionName;
    private readonly IReadOnlyList<IExpression> _arguments;

    public TypeCastOrNamedFunctionCallExpression(Ast.TypeCastOrNamedFunctionCallExpression expression, IDeclarationScope scope)
    {
        _typeOrFunctionName = expression.TypeOrFunctionName;
        _arguments = expression.Arguments.Select(e => e.ToIntermediate(scope)).ToList();
    }

    private static IExpression AggregateCommaExpression(IReadOnlyList<IExpression> arguments)
    {
        var expr = arguments[0];

        foreach (var argument in arguments.Skip(1))
        {
            expr = new CommaExpression(expr, argument);
        }

        return expr;
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        return scope.TryGetType(_typeOrFunctionName) is { } type
            ? new TypeCastExpression(type, AggregateCommaExpression(_arguments)).Lower(scope)
            : new FunctionCallExpression(new IdentifierExpression(_typeOrFunctionName), null, _arguments).Lower(scope);
    }

    public void EmitTo(IEmitScope scope) => throw new CompilationException("Should be lowered");

    public IType GetExpressionType(IDeclarationScope scope) => throw new CompilationException("Should be lowered");
}
