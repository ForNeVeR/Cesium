using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Cesium.CodeGen.Ir.Expressions;

internal abstract class FunctionCallExpressionBase : IExpression
{
    public abstract IExpression Lower(IDeclarationScope scope);
    public abstract void EmitTo(IEmitScope scope);
    public abstract IType GetExpressionType(IDeclarationScope scope);

    protected void EmitArgumentList(IEmitScope scope, ParametersInfo? paramInfo, IReadOnlyList<IExpression> arguments, MethodReference? method = null)
    {
        var explicitParametersCount = paramInfo?.Parameters.Count ?? 0;
        var varArgParametersCount = arguments.Count - explicitParametersCount;

        VariableDefinition? varArgBuffer = null;

        if (paramInfo?.IsVarArg == true)
        {
            // TODO[#285]:
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

        var counter = 0;
        foreach (var argument in arguments.Take(explicitParametersCount))
        {
            argument.EmitTo(scope);
            if (paramInfo?.IsVarArg != true && method != null)
            {
                var passedArg = argument.GetExpressionType((IDeclarationScope)scope).Resolve(scope.Context);
                var actualArg = method.Parameters[counter].ParameterType;
                counter++;
                if (passedArg.FullName != actualArg.FullName)
                {
                    var conversion = actualArg.FindConversionFrom(passedArg, scope.Context);
                    if (conversion == null)
                        continue;
                    scope.AddInstruction(OpCodes.Call, conversion);
                }
            }
        }

        if (paramInfo?.IsVarArg == true)
        {
            for (var i = 0; i < varArgParametersCount; i++)
            {
                var argument = arguments[i + explicitParametersCount];
                scope.AddInstruction(OpCodes.Ldloc, varArgBuffer!);
                scope.AddInstruction(OpCodes.Ldc_I4, i * 8);
                scope.AddInstruction(OpCodes.Add);
                argument.EmitTo(scope);
                scope.AddInstruction(OpCodes.Stind_I);
            }

            scope.AddInstruction(OpCodes.Ldloc, varArgBuffer!);
        }
    }
}
