using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.BinaryOperators;

internal class ArithmeticBinaryOperatorExpression: BinaryOperatorExpression
{
    internal ArithmeticBinaryOperatorExpression(IExpression left, BinaryOperator @operator, IExpression right)
        : base(left, @operator, right)
    {
        if (!Operator.IsArithmetic())
            throw new AssertException($"Operator {Operator} is not arithmetic.");
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
            BinaryOperator.Subtract => OpCodes.Sub,
            BinaryOperator.Multiply => OpCodes.Mul,
            _ => throw new AssertException($"Operator {Operator} is not arithmetic.")
        };
        scope.Method.Body.Instructions.Add(Instruction.Create(opcode));
    }

    public override IType GetExpressionType(IDeclarationScope scope)
    {
        var leftType = Left.GetExpressionType(scope);
        var rightType = Right.GetExpressionType(scope);

        return scope.CTypeSystem.GetCommonNumericType(leftType, rightType);
    }
}
