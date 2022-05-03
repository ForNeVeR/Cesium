using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal class UnaryOperatorExpression : IExpression
{
    private readonly UnaryOperator _operator;
    private readonly IExpression _target;

    internal UnaryOperatorExpression(UnaryOperator @operator, IExpression target)
    {
        _operator = @operator;
        _target = target;
    }

    public UnaryOperatorExpression(Ast.UnaryOperatorExpression expression)
    {
        var (@operator, target) = expression;
        _operator = GetOperatorKind(@operator);
        _target = target.ToIntermediate();
    }

    public virtual IExpression Lower() => new UnaryOperatorExpression(_operator, _target.Lower());

    public virtual void EmitTo(IDeclarationScope scope)
    {
        _target.EmitTo(scope);
        scope.Method.Body.Instructions.Add(GetInstruction());

        Instruction GetInstruction() => _operator switch
        {
            UnaryOperator.Negation => Instruction.Create(OpCodes.Neg),
            UnaryOperator.BitwiseNot => Instruction.Create(OpCodes.Not),
            _ => throw new NotSupportedException($"Unsupported unary operator: {_operator}.")
        };
    }

    private static UnaryOperator GetOperatorKind(string @operator) => @operator switch
    {
        "-" => UnaryOperator.Negation,
        "~" => UnaryOperator.BitwiseNot,
        _ => throw new NotSupportedException($"Unary operator not supported, yet: {@operator}."),
    };
}