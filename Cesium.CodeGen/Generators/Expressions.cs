using Cesium.Ast;
using Mono.Cecil.Cil;
using Yoakke.C.Syntax;

namespace Cesium.CodeGen.Generators;

internal static class Expressions
{
    public static void EmitExpression(FunctionScope scope, Expression expression)
    {
        switch (expression)
        {
            case ConstantExpression c:
                EmitConstantExpression(scope, c);
                break;
            case BinaryOperatorExpression b:
                EmitBinaryOperatorExpression(scope, b);
                break;
            default:
                throw new Exception($"Expression not supported: {expression}.");
        }
    }

    private static void EmitConstantExpression(FunctionScope scope, ConstantExpression expression)
    {
        var token = expression.Constant;
        var instruction = token switch
        {
            { Kind: CTokenType.IntLiteral } => Instruction.Create(OpCodes.Ldc_I4, int.Parse(token.Text)),
            // TODO: Optimizations like Ldc_I4_0 for selected constants
            _ => throw new Exception($"Constant token not supported: {token}.")
        };

        scope.Method.Body.Instructions.Add(instruction);
    }

    private static void EmitBinaryOperatorExpression(FunctionScope scope, BinaryOperatorExpression expression)
    {
        EmitExpression(scope, expression.Left);
        EmitExpression(scope, expression.Right);
        scope.Method.Body.Instructions.Add(Instruction.Create(GetOpCode()));

        OpCode GetOpCode() => expression.Operator switch
        {
            "+" => OpCodes.Add,
            "*" => OpCodes.Mul,
            _ => throw new Exception($"Operator not supported: {expression.Operator}.")
        };
    }
}
