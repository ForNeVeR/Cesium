using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Generators;
using Yoakke.C.Syntax;

namespace Cesium.CodeGen.Ir.Expressions;

internal class AstExpression : IExpression
{
    private readonly Expression _expression;

    public AstExpression(Expression expression)
    {
        _expression = expression;
    }

    public IExpression Lower() => Lowering.LowerExpression(_expression).ToIntermediate();

    public void EmitTo(FunctionScope scope) => Generators.Expressions.EmitExpression(scope, _expression);

    public string ConstantIdentifier
    {
        get
        {
            var expression = (ConstantExpression)_expression;
            var nameToken = expression.Constant;
            if (nameToken.Kind != CTokenType.Identifier)
                throw new Exception($"Not an lvalue: {nameToken.Kind} {nameToken.Text}");

            return nameToken.Text;
        }
    }
}
