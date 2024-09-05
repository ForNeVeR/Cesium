using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using QuikGraph;

namespace Cesium.CodeGen.Ir.ControlFlow;

internal sealed class ControlFlowChecker
{
    // we don't want structural equality here
    class CodeBlockVertex
    {
        public IBlockItem? BlockItem { get; }
        public bool Terminator { get; }
        public bool? Reached { get; set; }

        public CodeBlockVertex(IBlockItem? blockItem, bool terminator = false)
        {
            BlockItem = blockItem;
            Terminator = terminator;
        }
    }

    record CodeBlockEdge(CodeBlockVertex Source, CodeBlockVertex Target) : IEdge<CodeBlockVertex>;

    private static (CodeBlockVertex, List<CodeBlockVertex>) AddStatementToGraph(IMutableVertexAndEdgeSet<CodeBlockVertex, CodeBlockEdge> graph, IBlockItem stmt)
    {
        switch (stmt)
        {
            case ExpressionStatement:
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
            case CompoundStatement compound:
            {
                // copypasted from code below (think how to generalize)
                var compoundVtx = new CodeBlockVertex(compound);
                graph.AddVertex(compoundVtx);

                List<CodeBlockVertex> unboundVertices = new()
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
            case DoWhileStatement:
            case WhileStatement:
            case ForStatement:
            case SwitchStatement:
            case DeclarationBlockItem:
                throw new ArgumentOutOfRangeException(nameof(stmt), stmt.GetType().Name + " should be lowered");
            default:
                throw new ArgumentOutOfRangeException(nameof(stmt), stmt.GetType().Name);
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

    private static void AnalyzeReachability(IMutableVertexAndEdgeSet<CodeBlockVertex, CodeBlockEdge> graph)
    {
        var start = graph.Vertices.First();
        AnalyzeReachability(graph, start);
        foreach (var item in graph.Vertices)
        {
            if (item.Reached == null)
                item.Reached = false;
        }
    }

    private static void AnalyzeReachability(IMutableVertexAndEdgeSet<CodeBlockVertex, CodeBlockEdge> graph, CodeBlockVertex vertex)
    {
        if (vertex.Reached != null) return;
        vertex.Reached = true;
        foreach (var node in graph.Edges.Where(x => x.Source == vertex).Select(x => x.Target))
        {
            AnalyzeReachability(graph, node);
        }
    }

    private static CodeBlockVertex FillGraph(IMutableVertexAndEdgeSet<CodeBlockVertex, CodeBlockEdge> graph, CompoundStatement statement)
    {
        var start = new CodeBlockVertex(null);
        var terminator = new CodeBlockVertex(null, true);

        graph.AddVertex(start);
        graph.AddVertex(terminator);

        List<CodeBlockVertex> unboundVertices = new()
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
                    return x.BlockItem switch
                    {
                        LabelStatement { Identifier: { } id } when id == goTo.Identifier => true,
                        _ => false,
                    };
                });

                graph.AddEdge(new CodeBlockEdge(vtx, label));
            }

            if (vtx.BlockItem is ReturnStatement)
            {
                graph.AddEdge(new CodeBlockEdge(vtx, terminator));
            }
        }

