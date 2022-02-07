using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Generators;

internal static class Expressions // TODO[#73]: Remove this class.
{
    public static void EmitExpression(FunctionScope scope, Expression expression)
    {
        switch (expression)
        {
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
