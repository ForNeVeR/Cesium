using System.Collections.Immutable;
using Cesium.Ast;
using Yoakke.SynKit.C.Syntax;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Parser.Attributes;

namespace Cesium.Parser;

public partial class CParser
{
    [Rule("declaration_specifiers: cli_import_specifier declaration_specifiers")]
    private static ImmutableArray<IDeclarationSpecifier> MakeDeclarationSpecifiers(
        CliImportSpecifier clrImport,
        ImmutableArray<IDeclarationSpecifier> rest) => rest.Insert(0, clrImport);

    [Rule("cli_import_specifier: '__cli_import' '(' StringLiteral ')'")]
    private static CliImportSpecifier MakeClrImport(IToken _, IToken __, IToken<CTokenType> symbolName, IToken ___) =>
        new(symbolName.UnwrapStringLiteral());
}
