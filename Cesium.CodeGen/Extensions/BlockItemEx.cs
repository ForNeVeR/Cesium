// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.Core;

namespace Cesium.CodeGen.Extensions;

internal static class BlockItemEx
{
    public static IBlockItem ToIntermediate(this Ast.IBlockItem blockItem, IDeclarationScope scope) => blockItem switch
    {
        Ast.Declaration d => ToIntermediate(d, scope),
        Ast.CompoundStatement s => new CompoundStatement(s, scope),
        Ast.LabelStatement s => new LabelStatement(s, scope),
        Ast.ReturnStatement s => new ReturnStatement(s, scope),
        Ast.ExpressionStatement s => new ExpressionStatement(s, scope),
        Ast.IfElseStatement s => new IfElseStatement(s, scope),
        Ast.ForStatement s => new ForStatement(s, scope),
        Ast.WhileStatement s => new WhileStatement(s, scope),
        Ast.DoWhileStatement s => new DoWhileStatement(s, scope),
        Ast.SwitchStatement s => new SwitchStatement(s, scope),
        Ast.CaseStatement s => new CaseStatement(s, scope),
        Ast.BreakStatement => new BreakStatement(),
        Ast.ContinueStatement => new ContinueStatement(),
        Ast.GoToStatement s => new GoToStatement(s),
        Ast.AmbiguousBlockItem a => new AmbiguousBlockItem(a),
        _ => throw new WipException(206, $"Statement not supported, yet: {blockItem}.")
    };

    private static IBlockItem ToIntermediate(Ast.Declaration d, IDeclarationScope scope)
    {
        return new CompoundStatement(IScopedDeclarationInfo.Of(d, scope).Select(_ => _ switch
        {
            ScopedIdentifierDeclaration declaration => (IBlockItem)new DeclarationBlockItem(declaration),
            TypeDefDeclaration typeDefDeclaration => new TypeDefBlockItem(typeDefDeclaration),
            _ => throw new WipException(212, $"Unknown kind of declaration: {d}."),
        }).ToList(), null)
        { InheritScope = true };
    }
}
