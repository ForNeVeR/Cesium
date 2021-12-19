using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Yoakke.C.Syntax;
using Yoakke.Lexer;
using Yoakke.Parser.Attributes;

namespace Cesium.Parser;

using ICToken = IToken<CTokenType>;

using BlockItemList = ImmutableArray<IBlockItem>;
using DeclarationSpecifiers = ImmutableArray<DeclarationSpecifier>;
using IdentifierList = ImmutableArray<string>;
using InitDeclaratorList = ImmutableArray<InitDeclarator>;
using ParameterList = ImmutableArray<ParameterDeclaration>;
using TypeQualifierList = ImmutableArray<TypeQualifier>;

/// <remarks>See the section 6 of the C17 standard.</remarks>
[Parser(typeof(CTokenType))]
[SuppressMessage("ReSharper", "UnusedParameter.Local")] // parser parameters are mandatory even if unused
public partial class CParser
{
    // TODO: 6.5 Expressions
    // TODO: 6.5.1 Primary expressions
    // TODO: 6.5.2 Postfix operators
    // TODO: 6.5.3 Unary operators
    // TODO: 6.5.4 Cast operators
    // TODO: 6.5.5 Multiplicative operators
    // TODO: 6.5.6 Additive operators
    // TODO: 6.5.7 Bitwise shift operators
    // TODO: 6.5.8 Relational operators
    // TODO: 6.5.9 Equality operators
    // TODO: 6.5.10 Bitwise AND operator
    // TODO: 6.5.11 Bitwise exclusive OR operator
    // TODO: 6.5.12 Bitwise inclusive OR operator
    // TODO: 6.5.13 Logical AND operator
    // TODO: 6.5.14 Logical OR operator
    // TODO: 6.5.15 Conditional operator
    // TODO: 6.5.16 Assignment operators
    // TODO: 6.5.17 Comma operator

    // TODO: 6.6 Constant expressions

    // 6.7 Declarations
    [Rule("declaration: declaration_specifiers init_declarator_list?")]
    private static Declaration MakeDeclaration(
        DeclarationSpecifiers specifiers,
        InitDeclaratorList? initDeclarators) => new(specifiers, initDeclarators);

    // TODO: [Rule("declaration_specifiers: storage_class_specifier declaration_specifiers?")]
    [Rule("declaration_specifiers: type_specifier declaration_specifiers?")]
    // TODO: [Rule("declaration_specifiers: type_qualifier declaration_specifiers?")]
    // TODO: [Rule("declaration_specifiers: function_specifier declaration_specifiers?")]
    // TODO: [Rule("declaration_specifiers: alignment_specifier declaration_specifiers?")]
    private static DeclarationSpecifiers MakeDeclarationSpecifiers(
        TypeSpecifier typeSpecifier,
        DeclarationSpecifiers? rest)
    {
        var specifiers = ImmutableArray.Create((DeclarationSpecifier)typeSpecifier);
        return rest == null ? specifiers : specifiers.Concat(rest.Value).ToImmutableArray();
    }

    [Rule("init_declarator_list: init_declarator")]
    private static InitDeclaratorList MakeInitDeclaratorList(InitDeclarator declarator) =>
        ImmutableArray.Create(declarator);

    [Rule("init_declarator_list: init_declarator_list ',' init_declarator")]
    private static InitDeclaratorList MakeInitDeclaratorList(InitDeclaratorList prev, ICToken _, InitDeclarator newDeclarator) =>
        prev.Add(newDeclarator);

    [Rule("init_declarator: declarator")]
    private static InitDeclarator MakeInitDeclarator(Declarator declarator) => new(declarator);

    // TODO: [Rule("init_declarator: declarator = initializer")]
    // private static InitDeclarationList MakeInitDeclarator(Declarator declarator, Initializer initializer) =>
        // new InitDeclarator(declarator, initializer);

    // TODO: 6.7.1 Storage_class specifiers

    // 6.7.2 Type specifiers
    [Rule("type_specifier: 'void'")]
    [Rule("type_specifier: 'char'")]
    [Rule("type_specifier: 'short'")]
    [Rule("type_specifier: 'int'")]
    [Rule("type_specifier: 'long'")]
    [Rule("type_specifier: 'float'")]
    [Rule("type_specifier: 'double'")]
    [Rule("type_specifier: 'signed'")]
    [Rule("type_specifier: 'unsigned'")]
    [Rule("type_specifier: '_Bool'")]
    [Rule("type_specifier: '_Complex'")]
    // TODO: [Rule("type_specifier: atomic_type_specifier")]
    // TODO: [Rule("type_specifier: struct_or_union_specifier")]
    // TODO: [Rule("type_specifier: enum_specifier")]
    // TODO: [Rule("type_specifier: typedef_name")]
    private static TypeSpecifier MakeTypeSpecifier(ICToken specifier) => new(specifier.Text);

