using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Yoakke.C.Syntax;

namespace Cesium.CodeGen.Ir.Expressions;

internal class ConstantExpression : IExpression
{
    private readonly IConstant _constant;

    public ConstantExpression(Ast.ConstantExpression expression)
    {
        var constant = expression.Constant;
        _constant = constant.Kind switch
        {
            CTokenType.IntLiteral => new IntegerConstant(constant.Text),
            CTokenType.CharLiteral => new CharConstant(constant.Text),
            _ => throw new NotSupportedException($"Constant of kind {constant.Kind} is not supported.")
        };
    }

    public IExpression Lower() => this;

    public void EmitTo(FunctionScope scope) => _constant.EmitTo(scope);

    public override string ToString() => $"{nameof(ConstantExpression)}: {_constant}";
}
