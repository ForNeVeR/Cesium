using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal class BitwiseNotExpression : IExpression
{
    private readonly IExpression _target;
    private BitwiseNotExpression(IExpression target)
    {
        _target = target;
    }

    public BitwiseNotExpression(Ast.BitwiseNotExpression expression)
        : this(GetTarget(expression))
    {
    }

    public IExpression Lower() => new BitwiseNotExpression(_target.Lower());

    public void EmitTo(FunctionScope scope)
    {
        _target.EmitTo(scope);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Not));
    }

    private static IExpression GetTarget(Ast.BitwiseNotExpression expression)
    {
        expression.Deconstruct(out var target);
        return target.ToIntermediate();
    }
}
