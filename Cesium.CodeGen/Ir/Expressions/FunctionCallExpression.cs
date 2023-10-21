using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Cesium.CodeGen.Ir.Expressions;

internal class FunctionCallExpression : FunctionCallExpressionBase
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
                new GetValueExpression(new LValueLocalVariable(var.Type, var.Identifier)),
                f,
                _arguments.Select((a, index) =>
                {
                    int firstVarArgArgument = 0;
                    if (f.Parameters?.IsVarArg == true)
                    {
                        firstVarArgArgument = f.Parameters.Parameters.Count;
                    }

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
                    var loweredArg = a.Lower(scope);
                    var expressionType = loweredArg.GetExpressionType(scope);
                    if (expressionType.Equals(scope.CTypeSystem.Float))
                    {
                        // Seems to be float always use float-point registers and as such we need to covert to double.
                        return new TypeCastExpression(scope.CTypeSystem.Double, loweredArg);
                    }
                    else
                    {
                        return loweredArg;
                    }
                }

                return a.Lower(scope);
            }).ToList());
    }

    public override void EmitTo(IEmitScope scope)
    {
        if (_callee == null)
            throw new AssertException("Should be lowered");

        EmitArgumentList(scope, _callee.Parameters, _arguments);

        var functionName = _function.Identifier;
        var callee = _callee ?? throw new CompilationException($"Function \"{functionName}\" was not lowered.");

        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, callee.MethodReference));
    }

    public override IType GetExpressionType(IDeclarationScope scope)
    {
        var functionName = _function.Identifier;
        var callee = _callee ?? throw new AssertException($"Function \"{functionName}\" was not lowered.");
        return callee.ReturnType;
    }
}
