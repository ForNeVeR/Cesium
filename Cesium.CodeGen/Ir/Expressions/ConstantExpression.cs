using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
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
            CTokenType.FloatLiteral => new FloatingPointConstant(constant.Text),
            _ => throw new WipException(228, $"Constant of kind {constant.Kind} is not supported.")
        };
    }
}
