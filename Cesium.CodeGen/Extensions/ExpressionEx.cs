using Cesium.Ast;
using Cesium.CodeGen.Ir.Expressions;

namespace Cesium.CodeGen.Extensions;

internal static class ExpressionEx
{
    public static IExpression ToIntermediate(this Expression e) => new AstExpression(e);
}
