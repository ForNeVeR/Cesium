// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.ControlFlow;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
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
                writer.WriteLine($"{indent}}}");
                break;
            case BasicBlock bb:
                writer.WriteLine($"{indent} // bb");
                foreach (var item in bb.Statements)
                {
                    Dump(item, writer, indentLevel);
                }
                break;
            case Ir.BlockItems.ExpressionStatement expressionStatement:
                writer.Write(indent);
                expressionStatement.Expression?.Dump(writer);
                writer.WriteLine(";");
                break;
            case Ir.BlockItems.IfElseStatement ifElseStatement:
                writer.Write($"{indent}if (");
                ifElseStatement.Expression?.Dump(writer);
                writer.WriteLine(")");
                ifElseStatement.TrueBranch.Dump(writer, indentLevel + 1);
                if (ifElseStatement.FalseBranch is { } falseBranch)
                {
                    writer.WriteLine($"{indent}else");
                    falseBranch.Dump(writer, indentLevel + 1);
                }
                break;
            case Ir.BlockItems.GoToStatement gotoStatement:
                writer.Write($"{indent}goto ");
                writer.Write(gotoStatement.Identifier);
                writer.WriteLine(";");
                break;
            case Ir.BlockItems.LabelStatement labelStatement:
                writer.WriteLine($"{indent}{labelStatement.Identifier}:");
                labelStatement.Expression.Dump(writer, indentLevel);
                break;
            case Ir.BlockItems.ReturnStatement returnStatement:
                writer.Write($"{indent}return ");
                returnStatement.Expression?.Dump(writer);
                writer.WriteLine(";");
                break;
            case Ir.BlockItems.ConditionalGotoStatement gotoStatement:
                writer.Write($"{indent}if /*cgoto*/ (");
                if (gotoStatement.JumpType == ConditionalJumpType.False)
                {
                    writer.Write("!");
                }
                gotoStatement.Condition.Dump(writer);
                writer.Write(") goto ");
                writer.Write(gotoStatement.Identifier);
                writer.WriteLine(";");
                break;
            case Ir.BlockItems.LabeledNopStatement labelStatement:
                writer.WriteLine($"{indent}{labelStatement.Label}: /*label nop*/ ");
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
            case Ir.Expressions.UnaryOperatorExpression unaryExpression:
                unaryExpression.Operator.Dump(writer);
                unaryExpression.Target.Dump(writer);
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
            case Ir.Expressions.LocalAllocationExpression localAllocationExpression:
                localAllocationExpression.ArrayType.Dump(writer);
                break;
            case Ir.Expressions.GetAddressValueExpression getAddressValueExpression:
                writer.Write($"&");
                getAddressValueExpression.Value.Dump(writer);
                break;
            case Ir.Expressions.IdentifierExpression identifierExpression:
                writer.Write(identifierExpression.Identifier);
                break;
            case Ir.Expressions.FunctionCallExpression functionCallExpression:
                functionCallExpression.Function.Dump(writer);
                writer.Write($"(");
                for (var i = 0;i < functionCallExpression.Arguments.Count;i++)
                {
                    if (i > 0)
                    {
                        writer.Write(", ");
                    }
                    functionCallExpression.Arguments[i].Dump(writer);
                }
                writer.Write($")");
                break;
            case Ir.Expressions.CompoundInitializationExpression compoundInitializationExpression:
                compoundInitializationExpression.ArrayInitializer.Dump(writer);
                break;
            case Ir.Expressions.ArrayInitializerExpression arrayInitializerExpression:
                writer.Write("{");
                for (var i = 0; i < arrayInitializerExpression.Initializers.Length; i++)
                {
                    if (i > 0)
                    {
                        writer.Write(", ");
                    }
                    arrayInitializerExpression.Initializers[i]?.Dump(writer);
                }
                writer.Write("}");
                break;
            case Ir.Expressions.CompoundObjectInitializationExpression compoundObjectInitializationExpression:
                writer.Write("{");
                for (var i = 0; i < compoundObjectInitializationExpression.Initializers.Length; i++)
                {
                    if (i > 0)
                    {
                        writer.Write(", ");
                    }
                    compoundObjectInitializationExpression.Initializers[i]?.Dump(writer);
                }
                writer.Write("}");
                break;
            case Ir.Expressions.CompoundObjectFieldInitializer compoundObjectFieldInitializer:
                writer.Write("{");
                foreach (var d in compoundObjectFieldInitializer.Designation.Designators)
                {
                    d.Dump(writer);
                    writer.Write(" = ");
                }

                compoundObjectFieldInitializer.Inner.Dump(writer);
                writer.Write("}");
                break;
            case Ir.Expressions.TypeCastExpression typeCastExpression:
                writer.Write("(");
                writer.Write(typeCastExpression.TargetType.ToString());
                writer.Write(")");
                typeCastExpression.Expression.Dump(writer);
                break;
            case Ir.Expressions.ConsumeExpression consumeExpression:
                consumeExpression.Expression.Dump(writer);
                break;
            case Ir.Expressions.SizeOfOperatorExpression sizeOfOperatorExpression:
                writer.Write("sizeof(");
                writer.Write(sizeOfOperatorExpression.Type.ToString());
                writer.Write(")");
                break;
            case Ir.Expressions.IndirectFunctionCallExpression indirectFunctionCallExpression:
                indirectFunctionCallExpression.Callee.Dump(writer);
                writer.Write($"(");
                for (var i = 0; i < indirectFunctionCallExpression.Arguments.Count; i++)
                {
                    if (i > 0)
                    {
                        writer.Write(", ");
                    }
                    indirectFunctionCallExpression.Arguments[i].Dump(writer);
                }
                writer.Write($")");
                break;
            case Ir.Expressions.CompoundInitializationFunctionCallExpression compoundRuntimeInitializationExpression:
                writer.Write($"InitializeCompound(");
                compoundRuntimeInitializationExpression.Source.Dump(writer);
                writer.Write(", ");
                compoundRuntimeInitializationExpression.Target.Dump(writer);
                writer.Write(", ");
                compoundRuntimeInitializationExpression.Size.Dump(writer);
                writer.Write($")");
                break;
            case Ir.Expressions.CommaExpression commaExpression:
                writer.Write("(");
                writer.Write(commaExpression.Left.ToString());
                writer.Write(",");
                writer.Write(commaExpression.Right.ToString());
                writer.Write(")");
                break;
            case Ir.Expressions.ConditionalExpression conditionalExpression:
                conditionalExpression.Condition.Dump(writer);
                writer.Write("?");
                conditionalExpression.TrueExpression.Dump(writer);
                writer.Write(":");
                conditionalExpression.FalseExpression.Dump(writer);
                break;
            case Ir.Expressions.InstanceForOffsetOfExpression:
                // Do nothing for this expression/value
                break;
            case Ir.Expressions.SubscriptingExpression subscriptingExpression:
                subscriptingExpression.Expression.Dump(writer);
                writer.Write("[");
                subscriptingExpression.Index.Dump(writer);
                writer.Write("]");
                break;
            default:
                Debug.Assert(false, $"Dumping {expression.GetType().Name} not implemented");
                break;
        }
    }
    public static void Dump(this Ir.Types.IType type, TextWriter writer)
    {
        if (type is InPlaceArrayType inPlaceArrayType)
        {
            inPlaceArrayType.Base.Dump(writer);
            writer.Write($"[{inPlaceArrayType.Size}]");
        }
        else if (type is PrimitiveType primitiveType)
        {
            writer.Write(primitiveType.Kind.ToString());
        }
        else 
        {
            writer.Write(type.ToString());
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
            case Ir.Expressions.Values.LValueArrayElement localArrayVariable:
                localArrayVariable.Array.Dump(writer);
                writer.Write("[");
                localArrayVariable.Index.Dump(writer);
                writer.Write("]");
                break;
            case Ir.Expressions.Values.LValueArrayElementAddress localArrayAddressVariable:
                writer.Write("&");
                localArrayAddressVariable.Array.Dump(writer);
                writer.Write("[");
                localArrayAddressVariable.Index.Dump(writer);
                writer.Write("]");
                break;
            case Ir.Expressions.Values.LValueGlobalVariable localGlobalVariable:
                writer.Write($"{localGlobalVariable.Name}");
                break;
            case Ir.Expressions.Values.LValueIndirection localIndirection:
                writer.Write($"*");
                localIndirection.PointerExpression.Dump(writer);
                break;
            case Ir.Expressions.Values.LValueParameter parameter:
                writer.Write(parameter.ParameterInfo.Name);
                break;
            case Ir.Expressions.Values.LValueInstanceField instanceField:
                writer.Write("(");
                instanceField.Expression.Dump(writer);
                writer.Write(").");
                writer.Write(instanceField.Name);
                break;
            case Ir.Expressions.Values.FunctionValue functionValue:
                writer.Write(functionValue.FunctionInfo.Identifier);
                break;
            case Ir.Expressions.InstanceForOffsetOfExpression:
                // Do nothing for this expression/value
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
            case Ir.Expressions.Constants.FloatingPointConstant floatingPointConstant:
                writer.Write(floatingPointConstant.Value.ToString());
                break;
            case Ir.Expressions.Constants.StringConstant stringConstant:
                writer.Write("\"");
                writer.Write(stringConstant.Value);
                writer.Write("\"");
                break;
            case Ir.Expressions.Constants.CharConstant charConstant:
                writer.Write("'");
                writer.Write(charConstant.Value);
                writer.Write("'");
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
            Ir.Expressions.BinaryOperators.BinaryOperator.Subtract => "-",
            Ir.Expressions.BinaryOperators.BinaryOperator.Multiply => "*",
            Ir.Expressions.BinaryOperators.BinaryOperator.Divide => "/",
            Ir.Expressions.BinaryOperators.BinaryOperator.Remainder => "%",

            Ir.Expressions.BinaryOperators.BinaryOperator.BitwiseLeftShift => "<<",
            Ir.Expressions.BinaryOperators.BinaryOperator.BitwiseRightShift => ">>",
            Ir.Expressions.BinaryOperators.BinaryOperator.BitwiseOr => "|",
            Ir.Expressions.BinaryOperators.BinaryOperator.BitwiseAnd => "&",
            Ir.Expressions.BinaryOperators.BinaryOperator.BitwiseXor => "^",

            Ir.Expressions.BinaryOperators.BinaryOperator.EqualTo => "==",
            Ir.Expressions.BinaryOperators.BinaryOperator.NotEqualTo => "!=",
            Ir.Expressions.BinaryOperators.BinaryOperator.LessThan => "<",
            Ir.Expressions.BinaryOperators.BinaryOperator.LessThanOrEqualTo => "<=",
            Ir.Expressions.BinaryOperators.BinaryOperator.GreaterThan => ">",
            Ir.Expressions.BinaryOperators.BinaryOperator.GreaterThanOrEqualTo => ">=",

            Ir.Expressions.BinaryOperators.BinaryOperator.LogicalOr => "||",
            Ir.Expressions.BinaryOperators.BinaryOperator.LogicalAnd => "&&",
            _ => throw new InvalidOperationException($"Dumping BinaryOperator.{expression} not implemented"),
        };
        writer.Write(operatorString);
    }
    public static void Dump(this Ir.Expressions.UnaryOperator expression, TextWriter writer)
    {
        var operatorString = expression switch
        {
            Ir.Expressions.UnaryOperator.Negation => "-",
            Ir.Expressions.UnaryOperator.Promotion => "+",
            Ir.Expressions.UnaryOperator.BitwiseNot => "~",
            Ir.Expressions.UnaryOperator.LogicalNot => "!",
            Ir.Expressions.UnaryOperator.AddressOf => "&",
            Ir.Expressions.UnaryOperator.Indirection => "*",
            _ => throw new InvalidOperationException($"Dumping UnaryOperator.{expression} not implemented"),
        };
        writer.Write(operatorString);
    }
    public static void Dump(this Ast.Designator expression, TextWriter writer)
    {
        switch (expression)
        {
            case Ast.IdentifierDesignator identifier:
                writer.Write(identifier.FieldName);
                break;
            default:
                Debug.Assert(false, $"Dumping {expression.GetType().Name} not implemented");
                break;
        }
    }
}
