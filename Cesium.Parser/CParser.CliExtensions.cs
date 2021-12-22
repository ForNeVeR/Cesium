using System.Collections.Immutable;
using Cesium.Ast;
using Yoakke.C.Syntax;
using Yoakke.Lexer;
using Yoakke.Parser.Attributes;

namespace Cesium.Parser;

public partial class CParser
{
    [Rule("declaration_specifiers: cli_import_specifier declaration_specifiers")]
    private static ImmutableArray<DeclarationSpecifier> MakeDeclarationSpecifiers(
        CliImportSpecifier clrImport,
        ImmutableArray<DeclarationSpecifier> rest) => rest.Insert(0, clrImport);

    [Rule("cli_import_specifier: '__cli_import' '(' StringLiteral ')'")]
    private static CliImportSpecifier MakeClrImport(IToken _, IToken __, IToken<CTokenType> symbolName, IToken ___) =>
        new(symbolName.UnwrapStringLiteral());
}
