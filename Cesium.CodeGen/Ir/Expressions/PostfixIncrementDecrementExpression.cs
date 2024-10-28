using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.CodeGen.Ir.Expressions.Constants;
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
    private readonly IToken<CTokenType> _prefixOperator;
    public PostfixIncrementDecrementExpression(Ast.PostfixIncrementDecrementExpression expression)
    {
        expression.Deconstruct(out var prefixOperator, out var target);
        _target = target.ToIntermediate();
        _operator = GetOperator(prefixOperator);
        _prefixOperator = prefixOperator;
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        var target = _target.Lower(scope);

        if (target is not IValueExpression valueTarget)
        {
            throw new CompilationException($"'{_prefixOperator.Text}' needs l-value");
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
}
