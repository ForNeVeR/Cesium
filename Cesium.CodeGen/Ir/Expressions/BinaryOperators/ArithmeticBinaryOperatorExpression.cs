using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.BinaryOperators;

internal class ArithmeticBinaryOperatorExpression: BinaryOperatorExpression
{
    internal ArithmeticBinaryOperatorExpression(IExpression left, BinaryOperator @operator, IExpression right)
        : base(left, @operator, right)
    {
        if (!Operator.IsArithmetic())
            throw new NotSupportedException($"Internal error: operator {Operator} is not arithmetic.");
    }

    public ArithmeticBinaryOperatorExpression(Ast.ArithmeticBinaryOperatorExpression expression)
        : base(expression)
    {
    }

    public override IExpression Lower() => new ArithmeticBinaryOperatorExpression(Left.Lower(), Operator, Right.Lower());

    public override void EmitTo(IDeclarationScope scope)
    {
        var type = GetExpressionType(scope);
        var leftType = Left.GetExpressionType(scope);
        var rightType = Right.GetExpressionType(scope);

        Left.EmitTo(scope);
        EmitConversion(scope, leftType, type);

        Right.EmitTo(scope);
        EmitConversion(scope, rightType, type);

        var opcode = Operator switch
        {
            BinaryOperator.Add => OpCodes.Add,
            BinaryOperator.Multiply => OpCodes.Mul,
            _ => throw new NotSupportedException($"Operator {Operator} is not arithmetic.")
        };
        scope.Method.Body.Instructions.Add(Instruction.Create(opcode));
    }

    public override TypeReference GetExpressionType(IDeclarationScope scope)
    {
        var leftType = Left.GetExpressionType(scope);
        var rightType = Right.GetExpressionType(scope);

        return scope.TypeSystem.GetCommonNumericType(leftType, rightType);
    }
}
