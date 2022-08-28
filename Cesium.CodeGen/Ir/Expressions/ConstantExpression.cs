using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using System.Globalization;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.CodeGen.Ir.Expressions;

internal class ConstantExpression : IExpression
{
    private readonly IConstant _constant;
    internal ConstantExpression(IConstant constant)
    {
        _constant = constant;
    }

    public ConstantExpression(Ast.ConstantExpression expression)
        : this(GetConstant(expression))
    {
    }

    public IExpression Lower(IDeclarationScope scope) => this;

    public void EmitTo(IDeclarationScope scope) => _constant.EmitTo(scope);

    public IType GetExpressionType(IDeclarationScope scope) => _constant.GetConstantType(scope);

    public override string ToString() => $"{nameof(ConstantExpression)}: {_constant}";

    private static IConstant GetConstant(Ast.ConstantExpression expression)
    {
        var constant = expression.Constant;
        return constant.Kind switch
        {
            CTokenType.IntLiteral => new IntegerConstant(constant.Text),
            CTokenType.CharLiteral => new CharConstant(constant.Text),
            CTokenType.StringLiteral => new StringConstant(constant),
            CTokenType.FloatLiteral => ParseFloatingPoint(constant.Text),
            _ => throw new WipException(228, $"Constant of kind {constant.Kind} is not supported.")
        };
    }

    private static IConstant ParseFloatingPoint(string value)
    {
        if (value.EndsWith('f'))
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
