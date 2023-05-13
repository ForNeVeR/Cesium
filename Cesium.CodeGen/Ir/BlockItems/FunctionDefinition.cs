using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using QuikGraph;
using QuikGraph.Graphviz;
using ConstantLiteralExpression = Cesium.CodeGen.Ir.Expressions.ConstantLiteralExpression;
using PointerType = Cesium.CodeGen.Ir.Types.PointerType;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class FunctionDefinition : IBlockItem
{
    private const string MainFunctionName = "main";

    private readonly FunctionType _functionType;
    private readonly StorageClass _storageClass;
    private readonly string _name;
    private readonly CompoundStatement _statement;

    private bool IsMain => _name == MainFunctionName;

    public FunctionDefinition(Ast.FunctionDefinition function)
    {
        var (specifiers, declarator, declarations, astStatement) = function;
        _storageClass = StorageClass.Auto;
        var staticMarker = specifiers.FirstOrDefault(_ => _ is StorageClassSpecifier storageClass && storageClass.Name == "static");
        if (staticMarker is not null)
        {
            _storageClass = StorageClass.Static;
            specifiers = specifiers.Remove(staticMarker);
        }

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

    private FunctionDefinition(string name, StorageClass storageClass, FunctionType functionType, CompoundStatement statement)
    {
        _storageClass = storageClass;
        _name = name;
        _functionType = functionType;
        _statement = statement;
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        var resolvedFunctionType = (FunctionType)scope.ResolveType(_functionType);
        var (parameters, returnType) = resolvedFunctionType;
        if (IsMain && !returnType.Equals(scope.CTypeSystem.Int))
            throw new CompilationException(
                $"Invalid return type for the {_name} function: " +
                $"int expected, got {returnType}.");

        if (IsMain && parameters?.IsVarArg == true)
            throw new WipException(196, $"Variable arguments for the {_name} function aren't supported.");

        var declaration = scope.GetFunctionInfo(_name);
        if (declaration?.IsDefined == true)
            if (declaration.CliImportMember is null)
                throw new CompilationException($"Double definition of function {_name}.");
            else
                throw new CompilationException($"Function {_name} already defined as immutable.");

        var newDeclaration = new FunctionInfo(parameters, returnType, _storageClass, IsDefined: true);
        scope.DeclareFunction(_name, newDeclaration);

        return new FunctionDefinition(_name, _storageClass, resolvedFunctionType, _statement);
    }

    public void EmitTo(IEmitScope scope)
    {
        var context = scope.Context;
        var (parameters, returnType) = _functionType;

        var declaration = context.GetFunctionInfo(_name);

        var method = declaration switch
        {
            { MethodReference: null } => context.DefineMethod(_name, _storageClass, returnType, parameters),
            { MethodReference: MethodDefinition md } => md,
            _ => throw new CompilationException($"Function {_name} already defined as immutable.")
        };

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
        var loweredStmt = (CompoundStatement) _statement.Lower(scope);

        var dset = new AdjacencyGraph<CodeBlockVertex, CodeBlockEdge>();
        var terminator = FillGraph(dset, loweredStmt);

        var preTerminators = dset.Edges.Where(x => x.Target == terminator).Select(x => x.Source).ToArray();
        if (preTerminators.Length == 0)
            // log
            Console.WriteLine("Code does not terminate");

        var isVoidFn = _functionType.ReturnType.Resolve(context) == context.TypeSystem.Void;
        var isReturnRequired = !isVoidFn && !IsMain;

        foreach (var preTerminator in preTerminators)
        {
            if (preTerminator.BlockItem is ReturnStatement) continue;

            if (isReturnRequired)
            {
                // bad error message
                throw new CompilationException($"Function {scope.Method.Name} has no return statement.");
            }

            // inserting fake return
            var retn = new ReturnStatement(IsMain ? new ConstantLiteralExpression(new IntegerConstant(0)) : null);

            if (preTerminator.BlockItem is {} original)
            {
                var result = loweredStmt.TryUnsafeSubstitute(original, new CompoundStatement(new List<IBlockItem> { original, retn }));

                if (!result)
                    throw new CompilationException("[internal] Unable to insert fake return.");
            }
            else
            {
                // terminator directly follows start node
                if (loweredStmt.Statements.Count != 0)
                    throw new CompilationException("[internal] Function terminates immediately, but there was statements");

                loweredStmt.Statements.Add(retn);
            }
        }

        loweredStmt.EmitTo(scope);
    }

    private (CodeBlockVertex, List<CodeBlockVertex>) AddStatementToGraph(IMutableVertexAndEdgeSet<CodeBlockVertex, CodeBlockEdge> graph, IBlockItem stmt)
    {
        switch (stmt)
        {
            case ExpressionStatement:
            case DeclarationBlockItem:
            case AmbiguousBlockItem:
            case InitializationBlockItem:
                return Atom();
            case LabelStatement label:
            {
                var labelVtx = new CodeBlockVertex(label);
                graph.AddVertex(labelVtx);

                var (nextVtx, unboundedNext) = AddStatementToGraph(graph, label.Expression);
                graph.AddEdge(new CodeBlockEdge(labelVtx, nextVtx));

                return (labelVtx, unboundedNext);
            }
            case IfElseStatement ifElse:
            {
                var ifVtx = new CodeBlockVertex(stmt);
                graph.AddVertex(ifVtx);

                var (vtx, unboundedNext) = AddStatementToGraph(graph, ifElse.TrueBranch);
                graph.AddEdge(new CodeBlockEdge(ifVtx, vtx));

                // true case does not have terminating goto or return or something
                ifElse.IsEscapeBranchRequired = unboundedNext.Count > 0;

                if (ifElse.FalseBranch != null)
                {
                    var (falseVtx, falseUnboundedNext) = AddStatementToGraph(graph, ifElse.FalseBranch);
                    graph.AddEdge(new CodeBlockEdge(ifVtx, falseVtx));

                    unboundedNext.AddRange(falseUnboundedNext);
                }
                else
                {
                    unboundedNext.Add(ifVtx);
                }

                return (ifVtx, unboundedNext);
            }
            case DoWhileStatement doWhile:
            {
                var doWhileVtx = new CodeBlockVertex(doWhile);
                graph.AddVertex(doWhileVtx);

                var (vtx, unboundedNext) = AddStatementToGraph(graph, doWhile.Body);
                graph.AddEdge(new CodeBlockEdge(doWhileVtx, vtx));

                foreach (var unbounded in unboundedNext)
                {
                    graph.AddEdge(new CodeBlockEdge(unbounded, doWhileVtx));
                }

                unboundedNext.Clear();
                unboundedNext.Add(doWhileVtx);

                return (doWhileVtx, unboundedNext);
            }
            // copypasted
            case WhileStatement doWhile:
            {
                var doWhileVtx = new CodeBlockVertex(doWhile);
                graph.AddVertex(doWhileVtx);

                var (vtx, unboundedNext) = AddStatementToGraph(graph, doWhile.Body);
                graph.AddEdge(new CodeBlockEdge(doWhileVtx, vtx));

                foreach (var unbounded in unboundedNext)
                {
                    graph.AddEdge(new CodeBlockEdge(unbounded, doWhileVtx));
                }

                unboundedNext.Clear();
                unboundedNext.Add(doWhileVtx);

                return (doWhileVtx, unboundedNext);
            }
            // copypasted
            case ForStatement doWhile:
            {
                var doWhileVtx = new CodeBlockVertex(doWhile);
                graph.AddVertex(doWhileVtx);

                var (vtx, unboundedNext) = AddStatementToGraph(graph, doWhile.Body);
                graph.AddEdge(new CodeBlockEdge(doWhileVtx, vtx));

                foreach (var unbounded in unboundedNext)
                {
                    graph.AddEdge(new CodeBlockEdge(unbounded, doWhileVtx));
                }

                unboundedNext.Clear();
                unboundedNext.Add(doWhileVtx);

                return (doWhileVtx, unboundedNext);
            }
            case CompoundStatement compound:
            {
                // copypasted from code below (think how to generalize)
                var compoundVtx = new CodeBlockVertex(compound);
                graph.AddVertex(compoundVtx);

                List<CodeBlockVertex> unboundVertices = new List<CodeBlockVertex>
                {
                    compoundVtx
                };

                foreach (var cs in compound.Statements)
                {
                    (var csv, var newUnboundVertices) = AddStatementToGraph(graph, cs);

                    foreach (var ubv in unboundVertices)
                    {
                        graph.AddEdge(new CodeBlockEdge(ubv, csv));
                    }

                    unboundVertices = newUnboundVertices;
                }

                return (compoundVtx, unboundVertices);
            }
            case ReturnStatement:
            case GoToStatement:
                return Terminator();
            case ContinueStatement:
            case BreakStatement:
                throw new ArgumentOutOfRangeException(stmt.GetType().Name + " should be lowered");
            default:
                throw new ArgumentOutOfRangeException(nameof(stmt) + " " + stmt.GetType().Name);
        }

        (CodeBlockVertex, List<CodeBlockVertex>) Atom()
        {
            var vtx = new CodeBlockVertex(stmt);
            graph.AddVertex(vtx);
            return (vtx, new List<CodeBlockVertex> { vtx });
        }

        (CodeBlockVertex, List<CodeBlockVertex>) Terminator()
        {
            var vtx = new CodeBlockVertex(stmt);
            graph.AddVertex(vtx);
            return (vtx, new List<CodeBlockVertex>() /* empty */);
        }
    }

    private CodeBlockVertex FillGraph(IMutableVertexAndEdgeSet<CodeBlockVertex, CodeBlockEdge> graph, CompoundStatement statement)
    {
        var start = new CodeBlockVertex(null);
        var terminator = new CodeBlockVertex(null, true);

        graph.AddVertex(start);
        graph.AddVertex(terminator);

        List<CodeBlockVertex> unboundVertices = new List<CodeBlockVertex>
        {
            start
        };

        foreach (var cs in statement.Statements)
        {
            (var csv, var newUnboundVertices) = AddStatementToGraph(graph, cs);

            foreach (var ubv in unboundVertices)
            {
                graph.AddEdge(new CodeBlockEdge(ubv, csv));
            }

            unboundVertices = newUnboundVertices;
        }

        foreach (var ubv in unboundVertices)
        {
            graph.AddEdge(new CodeBlockEdge(ubv, terminator));
        }

        foreach (var vtx in graph.Vertices)
        {
            if (vtx.BlockItem is GoToStatement goTo)
            {
                var label = graph.Vertices.First(x =>
                {
                    switch (x.BlockItem)
                    {
                        case LabelStatement { Identifier: { } id } when id == goTo.Identifier:
                            return true;
                        default:
                            return false;
                    }
                });

                graph.AddEdge(new CodeBlockEdge(vtx, label));
            }

            if (vtx.BlockItem is ReturnStatement)
            {
                graph.AddEdge(new CodeBlockEdge(vtx, terminator));
            }
        }

        /*
        var gviz = graph.ToGraphviz(algo =>
        {
            algo.FormatVertex += (o, e) =>
            {
                if (e.Vertex.Terminator)
                    e.VertexFormat.Label = "<terminator>";
                else
                    e.VertexFormat.Label = e.Vertex.BlockItem?.ToString() ?? "<start>";
            };
        });

        Console.WriteLine(gviz); */

        return terminator;
    }

    public record CodeBlockVertex(IBlockItem? BlockItem, bool Terminator = false);
    public record CodeBlockEdge(CodeBlockVertex Source, CodeBlockVertex Target) : IEdge<CodeBlockVertex>;

    public bool TryUnsafeSubstitute(IBlockItem original, IBlockItem replacement)
    {
        throw new NotSupportedException();
    }
}
