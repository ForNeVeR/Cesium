using System.Diagnostics;
using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.ControlFlow;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Emitting;
using Cesium.CodeGen.Ir.Lowering;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using PointerType = Cesium.CodeGen.Ir.Types.PointerType;

namespace Cesium.CodeGen.Ir.BlockItems;

internal sealed class FunctionDefinition : IBlockItem
{
    private const string MainFunctionName = "main";

    public FunctionType FunctionType { get; }
    public StorageClass StorageClass { get; }
    public string Name { get; }
    public IBlockItem Statement { get; }

    public bool IsMain => Name == MainFunctionName;

    public FunctionDefinition(Ast.FunctionDefinition function)
    {
        var (specifiers, declarator, declarations, astStatement) = function;
        StorageClass = StorageClass.Auto;
        var staticMarker = specifiers.FirstOrDefault(_ => _ is StorageClassSpecifier storageClass && storageClass.Name == "static");
        if (staticMarker is not null)
        {
            StorageClass = StorageClass.Static;
            specifiers = specifiers.Remove(staticMarker);
        }

        var (type, name, cliImportMemberName) = LocalDeclarationInfo.Of(specifiers, declarator);
        FunctionType = type as FunctionType
                        ?? throw new AssertException($"Function of not a function type: {type}.");
        Name = name ?? throw new AssertException($"Function without name: {function}.");

        if (declarations?.IsEmpty == false)
            throw new WipException(
                231,
                $"Non-empty declaration list for a function is not yet supported: {string.Join(", ", declarations)}.");

        if (cliImportMemberName != null)
            throw new CompilationException($"CLI import specifier on a function declaration: {function}.");
        Statement = astStatement.ToIntermediate();
    }

    public FunctionDefinition(string name, StorageClass storageClass, FunctionType functionType, IBlockItem statement)
    {
        StorageClass = storageClass;
        Name = name;
        FunctionType = functionType;
        Statement = statement;
    }

    public void EmitCode(IEmitScope scope)
    {
        var context = scope.Context;
        var (parameters, returnType) = FunctionType;

        var declaration = context.GetFunctionInfo(Name);
        Debug.Assert(declaration != null, $"Function {Name} does not declared.");

        var method = declaration switch
        {
            { MethodReference: null } => context.DefineMethod(Name, StorageClass, returnType, parameters),
            { MethodReference: MethodDefinition md } => md,
            _ => throw new CompilationException($"Function {Name} already defined as immutable.")
        };

        var functionScope = new FunctionScope(context, declaration, method);
        if (IsMain)
        {
            var entryPoint = GenerateSyntheticEntryPoint(context, method);

            var assembly = context.Assembly;
            var currentEntryPoint = assembly.EntryPoint;
            if (currentEntryPoint != null)
                throw new CompilationException(
                    $"Function {Name} cannot override existing entry point for assembly {assembly}.");

            assembly.EntryPoint = entryPoint;
        }

        EmitCode(functionScope);
    }

    /// <summary>
    /// One of the standard signatures for the main function is <code>int main(int argc, char *argv[])</code>. This
    /// isn't directly supported by the CLI infrastructure, so we'll have to emit a synthetic entrypoint for this case,
    /// which will accept a managed string array and prepare the arguments for the C function.
    /// </summary>
    /// <returns>A synthetic entrypoint method created.</returns>
    private MethodDefinition GenerateSyntheticEntryPoint(
        TranslationUnitContext context,
        MethodReference userEntrypoint)
    {
        if (FunctionType.Parameters == null)
        {
            // TODO[#87]: Decide whether this is normal or not.
            return GenerateSyntheticEntryPointSimple(context, userEntrypoint);
        }

        var (parameterList, isVoid, isVarArg) = FunctionType.Parameters;
        if (isVoid)
        {
            return GenerateSyntheticEntryPointSimple(context, userEntrypoint);
        }

        if (isVarArg)
            throw new WipException(196, $"Variable arguments for the {Name} function aren't supported, yet.");

        if (parameterList.Count != 2)
            throw new CompilationException(
                $"Invalid parameter count for the {Name} function: " +
                $"2 expected, got {parameterList.Count}.");

        bool isValid = true;

        var argc = parameterList[0];
        if (argc.Type is not PrimitiveType { Kind: PrimitiveTypeKind.Int } // int argc
            and not ConstType { Base: PrimitiveType { Kind: PrimitiveTypeKind.Int } /* const int argc */ }) isValid = false;

        var argv = parameterList[1];
        if (argv.Type is not PointerType // char** or char*[]
            {
                Base: PointerType { Base: PrimitiveType { Kind: PrimitiveTypeKind.Char } }
            } and not PointerType // [opt const] char * const *
            {
                Base: PointerType { Base: ConstType { Base: PrimitiveType { Kind: PrimitiveTypeKind.Char } } }
            }) isValid = false;

        if (!isValid)
            throw new CompilationException(
                $"Invalid parameter types for the {Name} function: " +
                "int, char*[] expected.");

        return GenerateSyntheticEntryPointStrArray(context, userEntrypoint);
    }