    // 6.7.3 Type qualifiers
    [Rule("type_qualifier: 'const'")]
    [Rule("type_qualifier: 'restrict'")]
    [Rule("type_qualifier: 'volatile'")]
    [Rule("type_qualifier: '_Atomic'")]
    private static TypeQualifier MakeTypeQualifier(ICToken name) => new(name.Text);

    // TODO: 6.7.4 Function specifiers
    // TODO: 6.7.5 Alignment specifier

    // 6.7.6 Declarators
    [Rule("declarator: pointer? direct_declarator")]
    private static Declarator MakeDeclarator(Pointer? pointer, DirectDeclarator directDeclarator) =>
        new(pointer, directDeclarator);

    [Rule("direct_declarator: Identifier")]
    private static DirectDeclarator MakeDirectDeclarator(ICToken identifier) =>
        new(identifier.Text);

    // TODO: direct_declarator: ( declarator )
    // TODO: direct_declarator: direct_declarator [ type_qualifier_list? assignment_expression? ]
    // TODO: direct_declarator: direct_declarator [ static type_qualifier_list? assignment_expression ]
    // TODO: direct_declarator: direct_declarator [ type_qualifier_list static assignment_expression ]
    // TODO: direct_declarator: direct_declarator [ type_qualifier_list? * ]

    [Rule("direct_declarator: direct_declarator '(' parameter_type_list ')'")]
    private static DirectDeclarator MakeDirectDeclarator(
        DirectDeclarator declarator,
        ICToken _,
        ParameterTypeList parameterList,
        ICToken __)
    {
        if (declarator.ParameterList != null)
            throw new Exception(
                $"Cannot add a parameter list {parameterList} to a declarator {declarator} which already has a parameter list.");
        return declarator with { ParameterList = parameterList };
    }

    [Rule("direct_declarator: direct_declarator '(' identifier_list? ')'")]
    private static DirectDeclarator MakeDirectDeclarator(
        DirectDeclarator declarator,
        ICToken _,
        IdentifierList? identifierList,
        ICToken __)
    {
        if (declarator.IdentifierList != null)
            throw new Exception(
                $"Cannot add an identifier list {identifierList} to a declarator {declarator} which already has an identifier list.");

        return declarator with { IdentifierList = identifierList };
    }

    [Rule("pointer: '*' type_qualifier_list?")]
    private static Pointer MakePointer(ICToken _, TypeQualifierList? typeQualifiers) => new(typeQualifiers);

    [Rule("pointer: '*' type_qualifier_list? pointer")]
    private static Pointer MakePointer(ICToken _, TypeQualifierList? typeQualifiers, Pointer pointer) =>
        new(typeQualifiers, pointer);

    [Rule("type_qualifier_list: type_qualifier")]
    private static TypeQualifierList MakeTypeQualifierList(TypeQualifier qualifier) => ImmutableArray.Create(qualifier);

    [Rule("type_qualifier_list: type_qualifier_list type_qualifier")]
    private static TypeQualifierList MakeTypeQualifierList(TypeQualifierList prev, TypeQualifier qualifier) =>
        prev.Add(qualifier);

    [Rule("parameter_type_list: parameter_list")]
    private static ParameterTypeList MakeParameterTypeList(ParameterList parameters) => new(parameters);

    [Rule("parameter_type_list: parameter_list ',' '...'")]
    private static ParameterTypeList MakeParameterTypeList(ParameterList parameters, ICToken _, ICToken __) =>
        new(parameters, true);

    [Rule("parameter_list: parameter_declaration")]
    private static ParameterList MakeParameterList(ParameterDeclaration declaration) =>
        ImmutableArray.Create(declaration);

    [Rule("parameter_list: parameter_list ',' parameter_declaration")]
    private static ParameterList MakeParameterList(ParameterList prev, ICToken _, ParameterDeclaration declaration) =>
        prev.Add(declaration);

    [Rule("parameter_declaration: declaration_specifiers declarator")]
    private static ParameterDeclaration MakeParameterTypeList(
        DeclarationSpecifiers specifiers,
        Declarator declarator) => new(specifiers, declarator);

    // TODO: parameter_declaration: declaration_specifiers abstract_declarator?

