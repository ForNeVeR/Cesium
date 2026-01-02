// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.Core;
using System.Diagnostics;
using AmbiguousBlockItem = Cesium.Ast.AmbiguousBlockItem;
using BreakStatement = Cesium.Ast.BreakStatement;
using CaseStatement = Cesium.Ast.CaseStatement;
using CompoundStatement = Cesium.Ast.CompoundStatement;
using ContinueStatement = Cesium.Ast.ContinueStatement;
using DoWhileStatement = Cesium.Ast.DoWhileStatement;
using ExpressionStatement = Cesium.Ast.ExpressionStatement;
using ForStatement = Cesium.Ast.ForStatement;
using GoToStatement = Cesium.Ast.GoToStatement;
using IBlockItem = Cesium.CodeGen.Ir.BlockItems.IBlockItem;
using IfElseStatement = Cesium.Ast.IfElseStatement;
using LabelStatement = Cesium.Ast.LabelStatement;
using ReturnStatement = Cesium.Ast.ReturnStatement;
using SwitchStatement = Cesium.Ast.SwitchStatement;
using WhileStatement = Cesium.Ast.WhileStatement;

namespace Cesium.CodeGen.Extensions;

internal static class BlockItemEx
{
    public static IBlockItem ToIntermediate(this Ast.IBlockItem blockItem, IDeclarationScope scope) => blockItem switch
    {
        Declaration d => ToIntermediate(d, scope),
        CompoundStatement s => new Ir.BlockItems.CompoundStatement(s, scope),
        LabelStatement s => new Ir.BlockItems.LabelStatement(s, scope),
        ReturnStatement s => new Ir.BlockItems.ReturnStatement(s, scope),
        ExpressionStatement s => new Ir.BlockItems.ExpressionStatement(s, scope),
        IfElseStatement s => new Ir.BlockItems.IfElseStatement(s, scope),
        ForStatement s => new Ir.BlockItems.ForStatement(s, scope),
        WhileStatement s => new Ir.BlockItems.WhileStatement(s, scope),
        DoWhileStatement s => new Ir.BlockItems.DoWhileStatement(s, scope),
        SwitchStatement s => new Ir.BlockItems.SwitchStatement(s, scope),
        CaseStatement s => new Ir.BlockItems.CaseStatement(s, scope),
        BreakStatement => new Ir.BlockItems.BreakStatement(),
        ContinueStatement => new Ir.BlockItems.ContinueStatement(),
        GoToStatement s => new Ir.BlockItems.GoToStatement(s),
        AmbiguousBlockItem a => new Ir.BlockItems.AmbiguousBlockItem(a),
        _ => throw new WipException(206, $"Statement not supported, yet: {blockItem}.")
    };

    private static IBlockItem ToIntermediate(Declaration d, IDeclarationScope scope)
    {
        return new Ir.BlockItems.CompoundStatement(IScopedDeclarationInfo.Of(d, scope).Select(_ => _ switch
        {
            ScopedIdentifierDeclaration declaration => (IBlockItem)new DeclarationBlockItem(declaration),
            TypeDefDeclaration typeDefDeclaration => new TypeDefBlockItem(typeDefDeclaration),
            _ => throw new WipException(212, $"Unknown kind of declaration: {d}."),
        }).ToList(), null)
        { InheritScope = true };
    }

