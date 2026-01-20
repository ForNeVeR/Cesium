// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.ControlFlow;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.Constants;

namespace Cesium.CodeGen.Tests;

public class FlowGraphTests
{
    [Fact]
    public void EmptyStatement()
    {
        var compound = new CompoundStatement(new List<IBlockItem>());

        var flow = new FlowGraph(compound);

        Assert.Equivalent(new BasicBlock[0], flow.BasicBlocks);
    }
    [Fact]
    public void SingleExpressionStatement()
    {
        var e1 = new ExpressionStatement(new ConstantLiteralExpression(new IntegerConstant(5)));
        var compound = new CompoundStatement(new List<IBlockItem>()
        {
            e1,
        });

        var flow = new FlowGraph(compound);

        var bb = Assert.Single(flow.BasicBlocks);
        
        Assert.Equivalent(new IBlockItem[] { e1 }, bb.Statements);
    }
    [Fact]
    public void LabelCreateNew()
    {
        var e1 = new ExpressionStatement(new ConstantLiteralExpression(new IntegerConstant(5)));
        var e2 = new ExpressionStatement(new ConstantLiteralExpression(new IntegerConstant(15)));
        var l1 = new LabeledNopStatement("L1");
        var compound = new CompoundStatement(
        [
            e1,
            l1,
            e2,
        ]);

        var flow = new FlowGraph(compound);

        Assert.Equal(2, flow.BasicBlocks.Count);

        Assert.Equivalent(new IBlockItem[] { e1 }, flow.BasicBlocks[0].Statements);
        Assert.Equivalent(new BasicBlock[0], flow.BasicBlocks[0].Sources);
        Assert.Equivalent(new BasicBlock[] { flow.BasicBlocks[1] }, flow.BasicBlocks[0].Targets);
        Assert.Equivalent(new IBlockItem[] { l1, e2 }, flow.BasicBlocks[1].Statements);
        Assert.Equivalent(new BasicBlock[] { flow.BasicBlocks[0] }, flow.BasicBlocks[1].Sources);
        Assert.Equivalent(new BasicBlock[0], flow.BasicBlocks[1].Targets);
    }
    [Fact]
    public void FirstLabel()
    {
        var e1 = new ExpressionStatement(new ConstantLiteralExpression(new IntegerConstant(5)));
        var l1 = new LabeledNopStatement("L1");
        var e2 = new ExpressionStatement(new ConstantLiteralExpression(new IntegerConstant(15)));
        var compound = new CompoundStatement(
        [
            l1,
            e1,
            e2,
        ]);

        var flow = new FlowGraph(compound);

        Assert.Equal(2, flow.BasicBlocks.Count);

        Assert.Equivalent(new IBlockItem[0], flow.BasicBlocks[0].Statements);
        Assert.Equivalent(new BasicBlock[0], flow.BasicBlocks[0].Sources);
        Assert.Equivalent(new BasicBlock[] { flow.BasicBlocks[1] }, flow.BasicBlocks[0].Targets);
        Assert.Equivalent(new IBlockItem[] { l1, e1, e2 }, flow.BasicBlocks[1].Statements);
        Assert.Equivalent(new BasicBlock[] { flow.BasicBlocks[0] }, flow.BasicBlocks[1].Sources);
        Assert.Equivalent(new BasicBlock[0], flow.BasicBlocks[1].Targets);
    }
    [Fact]
    public void ForRegression()
    {
        var e1 = new ExpressionStatement(new ConstantLiteralExpression(new IntegerConstant(5)));
        var l1 = new LabeledNopStatement("L1");
        var l2 = new LabeledNopStatement("L2");
        var l3 = new LabeledNopStatement("L3");
        var g1 = new GoToStatement("L1");
        var compound = new CompoundStatement(
        [
            l1,
            e1,
            l2,
            g1,
            l3,
        ]);

        var flow = new FlowGraph(compound);

        Assert.Equal(3, flow.BasicBlocks.Count);

        Assert.Equivalent(new IBlockItem[0], flow.BasicBlocks[0].Statements);
        Assert.Equivalent(new BasicBlock[0], flow.BasicBlocks[0].Sources);
        Assert.Equivalent(new BasicBlock[] { flow.BasicBlocks[1] }, flow.BasicBlocks[0].Targets);

        Assert.Equivalent(new IBlockItem[] { l1, e1 }, flow.BasicBlocks[1].Statements);
        Assert.Equivalent(new BasicBlock[0], flow.BasicBlocks[1].Sources);
        Assert.Equivalent(new BasicBlock[] { flow.BasicBlocks[2] }, flow.BasicBlocks[1].Targets);

        Assert.Equivalent(new IBlockItem[] { l2, g1 }, flow.BasicBlocks[2].Statements);
        Assert.Equivalent(new BasicBlock[] { flow.BasicBlocks[1] }, flow.BasicBlocks[2].Sources);
        Assert.Equivalent(new BasicBlock[0], flow.BasicBlocks[2].Targets);
    }
}
