using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Cesium.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using PointerType = Cesium.CodeGen.Ir.Types.PointerType;

namespace Cesium.CodeGen.Ir.TopLevel;

internal class FunctionDefinition : ITopLevelNode
{
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
                        ?? throw new NotSupportedException($"Function of not a function type: {type}.");
        _name = name ?? throw new NotSupportedException($"Function without name: {function}.");

        if (declarations?.IsEmpty == false)
            throw new NotImplementedException(
                $"Non-empty declaration list for a function is not yet supported: {string.Join(", ", declarations)}.");

        if (cliImportMemberName != null)
            throw new NotSupportedException($"CLI import specifier on a function declaration: {function}.");
        _statement = astStatement.ToIntermediate();
    }

    public void EmitTo(TranslationUnitContext context)
    {
        var (parameters, returnType) = _functionType;
        var resolvedReturnType = returnType.Resolve(context);
        if (IsMain && resolvedReturnType != context.TypeSystem.Int32)
            throw new NotSupportedException(
                $"Invalid return type for the {_name} function: " +
                $"int expected, got {returnType}.");

        if (IsMain && parameters?.IsVarArg == true)
            throw new NotSupportedException($"Variable arguments for the {_name} function aren't supported.");

        var declaration = context.Functions.GetValueOrDefault(_name);
        declaration?.VerifySignatureEquality(_name, parameters, returnType);

        var method = declaration switch
        {
            null => context.GlobalType.DefineMethod(context, _name, resolvedReturnType, parameters),
            { MethodReference: MethodDefinition md } => md,
            _ => throw new NotSupportedException($"Function {_name} already defined as immutable.")
        };

        if (declaration?.IsDefined == true)
            throw new NotSupportedException($"Double definition of function {_name}.");

        if (declaration == null)
            context.Functions.Add(_name, new FunctionInfo(parameters, returnType, method, IsDefined: true));
        else
            context.Functions[_name] = declaration with { IsDefined = true };

        var scope = new FunctionScope(context, method);
        if (IsMain)
        {
            var isSyntheticEntryPointRequired = ValidateMainParameters();
            var entryPoint = isSyntheticEntryPointRequired ? GenerateSyntheticEntryPoint(context, method) : method;

            var assembly = context.Assembly;
            var currentEntryPoint = assembly.EntryPoint;
            if (currentEntryPoint != null)
                throw new NotSupportedException(
                    $"Function {_name} cannot override existing entry point for assembly {assembly}.");

            assembly.EntryPoint = entryPoint;
        }

        EmitCode(context, scope);
    }

    /// <remarks><see cref="GenerateSyntheticEntryPoint"/></remarks>
    /// <returns>Whether the synthetic entry point should be generated.</returns>
    private bool ValidateMainParameters()
    {
        if (_functionType.Parameters == null)
            return false; // TODO[#87]: Decide whether this is normal or not.

        var (parameterList, isVoid, isVarArg) = _functionType.Parameters;
        if (isVoid) return false; // supported, no synthetic entry point required

        if (isVarArg)
            throw new NotImplementedException($"Variable arguments for the {_name} function aren't supported, yet.");

        if (parameterList.Count != 2)
            throw new NotSupportedException(
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
            throw new NotSupportedException(
                $"Invalid parameter types for the {_name} function: " +
                "int, char*[] expected.");

        return true;
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
        var module = context.Module;
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
        var argsToArgv = module.ImportReference(typeof(RuntimeHelpers).GetMethod("ArgsToArgv"));
        var freeArgv = module.ImportReference(typeof(RuntimeHelpers).GetMethod("FreeArgv"));
        var arrayCopyTo = module.ImportReference(typeof(byte*[])
            .GetMethod("CopyTo", new[] { typeof(Array), typeof(int) }));

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
        var atExitLdLocExitCode = Instruction.Create(OpCodes.Ldloc_S, exitCode);

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
                instructions.Add(Instruction.Create(OpCodes.Leave_S, atExitLdLocExitCode));
            }
            // finally: unpin
            {
                instructions.Add(Instruction.Create(OpCodes.Ldnull));
                instructions.Add(Instruction.Create(OpCodes.Stloc_3)); // 3 = argVPinned.Index
                instructions.Add(Instruction.Create(OpCodes.Endfinally));
            }
        }
        // finally
        {
            // Cesium.Runtime.RuntimeHelpers.FreeArgv(argV);
            instructions.Add(Instruction.Create(OpCodes.Ldloc_1)); // 1 = argV.Index
            instructions.Add(Instruction.Create(OpCodes.Call, freeArgv));
            instructions.Add(Instruction.Create(OpCodes.Endfinally));
        }

        instructions.Add(atExitLdLocExitCode);
        instructions.Add(Instruction.Create(OpCodes.Ret));
        return syntheticEntrypoint;
    }

    private void EmitCode(TranslationUnitContext context, FunctionScope scope)
    {
        _statement.EmitTo(scope);
        if (!_statement.HasDefiniteReturn)
        {
            if (IsMain)
            {
                var instructions = scope.Method.Body.Instructions;
                instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                instructions.Add(Instruction.Create(OpCodes.Ret));
            }
            else if (_functionType.ReturnType.Resolve(context) == context.TypeSystem.Void)
            {
                var instructions = scope.Method.Body.Instructions;
                instructions.Add(Instruction.Create(OpCodes.Ret));
            }
            else
            {
                throw new InvalidOperationException($"{scope.Method.Name} do not have return statement. This is compiler bug.");
            }
        }
    }
}
