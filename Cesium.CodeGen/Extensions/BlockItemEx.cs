// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.Core;
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
}
