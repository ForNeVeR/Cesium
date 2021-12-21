using Cesium.Ast;
using Cesium.CodeGen.Extensions;
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
            case AssignmentExpression a:
                EmitAssignmentExpression(scope, a);
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
            // TODO: Optimizations like Ldc_I4_0 for selected constants
            { Kind: CTokenType.IntLiteral } => Instruction.Create(OpCodes.Ldc_I4, int.Parse(token.Text)),
            { Kind: CTokenType.Identifier, Text: var name } =>
                Instruction.Create(OpCodes.Ldloc, scope.Variables[name]),
            _ => throw new Exception($"Constant token not supported: {token.Kind} {token.Text}.")
        };

        scope.Method.Body.Instructions.Add(instruction);
    }

    private static void EmitBinaryOperatorExpression(FunctionScope scope, BinaryOperatorExpression expression)
    {
        EmitExpression(scope, expression.Left);
        EmitExpression(scope, expression.Right);
        scope.Method.Body.Instructions.Add(GetInstruction());

        Instruction GetInstruction() => expression.Operator switch
        {
            "+" => Instruction.Create(OpCodes.Add),
            "*" => Instruction.Create(OpCodes.Mul),
            _ => throw new Exception($"Operator not supported: {expression.Operator}.")
        };
    }

    private static void EmitAssignmentExpression(FunctionScope scope, AssignmentExpression expression)
    {
        EmitExpression(scope, expression.Right);

        switch (expression.Operator)
        {
            case "=":
                var nameToken = ((ConstantExpression)expression.Left).Constant;
                if (nameToken.Kind != CTokenType.Identifier)
                    throw new Exception($"Not an lvalue: {nameToken.Kind} {nameToken.Text}");

                scope.StLoc(scope.Variables[nameToken.Text]);
                break;
            default:
                throw new Exception($"Assignment expression not supported: {expression.Operator}.");
        }
    }
}
