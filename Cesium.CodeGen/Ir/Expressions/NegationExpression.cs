using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal class NegationExpression : IExpression
{
    private readonly IExpression _target;
    private NegationExpression(IExpression target)
    {
        _target = target;
    }

    public NegationExpression(Ast.NegationExpression expression)
        : this(GetTarget(expression))
    {
    }

    public IExpression Lower() => new NegationExpression(_target.Lower());

    public void EmitTo(FunctionScope scope)
    {
        _target.EmitTo(scope);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Neg));
    }

    private static IExpression GetTarget(Ast.NegationExpression expression)
    {
        expression.Deconstruct(out var target);
        return target.ToIntermediate();
    }
}
