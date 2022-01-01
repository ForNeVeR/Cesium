using Cesium.Ast;
using Cesium.CodeGen.Contexts;
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
            case NegationExpression negationExpression:
                EmitNegationExpression(scope, negationExpression);
                break;
            case AssignmentExpression a:
                EmitAssignmentExpression(scope, a);
                break;
            case BinaryOperatorExpression b:
                EmitBinaryOperatorExpression(scope, b);
                break;
            case FunctionCallExpression f:
                EmitFunctionCallExpression(scope, f);
                break;
            case StringConstantExpression s:
                EmitStringConstantExpression(scope, s);
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

    private static void EmitNegationExpression(FunctionScope scope, NegationExpression expression)
    {
        EmitExpression(scope, expression.Target);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Neg));
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

    private static void EmitFunctionCallExpression(FunctionScope scope, FunctionCallExpression expression)
    {
        foreach (var argument in expression.Arguments ?? Enumerable.Empty<Expression>())
            EmitExpression(scope, argument);

        var functionNameToken = ((ConstantExpression)expression.Function).Constant;
        if (functionNameToken.Kind != CTokenType.Identifier)
            throw new NotSupportedException(
                $"Function call {functionNameToken.Kind} {functionNameToken.Text} is not supported.");

        var functionName = functionNameToken.Text;
        var callee = scope.Functions[functionName];

        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, callee));
    }

    private static void EmitStringConstantExpression(FunctionScope scope, StringConstantExpression expression)
    {
        var fieldReference = scope.AssemblyContext.GetConstantPoolReference(expression.ConstantContent);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsflda, fieldReference));
    }
}
