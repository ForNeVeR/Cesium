using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Extensions;

internal class FunctionCallExpression : IExpression
{
    private readonly IdentifierConstantExpression _function;
    private readonly IList<IExpression> _arguments;

    private FunctionCallExpression(IdentifierConstantExpression function, IList<IExpression> arguments)
    {
        _function = function;
        _arguments = arguments;
    }

    public FunctionCallExpression(Ast.FunctionCallExpression expression)
    {
        var (function, arguments) = expression;
        var functionExpression = function.ToIntermediate();
        _function = functionExpression as IdentifierConstantExpression
                    ?? throw new NotImplementedException(
                        $"Non-constant expressions as function name aren't supported, yet: {functionExpression}.");
        _arguments = (IList<IExpression>?)arguments?.Select(e => e.ToIntermediate()).ToList()
                     ?? Array.Empty<IExpression>();
    }

    public IExpression Lower() => new FunctionCallExpression(
        (IdentifierConstantExpression)_function.Lower(),
        _arguments.Select(a => a.Lower()).ToList());

    public void EmitTo(FunctionScope scope)
    {
        foreach (var argument in _arguments)
            argument.EmitTo(scope);

        var functionName = _function.Identifier;
        var callee = scope.Functions[functionName];

        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, callee));
    }
}
