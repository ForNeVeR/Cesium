using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Cesium.CodeGen.Ir.Expressions;

internal class IndirectFunctionCallExpression : IExpression
{
    private readonly IdentifierExpression _function;
    private readonly IReadOnlyList<IExpression> _arguments;
    private readonly IExpression _callee;
    private readonly FunctionType _calleeType;

    public IndirectFunctionCallExpression(IdentifierExpression function, IExpression callee, FunctionType calleeType, IReadOnlyList<IExpression> arguments)
    {
        _function = function;
        _arguments = arguments;
        _callee = callee;
        _calleeType = calleeType;
    }

    public IExpression Lower(IDeclarationScope scope) => this;

    public void EmitTo(IEmitScope scope)
    {
        if (_calleeType.Parameters?.IsVarArg == true)
            throw new CompilationException("Vararg is not supported in function pointers");

        if (_callee == null)
            throw new AssertException("Should be lowered");

        VariableDefinition? varArgBuffer = null;
        var explicitParametersCount = _calleeType.Parameters?.Parameters.Count ?? 0;
        var varArgParametersCount = _arguments.Count - explicitParametersCount;
        if (_calleeType.Parameters?.IsVarArg == true)
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

        if (_calleeType.Parameters?.IsVarArg == true)
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

        _callee.EmitTo(scope);

        var callSite = new CallSite(_calleeType.ReturnType.Resolve(scope.Context));

        if (_calleeType.Parameters != null)
        {
            foreach (var param in _calleeType.Parameters.Parameters)
            {
                callSite.Parameters.Add(new ParameterDefinition(param.Type.Resolve(scope.Context)));
            }
        }

        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Calli, callSite));
    }

    public IType GetExpressionType(IDeclarationScope scope)
    {
        return _calleeType.ReturnType;
    }
}
