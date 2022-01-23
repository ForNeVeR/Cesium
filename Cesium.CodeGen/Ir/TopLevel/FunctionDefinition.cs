using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Statements;
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

    private readonly IType _returnType;
    private readonly string _name;
    private readonly ParametersInfo _parameters;
    private readonly IStatement _statement;

    private bool IsMain => _name == MainFunctionName;

    /// <remarks><see cref="GenerateSyntheticEntryPoint"/></remarks>
    private bool IsSyntheticEntryPointRequired
    {
        get
        {
            if (!IsMain) return false;

            var (parameterList, isVoid, isVarArg) = _parameters;
            if (isVoid || isVarArg || parameterList.Count != 2) return false;

            var argc = parameterList[0];
            if (argc.Type is not PrimitiveType { Kind: PrimitiveTypeKind.Int }) return false;

            var argv = parameterList[1];
            return argv.Type is PointerType
            {
                Base: PointerType { Base: PrimitiveType { Kind: PrimitiveTypeKind.Char } }
            };
        }
    }

    public FunctionDefinition(Ast.FunctionDefinition ast)
    {
        var (specifiers, declarator, declarations, astStatement) = ast;
        var (pointer, directDeclarator) = declarator;
        if (pointer != null)
            throw new NotImplementedException(
                $"Function with pointer in declaration not supported, yet: {declarator}.");

        (_returnType, var isConstReturn, _name, _parameters) = DeclarationInfo.Of(specifiers, directDeclarator);
        if (isConstReturn)
            throw new NotImplementedException(
                $"Functions with const return type aren't supported, yet: {string.Join(", ", specifiers)}.");

        if (declarations?.IsEmpty == false)
            throw new NotImplementedException(
                $"Non-empty declaration list for a function is not yet supported: {string.Join(", ", declarations)}.");
        _statement = astStatement.ToIntermediate();
    }

    public void EmitTo(TranslationUnitContext context)
    {
        var method = new MethodDefinition(
            _name,
            MethodAttributes.Public | MethodAttributes.Static,
            _returnType.Resolve(context.TypeSystem));

        context.ModuleType.Methods.Add(method);
        context.Functions.Add(_name, method);

        var scope = new FunctionScope(context, method);
        AddParameters(scope);

        if (IsMain)
        {
            var entryPoint = IsSyntheticEntryPointRequired ? GenerateSyntheticEntryPoint(context, method) : method;

            var assembly = context.Assembly;
            var currentEntryPoint = assembly.EntryPoint;
            if (currentEntryPoint != null)
                throw new NotSupportedException(
                    $"Function {_name} cannot override existing entry point for assembly {assembly}.");

            assembly.EntryPoint = entryPoint;
        }

        _statement.EmitTo(scope);
    }

    private void AddParameters(FunctionScope scope)
    {
        var (parameters, isVoid, isVarArg) = _parameters;
        if (isVoid) return;
        if (isVarArg)
            throw new NotImplementedException($"VarArg functions not supported, yet: {_name}.");

        // TODO[#87]: Process empty (non-void) parameter list.

        foreach (var parameter in parameters)
        {
            var (type, name) = parameter;
            var parameterDefinition = new ParameterDefinition(type.Resolve(scope.TypeSystem))
            {
                Name = name
            };
            scope.Method.Parameters.Add(parameterDefinition);
            scope.Parameters.Add(parameter.Name, parameterDefinition);
        }
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

        // try
        Instruction tryStart, tryEnd;
        Instruction pinStart, pinEnd;
        Instruction unpinStart, unpinEnd;
        {
            // argVCopy = new byte*[argV.Length];
            instructions.Add(tryStart = Instruction.Create(OpCodes.Ldloc_1)); // 1 = argV.Index
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
                instructions.Add(pinStart = Instruction.Create(OpCodes.Ldloc_0)); // 0 = argC.Index
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
                instructions.Add(pinEnd = unpinStart = Instruction.Create(OpCodes.Ldnull));
                instructions.Add(Instruction.Create(OpCodes.Stloc_3)); // 3 = argVPinned.Index
                instructions.Add(Instruction.Create(OpCodes.Endfinally));
            }
        }
        // finally
        Instruction finallyStart, finallyEnd;
        {
            // Cesium.Runtime.RuntimeHelpers.FreeArgv(argV);
            instructions.Add(unpinEnd = tryEnd = finallyStart = Instruction.Create(OpCodes.Ldloc_1)); // 1 = argV.Index
            instructions.Add(Instruction.Create(OpCodes.Call, freeArgv));
            instructions.Add(Instruction.Create(OpCodes.Endfinally));
        }

        instructions.Add(finallyEnd = atExitLdLocExitCode);
        instructions.Add(Instruction.Create(OpCodes.Ret));

        var unpinHandler = new ExceptionHandler(ExceptionHandlerType.Finally)
        {
            TryStart = pinStart,
            TryEnd = pinEnd,
            HandlerStart = unpinStart,
            HandlerEnd = unpinEnd
        };
        syntheticEntrypoint.Body.ExceptionHandlers.Add(unpinHandler);

        var finallyHandler = new ExceptionHandler(ExceptionHandlerType.Finally)
        {
            TryStart = tryStart,
            TryEnd = tryEnd,
            HandlerStart = finallyStart,
            HandlerEnd = finallyEnd
        };
        syntheticEntrypoint.Body.ExceptionHandlers.Add(finallyHandler);

        return syntheticEntrypoint;
    }
}