        return terminator;
    }

    private static (bool Success, IBlockItem Result) InsertSyntheticReturn(IBlockItem statement, IBlockItem target, IBlockItem replacement)
    {
        switch (statement)
        {
            case AmbiguousBlockItem:
            case BreakStatement:
            case ContinueStatement:
            case InitializationBlockItem:
            case TagBlockItem:
            case TypeDefBlockItem:
            case ReturnStatement:
            case ExpressionStatement:
            case FunctionDeclaration:
            case FunctionDefinition:
            case GoToStatement:
            case GlobalVariableDefinition:
                return (false, statement);
            case SwitchStatement:
            case WhileStatement:
            case DoWhileStatement:
            case ForStatement:
            case CaseStatement:
            case DeclarationBlockItem:
                throw new AssertException("Should be lowered");
            case CompoundStatement s:
            {
                for (var i = 0; i < s.Statements.Count; i++)
                {
                    var stmt = s.Statements[i];

                    if (ReferenceEquals(stmt, target))
                    {
                        var copy = s.Statements.ToList();
                        copy[i] = replacement;

                        return (true, s with { Statements = copy });
                    }

                    if (InsertSyntheticReturn(stmt, target, replacement) is { Success: true, Result: { } newStmt })
                    {
                        var copy = s.Statements.ToList();
                        copy[i] = newStmt;

                        return (true, s with { Statements = copy });
                    }
                }

                return (false, s);
            }
            case IfElseStatement s:
            {
                if (ReferenceEquals(s.TrueBranch, target))
                {
                    return (true, s with { TrueBranch = replacement });
                }

                if (InsertSyntheticReturn(s.TrueBranch, target, replacement) is { Success: true, Result: { } newTrueStmt })
                {
                    return (true, s with { TrueBranch = newTrueStmt });
                }

                if (s.FalseBranch != null)
                {
                    if (ReferenceEquals(s.FalseBranch, target))
                    {
                        return (true, s with { FalseBranch = replacement });
                    }

                    if (InsertSyntheticReturn(s.FalseBranch, target, replacement) is { Success: true, Result: { } newFalseStmt })
                    {
                        return (true, s with { FalseBranch = newFalseStmt });
                    }
                }

                return (false, s);
            }
            case LabelStatement s:
            {
                if (ReferenceEquals(s.Expression, target))
                {
                    return (true, s with { Expression = replacement });
                }

                if (InsertSyntheticReturn(s.Expression, target, replacement) is { Success: true, Result: { } newStmt })
                {
                    return (true, s with { Expression = newStmt });
                }

                return (false, s);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(statement));
        }
    }

    public static IBlockItem CheckAndTransformControlFlow(
        FunctionScope scope,
        CompoundStatement block,
        IType returnType,
        bool isMain
    )
    {
        var dset = new AdjacencyGraph<CodeBlockVertex, CodeBlockEdge>();
        var terminator = FillGraph(dset, block);
        AnalyzeReachability(dset);

        var preTerminators = dset.Edges.Where(x => x.Target == terminator).Select(x => x.Source).ToArray();
        if (preTerminators.Length == 0)
            // log
            Console.WriteLine("Code does not terminate");

        var isVoidFn = returnType.Equals(CTypeSystem.Void);
        var isReturnRequired = !isVoidFn && !isMain;

        if (isVoidFn)
        {
            var hasExpressionReturn = (ReturnStatement?)dset.Vertices.FirstOrDefault(_ => _.BlockItem is ReturnStatement { Expression: { } })?.BlockItem;
            if (hasExpressionReturn is not null)
            {
                throw new CompilationException($"Function {scope.Method.Name} has return type void, and thus cannot have expression in return.");
            }
        }

        foreach (var preTerminator in preTerminators)
        {
            if (preTerminator.BlockItem is ReturnStatement) continue;
            if (preTerminator.Reached == false) continue;

            // inserting fake return
            var retn = new ReturnStatement(!isVoidFn ? new ConstantLiteralExpression(new IntegerConstant(0)) : null);

            if (preTerminator.BlockItem is {} original)
            {
                var (success, result) = InsertSyntheticReturn(block, original, new CompoundStatement(new List<IBlockItem> { original, retn }));

                if (!success)
                    throw new CompilationException("[internal] Unable to insert fake return.");

                block = (CompoundStatement) result;
            }
            else
            {
                // terminator directly follows start node
                if (block.Statements.Count != 0)
                    throw new CompilationException("[internal] Function terminates immediately, but there was statements");

                block.Statements.Add(retn);
            }
        }

        if (preTerminators.All(pt => pt.Reached == false))
        {
            var retn = new ReturnStatement(isMain ? new ConstantLiteralExpression(new IntegerConstant(0)) : null);
            block.Statements.Add(retn);
        }

        return block;
    }
}
