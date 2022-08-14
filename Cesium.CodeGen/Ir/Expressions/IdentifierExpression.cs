using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.Core.Exceptions;
using Mono.Cecil;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.CodeGen.Ir.Expressions;

internal class IdentifierExpression : IExpression, IValueExpression
{
    public string Identifier { get; }

    public IdentifierExpression(Ast.ConstantExpression expression)
    {
        var constant = expression.Constant;
        if (expression.Constant.Kind != CTokenType.Identifier)
            throw new CompilationException($"Constant kind not supported: {expression.Constant.Kind}.");

        Identifier = constant.Text;
    }

    public IdentifierExpression(Ast.IdentifierExpression expression)
    {
        Identifier = expression.Identifier;
    }

    public IExpression Lower() => this;

    public void EmitTo(IDeclarationScope scope) => Resolve(scope).EmitGetValue(scope);

    public TypeReference GetExpressionType(IDeclarationScope scope) => Resolve(scope).GetValueType();

    public IValue Resolve(IDeclarationScope scope)
    {
        scope.Variables.TryGetValue(Identifier, out var var);
        scope.Functions.TryGetValue(Identifier, out FunctionInfo? fun);
        var par = scope.GetParameter(Identifier);
        scope.Context.AssemblyContext.GlobalFields.TryGetValue(Identifier, out var globalType);

        if (var is not null && par is not null)
            throw new CompilationException($"Variable {Identifier} is both available as a local and as a function parameter.");

        if (var is not null && fun is not null)
            throw new CompilationException($"Variable {Identifier} is both available as a local and as a function name.");

        if (fun is not null && par is not null)
            throw new CompilationException($"Variable {Identifier} is both available as a function name and as a function parameter.");

        if (var is not null)
        {
            var variableDefinition = scope.ResolveVariable(Identifier);
            return new LValueLocalVariable(variableDefinition);
        }

        if (par is not null)
        {
            return new LValueParameter(par);
        }

        if (fun is not null)
        {
            return new FunctionValue(fun.MethodReference);
        }

        if (globalType != null)
        {
            var globalField = scope.Context.AssemblyContext.ResolveGlobalField(Identifier, scope.Context);
            return new LValueGlobalVariable(globalField);
        }

        throw new CompilationException($"Cannot find a local variable, a function parameter, a global variable or a function {Identifier}.");
    }
}
