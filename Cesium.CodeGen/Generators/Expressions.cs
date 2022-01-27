using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Mono.Cecil.Cil;
using Yoakke.C.Syntax;

namespace Cesium.CodeGen.Generators;

internal static class Expressions // TODO[F]: Remove this class
{
    public static void EmitExpression(FunctionScope scope, Expression expression)
    {
        switch (expression)
        {
            case ConstantExpression c:
                EmitConstantExpression(scope, c);
                break;
            case IntConstantExpression intConstant:
                EmitIntConstantExpression(scope, intConstant);
                break;
            case NegationExpression negationExpression:
                EmitNegationExpression(scope, negationExpression);
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

    private static void EmitIntConstantExpression(FunctionScope scope, IntConstantExpression expression)
    {
        var instructions = scope.Method.Body.Instructions;
        var instruction = Instruction.Create(OpCodes.Ldc_I4, expression.Constant);
        instructions.Add(instruction);
    }
    private static void EmitConstantExpression(FunctionScope scope, ConstantExpression expression)
    {
        var token = expression.Constant;
        var instructions = scope.Method.Body.Instructions;
        switch (token)
        {
            // TODO: Optimizations like Ldc_I4_0 for selected constants
            case { Kind: CTokenType.IntLiteral }:
                {
                    var instruction = Instruction.Create(OpCodes.Ldc_I4, int.Parse(token.Text));
                    instructions.Add(instruction);
                    break;
                }
            case { Kind: CTokenType.CharLiteral }:
                {
                    var charValue = UnescapeCharacter(token.Text);
                    instructions.Add(Instruction.Create(OpCodes.Ldc_I4, (int)charValue));
                    instructions.Add(Instruction.Create(OpCodes.Conv_U1));
                    break;
                }
            case { Kind: CTokenType.Identifier, Text: var name } when scope.Variables.TryGetValue(name, out var var):
                {
                    var instruction = Instruction.Create(OpCodes.Ldloc, var);
                    instructions.Add(instruction);
                    break;
                }
            case { Kind: CTokenType.Identifier, Text: var name } when scope.Parameters.TryGetValue(name, out var par):
                {
                    var instruction = Instruction.Create(OpCodes.Ldarg, par);
                    instructions.Add(instruction);
                    break;
                }
            default:
                throw new Exception($"Constant token not supported: {token.Kind} {token.Text}.");
        };
    }

    private static void EmitNegationExpression(FunctionScope scope, NegationExpression expression)
    {
        EmitExpression(scope, expression.Target);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Neg));
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

    private static char UnescapeCharacter(string text)
    {
        text = text.Replace("'", string.Empty);
        if (text.Length == 1) return text[0];
        return text[1] switch
        {
            '\'' => '\'',
            '"' => '"',
            //'?' => '\?',
            '\\' => '\\',
            'a' => '\a',
            'b' => '\b',
            'f' => '\f',
            'n' => '\n',
            'r' => '\r',
            't' => '\t',
            'v' => '\v',
            'x' => (char)int.Parse(text.AsSpan(2), System.Globalization.NumberStyles.AllowHexSpecifier),
            > '0' and < '9' => (char)Convert.ToInt32(text.Substring(2), 8),
            _ => throw new InvalidOperationException($"Unknown escape sequence '{text}'"),
        };
    }
}
