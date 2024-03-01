using System.Globalization;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Cesium.Parser;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class ConstantLiteralExpression : IExpression
{
    public static ConstantLiteralExpression OfInt32(int value) => new(new IntegerConstant(value));

    internal ConstantLiteralExpression(IConstant constant)
    {
        Constant = constant;
    }

    public ConstantLiteralExpression(Ast.ConstantLiteralExpression expression)
        : this(GetConstant(expression))
    {
    }

    internal IConstant Constant { get; }

    public IExpression Lower(IDeclarationScope scope) => this;

    public void EmitTo(IEmitScope scope) => Constant.EmitTo(scope);

    public IType GetExpressionType(IDeclarationScope scope) => Constant.GetConstantType();

    public override string ToString() => $"{nameof(ConstantLiteralExpression)}: {Constant}";

    private static IConstant GetConstant(Ast.ConstantLiteralExpression expression)
    {
        var constant = expression.Constant;
        return constant.Kind switch
        {
            CTokenType.IntLiteral => new IntegerConstant(constant.Text),
            CTokenType.CharLiteral => new CharConstant(constant.Text),
            CTokenType.StringLiteral => new StringConstant(constant.UnwrapStringLiteral()),
            CTokenType.FloatLiteral => ParseFloatingPoint(constant.Text),
            _ => throw new AssertException($"Not a literal: {constant.Kind}.")
        };
    }

    private static IConstant ParseFloatingPoint(string value)
    {
        if (value.EndsWith('f') || value.EndsWith('F'))
        {
            if (!float.TryParse(value.AsSpan().Slice(0, value.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out var floatValue))
            {
                throw new CompilationException($"Value {value} is too large to fit into float");
            }

            return new FloatingPointConstant(floatValue, true);
        }
        else
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out var doubleValue))
                throw new CompilationException($"Cannot parse a double literal: {value}.");

            return new FloatingPointConstant(doubleValue, false);
        }
    }
}
