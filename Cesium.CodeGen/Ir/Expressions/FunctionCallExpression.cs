using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;
using System.Diagnostics.Metrics;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class FunctionCallExpression : FunctionCallExpressionBase
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

    public override IExpression Lower(IDeclarationScope scope)
    {
        if (_function.Identifier == "__builtin_offsetof_instance")
        {
            if (_arguments is not [TypeCastExpression { TargetType: PointerType { Base: { } baseType } }])
            {
                throw new CompilationException($"__builtin_offsetof_instance: invalid arguments");
            }

            var resolvedType = scope.ResolveType(baseType);

            if (resolvedType is not StructType resolvedStruct)
            {
                throw new CompilationException($"__builtin_offsetof_instance: type \"{resolvedType}\" is not a struct type.");
            }

            if (resolvedStruct.Members.Count == 0)
            {
                throw new CompilationException($"__builtin_offsetof_instance: struct type \"{resolvedStruct.Identifier}\" has no members - is it declared?");
            }

            return new InstanceForOffsetOfExpression(resolvedStruct);
        }

        var functionName = _function.Identifier;

        if (scope.GetVariable(functionName) is { } var)
        {
            if (var.Type is not PointerType { Base: FunctionType f })
            {
                throw new CompilationException("Attempted to call non-function pointer");
            }


            return new IndirectFunctionCallExpression(
                new GetValueExpression(new LValueLocalVariable(var.Type, var.Index)),
                f,
                ConvertArgs(scope, f.Parameters));
        }

        var callee = scope.GetFunctionInfo(functionName) ?? throw new CompilationException($"Function \"{functionName}\" was not found.");

        return new FunctionCallExpression(
            _function,
            callee,
            ConvertArgs(scope, callee.Parameters));
    }

    private List<IExpression> ConvertArgs(IDeclarationScope scope, ParametersInfo? parameters)
    {
        int firstVarArgArgument = parameters?.Parameters.Count ?? 0;
        return _arguments.Select((a, index) =>
        {
            IType targetType;
            var loweredArg = a.Lower(scope);
            if (index < firstVarArgArgument)
            {
                // Argument is not in vararg argument list. Just use the declared type.
                targetType = parameters!.Parameters[index].Type;
            }
            else
            {
                // Argument is in a vararg list. Use the actual argument type, except for cases when it is float
                // (convert to double then).
                targetType = loweredArg.GetExpressionType(scope);
                if (targetType.Equals(CTypeSystem.Float))
                {
                    targetType = CTypeSystem.Double;
                }
            }

            return CastTypeIfRequired(scope, loweredArg, targetType);
        }).ToList();
    }

    private static IExpression CastTypeIfRequired(IDeclarationScope scope, IExpression expression, IType targetType)
    {
        if (expression.GetExpressionType(scope).IsEqualTo(targetType))
        {
            return expression;
        }

        return new TypeCastExpression(targetType, expression);
    }

    public override void EmitTo(IEmitScope scope)
    {
        if (_callee == null)
            throw new AssertException("Should be lowered");

        var functionName = _function.Identifier;
        var callee = _callee ?? throw new CompilationException($"Function \"{functionName}\" was not lowered.");
        var methodReference = callee.MethodReference ?? throw new CompilationException($"Function \"{functionName}\" was not found.");

        EmitArgumentList(scope, _callee.Parameters, _arguments, methodReference);

        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, methodReference));
        if (!_callee.ReturnType.IsVoid())
        {
            var passedArg = _callee.ReturnType.Resolve(scope.Context);
            var actualArg = methodReference.ReturnType;
            if (passedArg.FullName != actualArg.FullName)
            {
                var conversion = actualArg.FindConversionTo(passedArg, scope.Context);
                if (conversion != null)
                {
                    scope.AddInstruction(OpCodes.Call, conversion);
                }
            }
        }
    }

    public override IType GetExpressionType(IDeclarationScope scope)
    {
        var functionName = _function.Identifier;
        var callee = _callee ?? throw new AssertException($"Function \"{functionName}\" was not lowered.");
        return callee.ReturnType;
    }
}
