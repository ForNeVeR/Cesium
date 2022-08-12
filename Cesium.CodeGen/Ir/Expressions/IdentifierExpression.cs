using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir.Expressions.LValues;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;
using Mono.Cecil.Cil;
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
        scope.Functions.TryGetValue(Identifier, out FunctionInfo? fun);
        var par = scope.GetParameter(Identifier);
        scope.Context.AssemblyContext.GlobalFields.TryGetValue(Identifier, out var globalType);

        if (var is not null && par is not null)
            throw new NotSupportedException($"Variable {Identifier} is both available as a local and as a function parameter.");

        if (var is not null && fun is not null)
            throw new NotSupportedException($"Variable {Identifier} is both available as a local and as a function name.");

        if (fun is not null && par is not null)
            throw new NotSupportedException($"Variable {Identifier} is both available as a function name and as a function parameter.");

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
            var functionType = new FunctionType(fun.Parameters, fun.ReturnType);
            var variableDefinition = new VariableDefinition(functionType.ResolvePointer(scope.Context));
            return new LValueLocalVariable(variableDefinition);

        }

        if (globalType != null)
        {
            var flobalField = scope.Context.AssemblyContext.ResolveGlobalField(Identifier, scope.Context);
            return new LValueGlobalVariable(flobalField);
        }

        throw new NotSupportedException($"Cannot find a local variable, a function parameter, a global variable or a function {Identifier}.");
    }
}
