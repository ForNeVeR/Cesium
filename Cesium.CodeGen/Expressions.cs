using Cesium.Ast;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Yoakke.C.Syntax;

namespace Cesium.CodeGen;

internal static class Expressions
{
    public static void EmitExpression(MethodDefinition method, Expression expression)
    {
        switch (expression)
        {
            case ConstantExpression c:
                EmitConstantExpression(method, c);
                break;
            case BinaryOperatorExpression b:
                EmitBinaryOperatorExpression(method, b);
                break;
            default:
                throw new Exception($"Expression not supported: {expression}.");
        }
    }

    private static void EmitConstantExpression(MethodDefinition method, ConstantExpression expression)
    {
        var token = expression.Constant;
        var instruction = token switch
        {
            { Kind: CTokenType.IntLiteral } => Instruction.Create(OpCodes.Ldc_I4, int.Parse(token.Text)),
            // TODO: Optimizations like Ldc_I4_0 for selected constants
            _ => throw new Exception($"Constant token not supported: {token}.")
        };

        method.Body.Instructions.Add(instruction);
    }

    private static void EmitBinaryOperatorExpression(MethodDefinition method, BinaryOperatorExpression expression)
    {
        EmitExpression(method, expression.Left);
        EmitExpression(method, expression.Right);
        method.Body.Instructions.Add(Instruction.Create(GetOpCode()));

        OpCode GetOpCode() => expression.Operator switch
        {
            "+" => OpCodes.Add,
            "*" => OpCodes.Mul,
            _ => throw new Exception($"Operator not supported: {expression.Operator}.")
        };
    }
}