    private static MethodDefinition GenerateSyntheticEntryPointStrArray(
        TranslationUnitContext context,
        MethodReference userEntrypoint)
    {
        var syntheticEntrypoint = new MethodDefinition(
            "<SyntheticEntrypoint>",
            MethodAttributes.Public | MethodAttributes.Static,
            context.TypeSystem.Int32)
        {
            Parameters =
            {
                new ParameterDefinition("args", ParameterAttributes.None, context.TypeSystem.String.MakeArrayType())
            }
        };
        context.ModuleType.Methods.Add(syntheticEntrypoint);

        var bytePtrType = context.TypeSystem.Byte.MakePointerType();
        var bytePtrArrayType = bytePtrType.MakeArrayType();
        var argsToArgv = context.GetRuntimeHelperMethod("ArgsToArgv");
        var freeArgv = context.GetRuntimeHelperMethod("FreeArgv");
        var exit = context.GetRuntimeHelperMethod("Exit");
        var arrayCopyTo = context.GetArrayCopyToMethod();

        var argC = new VariableDefinition(context.TypeSystem.Int32); // 0
        syntheticEntrypoint.Body.Variables.Add(argC);

        var argV = new VariableDefinition(bytePtrArrayType); // 1
        syntheticEntrypoint.Body.Variables.Add(argV);

        // argVCopy is a copy for the user which could be changed during the program execution. Only original argV
        // strings will be freed at the end of the execution, though, since we don't know how any other strings may have
        // been allocated by the user.
        var argVCopy = new VariableDefinition(bytePtrArrayType); // 2
        syntheticEntrypoint.Body.Variables.Add(argVCopy);

        var argVPinned = new VariableDefinition(bytePtrArrayType.MakePinnedType()); // 3
        syntheticEntrypoint.Body.Variables.Add(argVPinned);

        var exitCode = new VariableDefinition(context.TypeSystem.Int32); // 4
        syntheticEntrypoint.Body.Variables.Add(exitCode);

        var instructions = syntheticEntrypoint.Body.Instructions;

        // argC = args.Length;
        instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); // args
        instructions.Add(Instruction.Create(OpCodes.Ldlen));
        // argC = 1 + argC;
        instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
        instructions.Add(Instruction.Create(OpCodes.Add));
        instructions.Add(Instruction.Create(OpCodes.Stloc_0)); // 0 = argC.Index
        // argV = Cesium.Runtime.RuntimeHelpers.ArgsToArgv(args);
        instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); // args
        instructions.Add(Instruction.Create(OpCodes.Call, argsToArgv));
        instructions.Add(Instruction.Create(OpCodes.Stloc_1)); // 1 = argV.Index

        {
            // argVCopy = new byte*[argV.Length];
            instructions.Add(Instruction.Create(OpCodes.Ldloc_1)); // 1 = argV.Index
            instructions.Add(Instruction.Create(OpCodes.Ldlen));
            instructions.Add(Instruction.Create(OpCodes.Newarr, bytePtrType));
            instructions.Add(Instruction.Create(OpCodes.Stloc_2)); // 2 = argVCopy.Index
            // argV.CopyTo(argVCopy, 0);
            instructions.Add(Instruction.Create(OpCodes.Ldloc_1)); // 1 = argV.Index
            instructions.Add(Instruction.Create(OpCodes.Ldloc_2)); // 2 = argVCopy.Index
            instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            instructions.Add(Instruction.Create(OpCodes.Call, arrayCopyTo));
            // fixed (byte** argVPtr = argVCopy)
            //     return main(argC, argVPtr);
            // pin
            {
                instructions.Add(Instruction.Create(OpCodes.Ldloc_0)); // 0 = argC.Index
                instructions.Add(Instruction.Create(OpCodes.Ldloc_2)); // 2 = argVCopy.Index
                instructions.Add(Instruction.Create(OpCodes.Stloc_3)); // 3 = argVPinned.Index
                instructions.Add(Instruction.Create(OpCodes.Ldloc_3));  // 3 = argVPinned.Index
                instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                instructions.Add(Instruction.Create(OpCodes.Ldelema, bytePtrType));
                instructions.Add(Instruction.Create(OpCodes.Call, userEntrypoint));
                instructions.Add(Instruction.Create(OpCodes.Stloc_S, exitCode));
            }
            //unpin
            {
                instructions.Add(Instruction.Create(OpCodes.Ldnull));
                instructions.Add(Instruction.Create(OpCodes.Stloc_3)); // 3 = argVPinned.Index
            }

            // Cesium.Runtime.RuntimeHelpers.FreeArgv(argV);
            instructions.Add(Instruction.Create(OpCodes.Ldloc_1)); // 1 = argV.Index
            instructions.Add(Instruction.Create(OpCodes.Call, freeArgv));
        }
        instructions.Add(Instruction.Create(OpCodes.Ldloc_S, exitCode));
        instructions.Add(Instruction.Create(OpCodes.Call, exit)); // exit(exitCode)
        instructions.Add(Instruction.Create(OpCodes.Ldloc_S, exitCode));
        instructions.Add(Instruction.Create(OpCodes.Ret));
        return syntheticEntrypoint;
    }

    private static MethodDefinition GenerateSyntheticEntryPointSimple(
        TranslationUnitContext context,
        MethodReference userEntrypoint)
    {
        var syntheticEntrypoint = new MethodDefinition(
            "<SyntheticEntrypoint>",
            MethodAttributes.Public | MethodAttributes.Static,
            context.TypeSystem.Int32);
        context.ModuleType.Methods.Add(syntheticEntrypoint);

        var exit = context.GetRuntimeHelperMethod("Exit");

        var exitCode = new VariableDefinition(context.TypeSystem.Int32); // 4
        syntheticEntrypoint.Body.Variables.Add(exitCode);

        var instructions = syntheticEntrypoint.Body.Instructions;

        // exitCode = userEntrypoint();
        instructions.Add(Instruction.Create(OpCodes.Call, userEntrypoint));
        instructions.Add(Instruction.Create(OpCodes.Stloc_S, exitCode));
        instructions.Add(Instruction.Create(OpCodes.Ldloc_S, exitCode));

        instructions.Add(Instruction.Create(OpCodes.Call, exit)); // exit(exitCode)
        instructions.Add(Instruction.Create(OpCodes.Ldloc_S, exitCode));
        instructions.Add(Instruction.Create(OpCodes.Ret));
        return syntheticEntrypoint;
    }

    private void EmitCode(FunctionScope scope)
    {
        var loweredStmt = BlockItemLowering.LowerBody(scope, Statement);
        var transformed = ControlFlowChecker.CheckAndTransformControlFlow(
            scope,
            loweredStmt,
            FunctionType.ReturnType,
            IsMain
        );

        BlockItemEmitting.EmitCode(scope, transformed);
        var isVoid = scope.FunctionInfo.ReturnType.Equals(CTypeSystem.Void);
        if (!isVoid && scope.Method.Body.Instructions.Last().OpCode != OpCodes.Ret)
        {
            scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
    }
}
