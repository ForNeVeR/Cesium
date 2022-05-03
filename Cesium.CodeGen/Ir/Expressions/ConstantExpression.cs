using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.Constants;
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

    public IExpression Lower() => this;

    public void EmitTo(IDeclarationScope scope) => _constant.EmitTo(scope);

    public override string ToString() => $"{nameof(ConstantExpression)}: {_constant}";

    private static IConstant GetConstant(Ast.ConstantExpression expression)
    {
        var constant = expression.Constant;
        return constant.Kind switch
        {
            CTokenType.IntLiteral => new IntegerConstant(constant.Text),
            CTokenType.CharLiteral => new CharConstant(constant.Text),
            CTokenType.StringLiteral => new StringConstant(constant),
            _ => throw new NotSupportedException($"Constant of kind {constant.Kind} is not supported.")
        };
    }
}
