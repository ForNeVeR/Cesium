using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.BinaryOperators;

internal class BitwiseBinaryOperatorExpression: BinaryOperatorExpression
{
    internal BitwiseBinaryOperatorExpression(IExpression left, BinaryOperator @operator, IExpression right)
        : base(left, @operator, right)
    {
        if (!Operator.IsBitwise())
            throw new NotSupportedException($"Internal error: operator {Operator} is not bitwise.");
    }

    public BitwiseBinaryOperatorExpression(Ast.BitwiseBinaryOperatorExpression expression)
        : base(expression)
    {
    }

    public override IExpression Lower() => new BitwiseBinaryOperatorExpression(Left.Lower(), Operator, Right.Lower());

    public override void EmitTo(IDeclarationScope scope)
    {
        Left.EmitTo(scope);
        Right.EmitTo(scope);

        var opcode = Operator switch
        {
            BinaryOperator.BitwiseAnd => OpCodes.And,
            BinaryOperator.BitwiseOr => OpCodes.Or,
            BinaryOperator.BitwiseXor => OpCodes.Xor,
            BinaryOperator.BitwiseLeftShift => OpCodes.Shl,
            BinaryOperator.BitwiseRightShift => OpCodes.Shr,
            _ => throw new NotSupportedException($"Operator {Operator} is not bitwise.")
        };

        scope.Method.Body.Instructions.Add(Instruction.Create(opcode));
    }

    public override TypeReference GetExpressionType(IDeclarationScope scope)
    {
        var leftType = Left.GetExpressionType(scope);
        if (!leftType.IsInteger(scope.TypeSystem))
            throw new NotSupportedException($"Left operand of '{Operator}' is not of integer type: {Left}");

        var rightType = Right.GetExpressionType(scope);
        if (!rightType.IsInteger(scope.TypeSystem))
            throw new NotSupportedException($"Right operand of '{Operator}' is not of integer type: {Right}");

        return leftType;
    }
}
