using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using System.Diagnostics;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.CodeGen.Ir.Expressions;

internal class IdentifierExpression : IExpression, IValueExpression
{
    public string Identifier { get; }

    public IdentifierExpression(Ast.ConstantLiteralExpression expression)
    {
        var constant = expression.Constant;
        if (expression.Constant.Kind != CTokenType.Identifier)
            throw new CompilationException($"Constant kind not supported: {expression.Constant.Kind}.");

        Identifier = constant.Text;
    }

    public IdentifierExpression(Ast.IdentifierExpression expression)
        : this(expression.Identifier)
    {
    }

    public IdentifierExpression(string identifier)
    {
        Identifier = identifier;
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        return new GetValueExpression(Resolve(scope));
    }

    public void EmitTo(IEmitScope scope) => throw new AssertException("Should be lowered");

    public IType GetExpressionType(IDeclarationScope scope) => Resolve(scope).GetValueType();

    public IValue Resolve(IDeclarationScope scope)
    {
        var var = scope.GetVariable(Identifier);
        var fun = scope.GetFunctionInfo(Identifier);
        var par = scope.GetParameterInfo(Identifier);
        scope.GlobalFields.TryGetValue(Identifier, out var globalType);

        if (var is not null && par is not null)
            throw new CompilationException($"Variable {Identifier} is both available as a local and as a function parameter.");

        if (var is not null && fun is not null)
            throw new CompilationException($"Variable {Identifier} is both available as a local and as a function name.");

        if (fun is not null && par is not null)
            throw new CompilationException($"Variable {Identifier} is both available as a function name and as a function parameter.");

        if (var is not null)
        {
            if (var.StorageClass == Declarations.StorageClass.Auto)
                return new LValueLocalVariable(var.Type, Identifier);

            if (var.StorageClass == Declarations.StorageClass.Static)
                return new LValueGlobalVariable(var.Type, Identifier);

            throw new CompilationException($"Identifier {Identifier} has unsupported storage class {var.StorageClass}.");
        }

        if (par is not null)
        {
            return new LValueParameter(par);
        }

        if (fun is not null)
        {
            Debug.Assert(fun.MethodReference is not null);
            return new FunctionValue(fun, fun.MethodReference);
        }

        if (globalType != null)
        {
            return new LValueGlobalVariable(globalType, Identifier);
        }

        throw new CompilationException($"Cannot find a local variable, a function parameter, a global variable or a function {Identifier}.");
    }
}
