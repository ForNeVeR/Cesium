using System.Globalization;
using System.Reflection.Metadata;
using System.Text;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Cesium.Parser;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.CodeGen.Ir.Expressions;

internal class StringLiteralListExpression : IExpression
{
    public static StringLiteralListExpression OfInt32(int value) => new(new IntegerConstant(value));

    internal StringLiteralListExpression(IConstant constant)
    {
        Constant = constant;
    }

    public StringLiteralListExpression(Ast.StringLiteralListExpression expression)
        : this(GetConstant(expression))
    {
    }

    internal IConstant Constant { get; }

    public IExpression Lower(IDeclarationScope scope) => this;

    public void EmitTo(IEmitScope scope) => Constant.EmitTo(scope);

    public IType GetExpressionType(IDeclarationScope scope) => Constant.GetConstantType(scope);

    public override string ToString() => $"{nameof(StringLiteralListExpression)}: {Constant}";

    private static IConstant GetConstant(Ast.StringLiteralListExpression expression)
    {
        StringBuilder builder = new();
        foreach (var constant in expression.ConstantList)
        {
            builder.Append(constant.Kind switch
            {
                CTokenType.StringLiteral => constant.UnwrapStringLiteral(),
                _ => throw new AssertException($"Not a string literal: {constant.Kind}.")
            });
        }

        return new StringConstant(builder.ToString());
    }
}
