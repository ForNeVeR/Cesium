using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Cesium.CodeGen.Ir.Expressions;

internal class FunctionCallExpression : IExpression
{
    private readonly IdentifierExpression _function;
    private readonly IList<IExpression> _arguments;
    private readonly FunctionInfo? _callee;

    private FunctionCallExpression(IdentifierExpression function, FunctionInfo callee, IList<IExpression> arguments)
    {
        _function = function;
        _arguments = arguments;
        _callee = callee;
    }

    public FunctionCallExpression(Ast.FunctionCallExpression expression)
    {
        var (function, arguments) = expression;
        var functionExpression = function.ToIntermediate();
        _function = functionExpression as IdentifierExpression
                    ?? throw new WipException(
                        229,
                        $"Non-constant expressions as function name aren't supported, yet: {functionExpression}.");
        _arguments = (IList<IExpression>?)arguments?.Select(e => e.ToIntermediate()).ToList()
                     ?? Array.Empty<IExpression>();
        _callee = null;
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        var functionName = _function.Identifier;
        var callee = scope.Functions.GetValueOrDefault(functionName)
                     ?? throw new CompilationException($"Function \"{functionName}\" was not found.");
        return new FunctionCallExpression(
            _function,
            callee,
            _arguments.Select(a => a.Lower(scope)).ToList());
    }

    public void EmitTo(IEmitScope scope)
    {
        var explicitParametersCount = _callee!.Parameters?.Parameters.Count ?? 0;
        foreach (var argument in _arguments.Take(explicitParametersCount))
            argument.EmitTo(scope);

        if (_callee!.Parameters?.IsVarArg == true)
        {
            // Using sparse population of the parameters on the stack. 8 bytes should be enough for anybody.
            var varArgParametersCount = _arguments.Count - explicitParametersCount;
            if (varArgParametersCount == 0)
            {
                scope.AddInstruction(OpCodes.Ldc_I4_0);
            }
            else
            {
                scope.AddInstruction(OpCodes.Ldc_I4, varArgParametersCount * 8);
                scope.AddInstruction(OpCodes.Localloc);
            }

            for (var i = 0; i < varArgParametersCount; i++)
            {
                var argument = _arguments[i + explicitParametersCount];
                scope.AddInstruction(OpCodes.Dup);
                scope.AddInstruction(OpCodes.Ldc_I4, i * 8);
                scope.AddInstruction(OpCodes.Add);
                argument.EmitTo(scope);
                scope.AddInstruction(OpCodes.Conv_I);
                scope.AddInstruction(OpCodes.Stind_I);
            }
        }

        var functionName = _function.Identifier;
        var callee = _callee ?? throw new CompilationException($"Function \"{functionName}\" was not lowered.");

        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, callee.MethodReference));
    }

    public IType GetExpressionType(IDeclarationScope scope)
    {
        var functionName = _function.Identifier;
        var callee = _callee ?? throw new AssertException($"Function \"{functionName}\" was not lowered.");
        return callee.ReturnType;
    }
}
