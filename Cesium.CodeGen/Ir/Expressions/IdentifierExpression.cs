using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.LValues;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.CodeGen.Ir.Expressions;

internal class IdentifierExpression : IExpression, ILValueExpression
{
    public string Identifier { get; }

    public IdentifierExpression(Ast.ConstantExpression expression)
    {
        var constant = expression.Constant;
        if (expression.Constant.Kind != CTokenType.Identifier)
            throw new NotSupportedException($"Constant kind not supported: {expression.Constant.Kind}.");

        Identifier = constant.Text;
    }

    public IdentifierExpression(Ast.IdentifierExpression expression)
    {
        Identifier = expression.Identifier;
    }

    public IExpression Lower() => this;

    public ILValue Resolve(IDeclarationScope scope)
    {
        scope.Variables.TryGetValue(Identifier, out var var);
        var par = scope.GetParameter(Identifier);

        switch (var, par)
        {
            case (null, null):
                throw new NotSupportedException($"Cannot find variable {Identifier}.");
            case ({ }, null):
                return new LValueLocalVariable(var);
            case (null, { }):
                return new LValueParameter(par);
            case ({ }, { }):
                throw new NotSupportedException(
                    $"Variable {Identifier} is both available as a local and as a function parameter.");
        }
    }

    public void EmitTo(IDeclarationScope scope) => Resolve(scope).EmitGetValue(scope);
}