    [Rule("identifier_list: Identifier")]
    private static IdentifierList MakeIdentifierList(ICToken identifier) => ImmutableArray.Create(identifier.Text);

    [Rule("identifier_list: identifier_list ',' Identifier")]
    private static IdentifierList MakeIdentifierList(IdentifierList prev, ICToken _, ICToken identifier) =>
        prev.Add(identifier.Text);

    // TODO: 6.7.7 Type names
    // TODO: 6.7.8 Type definitions
    // TODO: 6.7.9 Initialization
    // TODO: 6.7.10 Static assertions

    // 6.8 Statements and blocks
    // TODO: [Rule("statement: labeled_statement")]
    [Rule("statement: compound_statement")]
    // TODO: [Rule("statement: expression_statement")]
    // TODO: [Rule("statement: selection_statement")]
    // TODO: [Rule("statement: iteration_statement")]
    // TODO: [Rule("statement: jump_statement")]
    private static Statement MakeStatement(CompoundStatement statement) => statement;

    // TODO: 6.8.1 Labeled statements
    // 6.8.2 Compound statement
    [Rule("compound_statement: '{' block_item_list? '}'")]
    private static CompoundStatement MakeCompoundStatement(ICToken _, BlockItemList? block, ICToken __) =>
        new(block ?? BlockItemList.Empty);

    [Rule("block_item_list: block_item")]
    private static BlockItemList MakeBlockItemList(IBlockItem item) => ImmutableArray.Create(item);

    [Rule("block_item_list: block_item_list block_item")]
    private static BlockItemList MakeBlockItemList(BlockItemList prev, IBlockItem item) => prev.Add(item);

    [Rule("block_item: declaration")]
    private static IBlockItem MakeBlockItem(Declaration declaration) => declaration;

    // TODO: [Rule("block_item: statement")]
    // private static IBlockItem MakeBlockItem(Statement statement) => statement;

    // TODO: 6.8.3 Expression and null statements
    // TODO: 6.8.4 Selection statements
    // TODO: 6.8.5 Iteration statements
    // TODO: 6.8.6 Jump statements

    /// 6.9 External definitions
    [Rule("translation_unit: external_declaration")]
    private static TranslationUnit MakeTranslationUnit(ExternalDeclaration declaration) =>
        new(ImmutableArray.Create(declaration));

    [Rule("translation_unit: translation_unit external_declaration")]
    private static TranslationUnit MakeTranslationUnit(TranslationUnit init, ExternalDeclaration newDeclaration) =>
        new(init.Declarations.Add(newDeclaration));

    [Rule("external_declaration: function_definition")]
    // TODO: [Rule("external_declaration: declaration")]
    private static ExternalDeclaration MakeExternalDeclaration(FunctionDefinition function) => function;

    // 6.9.1 Function definitions
    [Rule("function_definition: declaration_specifiers declarator declaration_list? compound_statement")]
    private static FunctionDefinition MakeFunctionDefinition(
        DeclarationSpecifiers specifiers,
        Declarator declarator,
        ImmutableArray<Declaration>? declarationList,
        CompoundStatement statement) => new(specifiers, declarator, declarationList, statement);

    [Rule("declaration_list: declaration")]
    private static ImmutableArray<Declaration> MakeDeclarationList(Declaration declaration) =>
        ImmutableArray.Create(declaration);

    [Rule("declaration_list: declaration_list declaration")]
    private static ImmutableArray<Declaration> MakeDeclarationList(
        ImmutableArray<Declaration> declarations,
        Declaration newDeclaration) => declarations.Add(newDeclaration);

    // TODO: 6.9.2 External object definitions

    // TODO: 6.10 Preprocessing directives
    // TODO: 6.10.1 Conditional inclusion
    // TODO: 6.10.2 Source file inclusion
    // TODO: 6.10.3 Macro replacement
    // TODO: 6.10.4 Line control
    // TODO: 6.10.5 Error directive
    // TODO: 6.10.6 Pragma directive
    // TODO: 6.10.7 Null directive
    // TODO: 6.10.8 Predefined macro names
    // TODO: 6.10.9 Pragma operator

    // TODO: 6.11 Future language directions
    // TODO: 6.11.1 Floating types
    // TODO: 6.11.2 Linkages of identifiers
    // TODO: 6.11.3 External names
    // TODO: 6.11.4 Character escape sequences
    // TODO: 6.11.5 Storage-class specifiers
    // TODO: 6.11.6 Function declarators
    // TODO: 6.11.7 Function definitions
    // TODO: 6.11.8 Pragma directives
    // TODO: 6.11.9 Predefined macro names
}