    public static void Dump(this IBlockItem blockItem, TextWriter writer, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        switch (blockItem)
        {
            case Ir.BlockItems.CompoundStatement compoundStatement:
                writer.WriteLine($"{indent}{{");
                foreach (var item in compoundStatement.Statements)
                {
                    Dump(item, writer, indentLevel + 1);
                }
                Console.WriteLine($"{indent}}}");
                break;
            case Ir.BlockItems.ExpressionStatement expressionStatement:
                writer.Write(indent + "    ");
                expressionStatement.Expression?.Dump(writer);
                writer.WriteLine(";");
                break;
            case Ir.BlockItems.IfElseStatement ifElseStatement:
                writer.Write($"{indent}    if (");
                ifElseStatement.Expression?.Dump(writer);
                writer.WriteLine(")");
                ifElseStatement.TrueBranch.Dump(writer, indentLevel + 1);
                if (ifElseStatement.FalseBranch is { } falseBranch)
                {
                    writer.WriteLine($"{indent}    else");
                    falseBranch.Dump(writer, indentLevel + 1);
                }
                break;
            case Ir.BlockItems.GoToStatement gotoStatement:
                writer.Write($"{indent}    goto ");
                writer.Write(gotoStatement.Identifier);
                writer.WriteLine(";");
                break;
            case Ir.BlockItems.LabelStatement labelStatement:
                writer.WriteLine($"{indent}{labelStatement.Identifier}:");
                labelStatement.Expression.Dump(writer, indentLevel + 1);
                break;
            case Ir.BlockItems.ReturnStatement returnStatement:
                writer.Write($"{indent}return ");
                returnStatement.Expression?.Dump(writer);
                writer.WriteLine(";");
                break;
            default:
                Debug.Assert(false, $"Dumping {blockItem.GetType().Name} not implemented");
                break;
        }
    }
    public static void Dump(this Ir.Expressions.IExpression expression, TextWriter writer)
    {
        switch (expression)
        {
            case Ir.Expressions.DiscardResultExpression discardResultExpression:
                discardResultExpression.Expression.Dump(writer);
                break;
            case Ir.Expressions.SetValueExpression setValueExpression:
                setValueExpression.Value.Dump(writer);
                writer.Write(" = ");
                setValueExpression.Expression.Dump(writer);
                break;
            case Ir.Expressions.GetValueExpression getValueExpression:
                getValueExpression.Value.Dump(writer);
                break;
            case Ir.Expressions.BinaryOperators.BinaryOperatorExpression binaryExpression:
                binaryExpression.Left.Dump(writer);
                binaryExpression.Operator.Dump(writer);
                binaryExpression.Right.Dump(writer);
                break;
            case Ir.Expressions.ConstantLiteralExpression constLiteralExpression:
                constLiteralExpression.Constant.Dump(writer);
                break;
            case Ir.Expressions.PostfixIncrementDecrementExpression.DuplicateValueExpression duplicateValueExpression:
                duplicateValueExpression.Value.Dump(writer);
                break;
            case Ir.Expressions.PostfixIncrementDecrementExpression.ValuePreservationExpression valuePreservationExpression:
                valuePreservationExpression.Expression.Dump(writer);
                break;
            default:
                Debug.Assert(false, $"Dumping {expression.GetType().Name} not implemented");
                break;
        }
    }
    public static void Dump(this Ir.Expressions.Values.IValue expression, TextWriter writer)
    {
        switch (expression)
        {
            case Ir.Expressions.Values.LValueLocalVariable localValueVariable:
                if (localValueVariable.Definition is { })
                {
                    writer.Write(localValueVariable.Definition.ToString());
                }
                else
                {
                    writer.Write($"<var #{localValueVariable.VarIndex}>");
                }

                break;
            default:
                Debug.Assert(false, $"Dumping {expression.GetType().Name} not implemented");
                break;
        }
    }
    public static void Dump(this Ir.Expressions.Constants.IConstant expression, TextWriter writer)
    {
        switch (expression)
        {
            case Ir.Expressions.Constants.IntegerConstant integerConstant:
                writer.Write(integerConstant.Value.ToString());

                break;
            default:
                Debug.Assert(false, $"Dumping {expression.GetType().Name} not implemented");
                break;
        }
    }
    public static void Dump(this Ir.Expressions.BinaryOperators.BinaryOperator expression, TextWriter writer)
    {
        var operatorString = expression switch
        {
            Ir.Expressions.BinaryOperators.BinaryOperator.Add => "+",
            Ir.Expressions.BinaryOperators.BinaryOperator.EqualTo => "==",
            _ => throw new InvalidOperationException($"Dumping BinaryOperator.{expression} not implemented"),
        };
        writer.Write(operatorString);
    }
}
