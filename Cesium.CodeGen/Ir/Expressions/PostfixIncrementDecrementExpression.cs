// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Yoakke.SynKit.C.Syntax;
using Yoakke.SynKit.Lexer;
using BinaryOperatorExpression = Cesium.CodeGen.Ir.Expressions.BinaryOperators.BinaryOperatorExpression;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class PostfixIncrementDecrementExpression : IExpression
{
    private readonly IExpression _target;
    private readonly BinaryOperator _operator;
    private readonly IToken<CTokenType> _postfixOperator;
    public PostfixIncrementDecrementExpression(Ast.PostfixIncrementDecrementExpression expression, IDeclarationScope scope)
    {
        expression.Deconstruct(out var target, out var postfixOperator);
        _target = target.ToIntermediate(scope);
        _operator = GetOperator(postfixOperator);
        _postfixOperator = postfixOperator;
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        var target = _target.Lower(scope);

        if (target is not IValueExpression valueTarget)
        {
            throw new CompilationException($"'{_postfixOperator.Text}' needs l-value");
        }

        var value = valueTarget.Resolve(scope);
        var duplicateValueExpression = new DuplicateValueExpression(value);

        var newValueExpression = new BinaryOperatorExpression(
            duplicateValueExpression,
            _operator,
            new ConstantLiteralExpression(new IntegerConstant("1"))
        );

        return new ValuePreservationExpression(
            value,
            new AssignmentExpression(
            valueTarget,
            AssignmentOperator.Assign,
            newValueExpression,
            doReturn: false
        ).Lower(scope));
    }

    public void EmitTo(IEmitScope scope) => throw new AssertException("Should be lowered");

    public IType GetExpressionType(IDeclarationScope scope) => _target.GetExpressionType(scope);

    private static BinaryOperator GetOperator(IToken<CTokenType> token) => token.Kind switch
    {
        CTokenType.Increment => BinaryOperator.Add,
        CTokenType.Decrement => BinaryOperator.Subtract,
        _ => throw new AssertException($"Token type {token.Kind} is invalid"),
    };

    /// <summary>
    /// Emits the <c>dup</c> opcode for the passed value. Thus, leaves two values on the stack. Use with caution.
    /// </summary>
    private class DuplicateValueExpression : IExpression
    {
        private readonly IValue _value;

        internal DuplicateValueExpression(IValue value)
        {
            _value = value;
        }

        public IExpression Lower(IDeclarationScope scope) => this;

        public void EmitTo(IEmitScope scope)
        {
            _value.EmitGetValue(scope);
            scope.Dup();
        }

        public IType GetExpressionType(IDeclarationScope scope) => _value.GetValueType();
    }

    /// <summary>
    /// Provides access to already loaded value (already stored on stack before this expression).
    /// </summary>
    private class ValuePreservationExpression(IValue value, IExpression expression) : IExpression
    {
        public IExpression Lower(IDeclarationScope scope) =>
            new ValuePreservationExpression(value, expression.Lower(scope));

        public void EmitTo(IEmitScope scope)
        {
            if (expression is not SetValueExpression sv)
                throw new AssertException($"{expression} should be a {nameof(SetValueExpression)}.");

            sv
                .NoReturn() // thus exposes the previously set value
                .EmitTo(scope);
        }

        public IType GetExpressionType(IDeclarationScope scope) => value.GetValueType();
    }
}
