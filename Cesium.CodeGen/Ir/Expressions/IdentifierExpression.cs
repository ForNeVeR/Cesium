using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.LValues;
using Mono.Cecil;
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

    public void EmitTo(IDeclarationScope scope) => Resolve(scope).EmitGetValue(scope);

    public TypeReference GetExpressionType(IDeclarationScope scope) => Resolve(scope).GetValueType();

    public ILValue Resolve(IDeclarationScope scope)
    {
        scope.Variables.TryGetValue(Identifier, out var var);
        var par = scope.GetParameter(Identifier);
        scope.Context.AssemblyContext.GlobalFields.TryGetValue(Identifier, out var globalType);
        switch (var, par)
        {
            case (null, null):
                if (globalType != null)
                {
                    var flobalField = scope.Context.AssemblyContext.ResolveGlobalField(Identifier, scope.Context);
                    return new LValueGlobalVariable(flobalField);
                }
                throw new NotSupportedException($"Cannot find variable {Identifier}.");
            case ({ }, null):
                {
                    var variableDefinition = scope.ResolveVariable(Identifier);
                    return new LValueLocalVariable(variableDefinition);
                }
            case (null, { }):
                return new LValueParameter(par);
            case ({ }, { }):
                throw new NotSupportedException(
                    $"Variable {Identifier} is both available as a local and as a function parameter.");
        }
    }
}
