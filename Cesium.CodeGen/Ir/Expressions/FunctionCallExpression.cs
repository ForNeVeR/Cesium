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
    private readonly IReadOnlyList<IExpression> _arguments;
    private readonly FunctionInfo? _callee;

    public FunctionCallExpression(IdentifierExpression function, FunctionInfo? callee, IReadOnlyList<IExpression> arguments)
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
        _arguments = (IReadOnlyList<IExpression>?)arguments?.Select(e => e.ToIntermediate()).ToList()
                     ?? Array.Empty<IExpression>();
        _callee = null;
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        var functionName = _function.Identifier;
        var callee = scope.GetFunctionInfo(functionName);
        if (callee is null)
        {
            throw new CompilationException($"Function \"{functionName}\" was not found.");
        }

        int firstVarArgArgument = 0;
        if (callee.Parameters?.IsVarArg == true)
        {
            firstVarArgArgument = callee.Parameters.Parameters.Count;
        }

        return new FunctionCallExpression(
            _function,
            callee,
            _arguments.Select((a, index) =>
            {
                if (index >= firstVarArgArgument)
                {
                    var expressionType = a.GetExpressionType(scope);
                    if (expressionType.Equals(scope.CTypeSystem.Float))
                    {
                        // Seems to be float always use float-point registers and as such we need to covert to double.
                        return new TypeCastExpression(scope.CTypeSystem.Double, a.Lower(scope));
                    }
                    else
                    {
                        return a.Lower(scope);
                    }
                }

                return a.Lower(scope);
            }).ToList());
    }

    public void EmitTo(IEmitScope scope)
    {
        if (_callee == null)
            throw new AssertException("Should be lowered");

        VariableDefinition? varArgBuffer = null;
        var explicitParametersCount = _callee!.Parameters?.Parameters.Count ?? 0;
        var varArgParametersCount = _arguments.Count - explicitParametersCount;
        if (_callee!.Parameters?.IsVarArg == true)
        {
            // TODO: See https://github.com/ForNeVeR/Cesium/issues/285
            // Using sparse population of the parameters on the stack. 8 bytes should be enough for anybody.
            // Also we need perform localloc on empty stack, so we will use local variable to save vararg buffer to temporary variable.
            if (varArgParametersCount == 0)
            {
                scope.AddInstruction(OpCodes.Ldnull);
            }
            else
            {
                scope.AddInstruction(OpCodes.Ldc_I4, varArgParametersCount * 8);
                scope.AddInstruction(OpCodes.Localloc);
            }

            varArgBuffer = new VariableDefinition(scope.Context.TypeSystem.Void.MakePointerType());
            scope.Method.Body.Variables.Add(varArgBuffer);
            scope.AddInstruction(OpCodes.Stloc, varArgBuffer);
        }

        foreach (var argument in _arguments.Take(explicitParametersCount))
            argument.EmitTo(scope);

        if (_callee!.Parameters?.IsVarArg == true)
        {
            for (var i = 0; i < varArgParametersCount; i++)
            {
                var argument = _arguments[i + explicitParametersCount];
                scope.AddInstruction(OpCodes.Ldloc, varArgBuffer!);
                scope.AddInstruction(OpCodes.Ldc_I4, i * 8);
                scope.AddInstruction(OpCodes.Add);
                argument.EmitTo(scope);
                scope.AddInstruction(OpCodes.Stind_I);
            }

            scope.AddInstruction(OpCodes.Ldloc, varArgBuffer!);
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
