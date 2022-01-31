using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.LValues;
using Yoakke.C.Syntax;

namespace Cesium.CodeGen.Ir.Expressions;

internal class IdentifierConstantExpression : IExpression, ILValueExpression
{
    private readonly string _identifier;

    public IdentifierConstantExpression(Ast.ConstantExpression expression)
    {
        var constant = expression.Constant;
        if (expression.Constant.Kind != CTokenType.Identifier)
            throw new NotSupportedException($"Constant kind not supported: {expression.Constant.Kind}.");

        _identifier = constant.Text;
    }

    public IExpression Lower() => this;

    public ILValue Resolve(FunctionScope scope)
    {
        scope.Variables.TryGetValue(_identifier, out var var);
        scope.Parameters.TryGetValue(_identifier, out var par);

        switch (var, par)
        {
            case (null, null):
                throw new NotSupportedException($"Cannot find variable {_identifier}.");
            case ({ }, null):
                return new LValueLocalVariable(var);
            case (null, { }):
                return new LValueParameter(par);
            case ({ }, { }):
                throw new NotSupportedException(
                    $"Variable {_identifier} is both available as a local and as a function parameter.");
        }
    }

    public void EmitTo(FunctionScope scope) => Resolve(scope).EmitGetValue(scope);
}
