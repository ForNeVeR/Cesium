// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.ControlFlow;

internal sealed class ControlFlowChecker
{
    public static IBlockItem CheckAndTransformControlFlow(
        FunctionScope scope,
        CompoundStatement block,
        IType returnType,
        bool isMain
    )
    {
        var flowGraph = new FlowGraph(block);

        var isVoidFn = returnType.Equals(CTypeSystem.Void);
        var isReturnRequired = !isVoidFn && !isMain;

        if (isVoidFn)
        {
            var hasExpressionReturn = (ReturnStatement?)flowGraph.BasicBlocks.SelectMany(_ => _.Statements).FirstOrDefault(_ => _ is ReturnStatement { Expression: { } });
            if (hasExpressionReturn is not null)
            {
                throw new CompilationException($"Function {scope.Method.Name} has return type void, and thus cannot have expression in return.");
            }
        }

        var lastBlock = flowGraph.BasicBlocks.Last();
        if (lastBlock.Statements.Count == 0 || lastBlock.Statements.Last() is not ReturnStatement and not GoToStatement)
        {
            if (isReturnRequired &&
                flowGraph.BasicBlocks.Exists(b =>
                    !b.Statements.Exists(t => t is ReturnStatement)
                    && b.Targets.Count == 0
                )
            )
            {
                throw new CompilationException($"Not all control flow paths in function {scope.Method.Name} return a value.");
            }

            var retn = new ReturnStatement(!isVoidFn ? new ConstantLiteralExpression(new IntegerConstant(0)) : null);
            lastBlock.Statements.Add(retn);
        }

        return new CompoundStatement([.. flowGraph.BasicBlocks.OfType<IBlockItem>()], scope);
    }
}

internal class BasicBlock: IBlockItem
{
    public HashSet<BasicBlock> Sources { get; } = [];
    public HashSet<BasicBlock> Targets { get; } = [];
    public List<IBlockItem> Statements { get; } = [];
}

internal class FlowGraph
{
    public BasicBlock Entry { get; } = new BasicBlock();
    public List<BasicBlock> BasicBlocks { get; } = [];
    private Dictionary<string, BasicBlock> labeledBlocks = new();
    public FlowGraph(CompoundStatement compoundStatement)
    {
        var currentBlock = Entry;
        for (int i = 0; i < compoundStatement.Statements.Count; i++)
        {
            var blockItem = compoundStatement.Statements[i];
            if (blockItem is ReturnStatement)
            {
                currentBlock.Statements.Add(blockItem);
                BasicBlocks.Add(currentBlock);
                var newBlock = new BasicBlock();
                currentBlock = newBlock;
            }
            else if (blockItem is ExpressionStatement { Expression: null })
            {
                // Do nothing if statement is empty.
            }
            else if (blockItem is ExpressionStatement { Expression: { } })
            {
                currentBlock.Statements.Add(blockItem);
            }
            else if (blockItem is GoToStatement gotoStatement)
            {
                currentBlock.Statements.Add(blockItem);
                BasicBlocks.Add(currentBlock);
                var gotoBlock = Lookup(gotoStatement.Identifier);
                currentBlock.Targets.Add(gotoBlock);
                var newBlock = new BasicBlock();
                currentBlock = newBlock;
            }
            else if (blockItem is ConditionalGotoStatement conditional)
            {
                bool alwaysTaken =
                    conditional is { Value: ConditionalValue.ConstantlyTrue, JumpType: ConditionalJumpType.True }
                        or { Value: ConditionalValue.ConstantlyFalse, JumpType: ConditionalJumpType.False };
                bool neverTaken =
                    conditional is { Value: ConditionalValue.ConstantlyTrue, JumpType: ConditionalJumpType.False }
                        or { Value: ConditionalValue.ConstantlyFalse, JumpType: ConditionalJumpType.True };

                if (alwaysTaken)
                {
                    BasicBlocks.Add(currentBlock);
                    var gotoStmnt = new GoToStatement(conditional.Identifier);
                    currentBlock.Statements.Add(gotoStmnt);
                    var gotoBlock = Lookup(gotoStmnt.Identifier);
                    currentBlock.Targets.Add(gotoBlock);
                    currentBlock = new BasicBlock();
                }
                else if (neverTaken)
                {
                    // Just ignore and pursue the current block
                }
                else // ConditionalValue.Unknown
                {
                    BasicBlocks.Add(currentBlock);
                    currentBlock.Statements.Add(conditional);
                    var conditionalBlock = Lookup(conditional.Identifier);
                    currentBlock.Targets.Add(conditionalBlock);
                    var nextBlock = new BasicBlock();
                    currentBlock.Targets.Add(nextBlock);
                    currentBlock = nextBlock;
                }
            }
            else if (blockItem is LabeledNopStatement labeled)
            {
                // Close existing block.
                var lastBlock = BasicBlocks.LastOrDefault();
                if (lastBlock != currentBlock)
                {
                    BasicBlocks.Add(currentBlock);
                }
                BasicBlock newBlock = new();
                //if (currentBlock.Statements.LastOrDefault()
                //    is not GoToStatement and not ReturnStatement)
                //{
                //    currentBlock.Targets.Add(newBlock);
                //}

                if (currentBlock.Statements.Count == 0)
                {
                    if (labeledBlocks.TryGetValue(labeled.Label, out var existingBlock))
                    {
                        currentBlock = existingBlock;
                    }
                    else
                    {
                        labeledBlocks.Add(labeled.Label, currentBlock);
                        if (currentBlock.Statements.LastOrDefault()
                            is not GoToStatement and not ReturnStatement)
                        {
                            currentBlock.Targets.Add(newBlock);
                        }

                        currentBlock = newBlock;
                    }

                    currentBlock.Statements.Add(labeled);
                }
                else
                {
                    if (labeledBlocks.TryGetValue(labeled.Label, out var existingBlock))
                    {
                        newBlock = existingBlock;
                        currentBlock.Targets.Add(newBlock);
                    }
                    else
                    {
                        newBlock = new();
                        currentBlock.Targets.Add(newBlock);
                        labeledBlocks.Add(labeled.Label, newBlock);
                    }
                    newBlock.Statements.Add(labeled);
                    currentBlock = newBlock;
                }
            }
            else
            {
                throw new NotSupportedException($"Block item of type {blockItem.GetType()} is not supported in FlowGraph.");
            }
        }

        if (BasicBlocks.LastOrDefault() != currentBlock)
        {
            BasicBlocks.Add(currentBlock);
        }

        foreach (BasicBlock bb in BasicBlocks)
        {
            foreach (BasicBlock tar in bb.Targets)
                tar.Sources.Add(bb);
        }

    startagain:
        for (var i = BasicBlocks.Count - 1; i > 0; i--)
        {
            var bb = BasicBlocks[i];
            if (bb == Entry)
            {
                continue;
            }

            if (bb.Sources.Count == 0 || bb.Sources.All(_ =>_.Statements.LastOrDefault() is ReturnStatement))
            {
                BasicBlocks.RemoveAt(i);
                foreach (var item in bb.Targets)
                {
                    item.Sources.Remove(bb);
                }
                goto startagain;
            }
        }
    }
    private BasicBlock Lookup(string label)
    {
        if (labeledBlocks.TryGetValue(label, out var block))
        {
            return block;
        }
        else
        {
            block = new BasicBlock();
            labeledBlocks.Add(label, block);
            return block;
        }
    }
}
