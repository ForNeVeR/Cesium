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
            scope.AddInstruction(OpCodes.Ldc_I4, _arguments.Count - explicitParametersCount);
            scope.AddInstruction(OpCodes.Newarr, scope.Context.TypeSystem.Object);
            for (var i = 0; i < _arguments.Count - explicitParametersCount; i++)
            {
                var argument = _arguments[i + explicitParametersCount];
                scope.AddInstruction(OpCodes.Dup);
                scope.AddInstruction(OpCodes.Ldc_I4, i);
                argument.EmitTo(scope);
                var intPtrDefinition = scope.Context.TypeSystem.IntPtr.Resolve();
                var explicitConversionToPointer = intPtrDefinition.GetMethods()
                    .First(d => d.Name == "op_Explicit" && d.Parameters[0].ParameterType.IsPointer);
                var importedReference = scope.AssemblyContext.Module.ImportReference(explicitConversionToPointer);
                scope.AddInstruction(OpCodes.Call, importedReference);
                scope.AddInstruction(OpCodes.Box, scope.Context.TypeSystem.IntPtr);
                scope.AddInstruction(OpCodes.Stelem_Ref);
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
