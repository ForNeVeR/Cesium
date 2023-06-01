using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Cesium.CodeGen.Ir.Expressions;

internal class UnaryOperatorExpression : IExpression
{
    public UnaryOperator Operator { get; }
    public IExpression Target { get; }

    internal UnaryOperatorExpression(UnaryOperator @operator, IExpression target)
    {
        Operator = @operator;
        Target = target;
    }

    public UnaryOperatorExpression(Ast.UnaryOperatorExpression expression)
    {
        var (@operator, target) = expression;
        Operator = GetOperatorKind(@operator);
        Target = target.ToIntermediate();
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        var loweredTarget = Target.Lower(scope);

        if (Operator == UnaryOperator.AddressOf)
        {
            if (loweredTarget is not IValueExpression expression)
                throw new CompilationException($"Required a value expression to get address, got {Target} instead.");

            var value = expression.Resolve(scope);
            if (value is not IAddressableValue aValue)
                throw new CompilationException($"Required an addressable value to get address, got {value} instead.");

            return new GetAddressValueExpression(aValue);
        }

        return new UnaryOperatorExpression(Operator, loweredTarget);
    }

    public void EmitTo(IEmitScope scope)
    {
        switch (Operator)
        {
            case UnaryOperator.AddressOf:
                throw new AssertException("Should be lowered");
            case UnaryOperator.LogicalNot:
                Target.EmitTo(scope);
                scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ceq));
                break;
            default:
                Target.EmitTo(scope);
                scope.Method.Body.Instructions.Add(GetInstruction());
                break;
        }

        Instruction GetInstruction() => Operator switch
        {
            UnaryOperator.Negation => Instruction.Create(OpCodes.Neg),
            UnaryOperator.BitwiseNot => Instruction.Create(OpCodes.Not),
            _ => throw new WipException(197, $"Unsupported unary operator: {Operator}.")
        };
    }

    public IType GetExpressionType(IDeclarationScope scope) => Operator switch
    {
        UnaryOperator.AddressOf => Target.GetExpressionType(scope).MakePointerType(), // address-of returns T*
        _ => Target.GetExpressionType(scope), // other operators return T
    };

    private static UnaryOperator GetOperatorKind(string @operator) => @operator switch
    {
        "-" => UnaryOperator.Negation,
        "!" => UnaryOperator.LogicalNot,
        "~" => UnaryOperator.BitwiseNot,
        "&" => UnaryOperator.AddressOf,
        _ => throw new WipException(197, $"Unary operator not supported, yet: {@operator}."),
    };
}
