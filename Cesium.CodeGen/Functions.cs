using Cesium.Ast;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static Cesium.CodeGen.Statements;

namespace Cesium.CodeGen;

internal static class Functions
{
    public static void EmitMainFunction(MethodDefinition method, FunctionDefinition function)
    {
        // TODO: Alternate signature support.
        if (function.Statement.Block.IsEmpty)
        {
            // TODO: Better definite return analysis.
            var instructions = method.Body.Instructions;
            instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        else
        {
            EmitFunction(method, function);
        }
    }

    public static void EmitFunction(MethodDefinition method, FunctionDefinition function)
    {
        foreach (var statement in function.Statement.Block)
        {
            EmitStatement(method, (Statement)statement);
        }
    }
}
