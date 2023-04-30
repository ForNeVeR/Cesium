using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using PointerType = Cesium.CodeGen.Ir.Types.PointerType;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class FunctionDefinition : IBlockItem
{
    public List<IBlockItem>? NextNodes { get; set; }
    public IBlockItem? Parent { get; set; }

    private const string MainFunctionName = "main";

    private readonly FunctionType _functionType;
    private readonly string _name;
    private readonly CompoundStatement _statement;

    private bool IsMain => _name == MainFunctionName;

    public FunctionDefinition(Ast.FunctionDefinition function)
    {
        var (specifiers, declarator, declarations, astStatement) = function;
        var (type, name, cliImportMemberName) = LocalDeclarationInfo.Of(specifiers, declarator);
        _functionType = type as FunctionType
                        ?? throw new AssertException($"Function of not a function type: {type}.");
        _name = name ?? throw new AssertException($"Function without name: {function}.");

        if (declarations?.IsEmpty == false)
            throw new WipException(
                231,
                $"Non-empty declaration list for a function is not yet supported: {string.Join(", ", declarations)}.");

        if (cliImportMemberName != null)
            throw new CompilationException($"CLI import specifier on a function declaration: {function}.");
        _statement = astStatement.ToIntermediate();
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        return this;
    }

    public void EmitTo(IEmitScope scope)
    {
        var context = scope.Context;
        var (parameters, returnType) = _functionType;
        var resolvedReturnType = returnType.Resolve(context);
        if (IsMain && resolvedReturnType != context.TypeSystem.Int32)
            throw new CompilationException(
                $"Invalid return type for the {_name} function: " +
                $"int expected, got {returnType}.");

        if (IsMain && parameters?.IsVarArg == true)
            throw new WipException(196, $"Variable arguments for the {_name} function aren't supported.");

        var declaration = context.Functions.GetValueOrDefault(_name);
        declaration?.VerifySignatureEquality(_name, parameters, returnType);

        var method = declaration switch
        {
            null => context.GlobalType.DefineMethod(context, _name, resolvedReturnType, parameters),
            { MethodReference: MethodDefinition md } => md,
            _ => throw new CompilationException($"Function {_name} already defined as immutable.")
        };

        if (declaration?.IsDefined == true)
            throw new CompilationException($"Double definition of function {_name}.");

        if (declaration == null)
        {
            declaration = new FunctionInfo(parameters, returnType, method, IsDefined: true);
            context.Functions.Add(_name, declaration);
        }
        else
            context.Functions[_name] = declaration with { IsDefined = true };

        var functionScope = new FunctionScope(context, declaration, method);
        if (IsMain)
        {
            var entryPoint = GenerateSyntheticEntryPoint(context, method);

            var assembly = context.Assembly;
            var currentEntryPoint = assembly.EntryPoint;
            if (currentEntryPoint != null)
                throw new CompilationException(
                    $"Function {_name} cannot override existing entry point for assembly {assembly}.");

            assembly.EntryPoint = entryPoint;
        }

        EmitCode(context, functionScope);
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
        if (_functionType.Parameters == null)
        {
            // TODO[#87]: Decide whether this is normal or not.
            return GenerateSyntheticEntryPointSimple(context, userEntrypoint);
        }

        var (parameterList, isVoid, isVarArg) = _functionType.Parameters;
        if (isVoid)
        {
            return GenerateSyntheticEntryPointSimple(context, userEntrypoint);
        }

        if (isVarArg)
            throw new WipException(196, $"Variable arguments for the {_name} function aren't supported, yet.");

        if (parameterList.Count != 2)
            throw new CompilationException(
                $"Invalid parameter count for the {_name} function: " +
                $"2 expected, got {parameterList.Count}.");

        bool isValid = true;

        var argc = parameterList[0];
        if (argc.Type is not PrimitiveType { Kind: PrimitiveTypeKind.Int }) isValid = false;

        var argv = parameterList[1];
        if (argv.Type is not PointerType
            {
                Base: PointerType { Base: PrimitiveType { Kind: PrimitiveTypeKind.Char } }
            }) isValid = false;

        if (!isValid)
            throw new CompilationException(
                $"Invalid parameter types for the {_name} function: " +
                "int, char*[] expected.");

        return GenerateSyntheticEntryPointStrArray(context, userEntrypoint);
    }

    private MethodDefinition GenerateSyntheticEntryPointStrArray(
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

        var argC = new Mono.Cecil.Cil.VariableDefinition(context.TypeSystem.Int32); // 0
        syntheticEntrypoint.Body.Variables.Add(argC);

        var argV = new Mono.Cecil.Cil.VariableDefinition(bytePtrArrayType); // 1
        syntheticEntrypoint.Body.Variables.Add(argV);

        // argVCopy is a copy for the user which could be changed during the program execution. Only original argV
        // strings will be freed at the end of the execution, though, since we don't know how any other strings may have
        // been allocated by the user.
        var argVCopy = new Mono.Cecil.Cil.VariableDefinition(bytePtrArrayType); // 2
        syntheticEntrypoint.Body.Variables.Add(argVCopy);

        var argVPinned = new Mono.Cecil.Cil.VariableDefinition(bytePtrArrayType.MakePinnedType()); // 3
        syntheticEntrypoint.Body.Variables.Add(argVPinned);

        var exitCode = new Mono.Cecil.Cil.VariableDefinition(context.TypeSystem.Int32); // 4
        syntheticEntrypoint.Body.Variables.Add(exitCode);

        var instructions = syntheticEntrypoint.Body.Instructions;

        // argC = args.Length;
        instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); // args
        instructions.Add(Instruction.Create(OpCodes.Ldlen));
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

    private MethodDefinition GenerateSyntheticEntryPointSimple(
        TranslationUnitContext context,
        MethodReference userEntrypoint)
    {
        var syntheticEntrypoint = new MethodDefinition(
            "<SyntheticEntrypoint>",
            MethodAttributes.Public | MethodAttributes.Static,
            context.TypeSystem.Int32);
        context.ModuleType.Methods.Add(syntheticEntrypoint);

        var exit = context.GetRuntimeHelperMethod("Exit");

        var exitCode = new Mono.Cecil.Cil.VariableDefinition(context.TypeSystem.Int32); // 4
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

    private void EmitCode(TranslationUnitContext context, FunctionScope scope)
    {
        var statement = _statement.Lower(scope);

        statement.EmitTo(scope);
        statement.ResolveParents(statement);
        statement.ResolveNextNodes(statement, statement);
        var syntheticRetRequired = !statement.CheckNextNodes(_functionType.ReturnType.Resolve(context) != context.TypeSystem.Void);

        if (syntheticRetRequired)
        {
            var instructions = scope.Method.Body.Instructions;
            instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        //
        // if ()
        // {
        //     if (_functionType.ReturnType.Resolve(context) == context.TypeSystem.Void)
        //     {
        //         var instructions = scope.Method.Body.Instructions;
        //         instructions.Add(Instruction.Create(OpCodes.Ret));
        //     }
        //     else if (IsMain)
        //     {
        //         var instructions = scope.Method.Body.Instructions;
        //         instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        //         instructions.Add(Instruction.Create(OpCodes.Ret));
        //     }
        //     else
        //     {
        //         throw new CompilationException($"Function {scope.Method.Name} has no return statement.");
        //     }
        // }
    }
}
