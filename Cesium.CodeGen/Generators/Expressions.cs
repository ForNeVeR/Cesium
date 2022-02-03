using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Generators;

internal static class Expressions // TODO[F]: Remove this class
{
    public static void EmitExpression(FunctionScope scope, Expression expression)
    {
        switch (expression)
        {
            case IntConstantExpression intConstant:
                EmitIntConstantExpression(scope, intConstant);
                break;
            case NegationExpression negationExpression:
                EmitNegationExpression(scope, negationExpression);
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

    private static void EmitNegationExpression(FunctionScope scope, NegationExpression expression)
    {
        expression.Target.ToIntermediate().Lower().EmitTo(scope);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Neg));
    }

    private static void EmitStringConstantExpression(FunctionScope scope, StringConstantExpression expression)
    {
        var fieldReference = scope.AssemblyContext.GetConstantPoolReference(expression.ConstantContent);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsflda, fieldReference));
    }


}
