using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Cesium.Ast;
using Yoakke.C.Syntax;
using Yoakke.Lexer;
using Yoakke.Parser.Attributes;

namespace Cesium.Parser;

using ICToken = IToken<CTokenType>;

using ArgumentExpressionList = ImmutableArray<Expression>;
using BlockItemList = ImmutableArray<IBlockItem>;
using DeclarationSpecifiers = ImmutableArray<IDeclarationSpecifier>;
using IdentifierList = ImmutableArray<string>;
using InitDeclaratorList = ImmutableArray<InitDeclarator>;
using ParameterList = ImmutableArray<ParameterDeclaration>;
using SpecifierQualifierList = ImmutableArray<ISpecifierQualifierListItem>;
using StructDeclarationList = ImmutableArray<StructDeclaration>;
using StructDeclaratorList = ImmutableArray<StructDeclarator>;
using TypeQualifierList = ImmutableArray<TypeQualifier>;

/// <remarks>See the section 6 of the C17 standard.</remarks>
[Parser(typeof(CTokenType))]
[SuppressMessage("ReSharper", "UnusedParameter.Local")] // parser parameters are mandatory even if unused
public partial class CParser
{
    // 6.4.4 Constants
    [Rule("constant: IntLiteral")]
    [Rule("constant: FloatLiteral")]
    [Rule("constant: enumeration_constant")]
    [Rule("constant: CharLiteral")]
    private static ICToken MakeConstant(ICToken constant) => constant;

    // 6.4.4.3 Enumeration constants
    [Rule("enumeration_constant: Identifier")]
    private static ICToken MakeEnumerationConstant(ICToken identifier) => identifier;

    // 6.5 Expressions

    [Rule("postfix_expression: primary_expression")] // 6.5.2 Postfix operators
    [Rule("unary_expression: postfix_expression")] // 6.5.3 Unary operators
    [Rule("cast_expression: unary_expression")] // 6.5.4 Cast operators
    [Rule("multiplicative_expression: cast_expression")] // 6.5.5 Multiplicative operators
    [Rule("additive_expression: multiplicative_expression")] // 6.5.6 Additive operators
    [Rule("shift_expression: additive_expression")] // 6.5.7 Bitwise shift operators
    [Rule("relational_expression: shift_expression")] // 6.5.8 Relational operators
    [Rule("equality_expression: relational_expression")] // 6.5.9 Equality operators
    [Rule("AND_expression: equality_expression")] // 6.5.10 Bitwise AND operator
    [Rule("exclusive_OR_expression: AND_expression")] // 6.5.11 Bitwise exclusive OR operator
    [Rule("inclusive_OR_expression: exclusive_OR_expression")] // 6.5.12 Bitwise inclusive OR operator
    [Rule("logical_AND_expression: inclusive_OR_expression")] // 6.5.13 Logical AND operator
    [Rule("logical_OR_expression: logical_AND_expression")] // 6.5.14 Logical OR operator
    [Rule("conditional_expression: logical_OR_expression")] // 6.5.15 Conditional operator
    [Rule("assignment_expression: conditional_expression")] // 6.5.16 Assignment operators
    [Rule("expression: assignment_expression")] // 6.5.17 Comma operator
    private static Expression CreateExpressionIdentity(Expression expression) => expression;

    // 6.5.1 Primary expressions
    [Rule("primary_expression: constant")]
    private static Expression MakeConstantExpression(ICToken constant) => new ConstantExpression(constant);

    [Rule("primary_expression: Identifier")]
    private static Expression MakeIdentifierExpression(IToken identifier) => new IdentifierExpression(identifier.Text);

    [Rule("primary_expression: StringLiteral")]
    private static Expression MakeStringLiteralExpression(ICToken stringLiteral) =>
        new ConstantExpression(stringLiteral);

    // TODO:
    // primary-expression:
    //     ( expression )
    //     generic-selection

    // 6.5.2 Postfix operators

    [Rule("postfix_expression: postfix_expression '[' expression ']'")]
    private static Expression MakeSubscriptingExpression(Expression @base, IToken _, Expression index, IToken __) =>
        new SubscriptingExpression(@base, index);

    [Rule("postfix_expression: postfix_expression '(' argument_expression_list? ')'")]
    private static Expression MakeFunctionCallExpression(
        Expression function,
        IToken _,
        ArgumentExpressionList? arguments,
        IToken __) => new FunctionCallExpression(function, arguments);

    // TODO:
    // postfix-expression:
    //     postfix-expression . identifier
    //     postfix-expression -> identifier
    //     postfix-expression ++
    //     postfix-expression -
    //     ( type-name ) { initializer-list }
    //     ( type-name ) { initializer-list , }

    [Rule("argument_expression_list: assignment_expression")]
    private static ArgumentExpressionList MakeArgumentExpressionList(Expression expression) =>
        ImmutableArray.Create(expression);

    [Rule("argument_expression_list: argument_expression_list ',' assignment_expression")]
    private static ArgumentExpressionList MakeArgumentExpressionList(
        ArgumentExpressionList prev,
        IToken _,
        Expression expression) => prev.Add(expression);

    // TODO: 6.5.3 Unary operators
    // unary-expression:
    //    postfix-expression
    //    ++ unary-expression
    //    unary-operator cast-expression
    //    sizeof unary-expression
    //    sizeof ( type-name )
    //    _Alignof ( type-name )
    // unary-operator: one of
    //    & * + - ˜ !
    [Rule("unary_expression: '-' unary_expression")]
    private static Expression MakeNegationExpression(ICToken _, Expression target) =>
        new NegationExpression(target);

    [Rule("unary_expression: '++' unary_expression")]
    private static Expression MakePrefixIncrementExpression(ICToken _, Expression target) =>
        new PrefixIncrementExpression(target);

    // TODO: 6.5.4 Cast operators

    // 6.5.5 Multiplicative operators
    [Rule("multiplicative_expression: multiplicative_expression '*' cast_expression")]
    [Rule("multiplicative_expression: multiplicative_expression '/' cast_expression")]
    [Rule("multiplicative_expression: multiplicative_expression '%' cast_expression")]
    private static Expression MakeMultiplicativeExpression(Expression a, ICToken @operator, Expression b) =>
        new BinaryOperatorExpression(a, @operator.Text, b);

    // 6.5.6 Additive operators
    [Rule("additive_expression: additive_expression '+' multiplicative_expression")]
    [Rule("additive_expression: additive_expression '-' multiplicative_expression")]
    private static Expression MakeAdditiveExpression(Expression a, ICToken @operator, Expression b) =>
        new BinaryOperatorExpression(a, @operator.Text, b);

    // TODO: 6.5.7 Bitwise shift operators
    // TODO: 6.5.8 Relational operators
    // TODO: 6.5.9 Equality operators
    // TODO: 6.5.10 Bitwise AND operator
    // TODO: 6.5.11 Bitwise exclusive OR operator
    // TODO: 6.5.12 Bitwise inclusive OR operator
    // TODO: 6.5.13 Logical AND operator
    // TODO: 6.5.14 Logical OR operator
    // TODO: 6.5.15 Conditional operator

    // 6.5.16 Assignment operators
    [Rule("assignment_expression: unary_expression assignment_operator assignment_expression")]
    private static Expression MakeAssignmentExpression(
        Expression storage,
        IToken @operator,
        Expression value) => new AssignmentExpression(storage, @operator.Text, value);

    [Rule("assignment_operator: '='")]
    [Rule("assignment_operator: '*='")]
    [Rule("assignment_operator: '/='")]
    [Rule("assignment_operator: '%='")]
    [Rule("assignment_operator: '+='")]
    [Rule("assignment_operator: '-='")]
    [Rule("assignment_operator: '<<='")]
    [Rule("assignment_operator: '>>='")]
    [Rule("assignment_operator: '&='")]
    [Rule("assignment_operator: '^='")]
    [Rule("assignment_operator: '|='")]
    private static IToken MakeAssignmentOperator(IToken token) => token;

    // 6.5.17 Comma operator
    // TODO: [Rule("expression: expression ',' assignment_expression")]

    // TODO: 6.6 Constant expressions

    // 6.7 Declarations
    [Rule("declaration: declaration_specifiers init_declarator_list? ';'")]
    private static Declaration MakeDeclaration(
        DeclarationSpecifiers specifiers,
        InitDeclaratorList? initDeclarators,
        IToken _) => new(specifiers, initDeclarators);

    [Rule("declaration_specifiers: storage_class_specifier declaration_specifiers?")]
    [Rule("declaration_specifiers: type_specifier declaration_specifiers?")]
    [Rule("declaration_specifiers: type_qualifier declaration_specifiers?")]
    private static DeclarationSpecifiers MakeDeclarationSpecifiers(
        IDeclarationSpecifier storageClassSpecifier,
        DeclarationSpecifiers? rest) =>
        rest?.Insert(0, storageClassSpecifier) ?? ImmutableArray.Create(storageClassSpecifier);

    // TODO: [Rule("declaration_specifiers: function_specifier declaration_specifiers?")]
    // TODO: [Rule("declaration_specifiers: alignment_specifier declaration_specifiers?")]

    [Rule("init_declarator_list: init_declarator")]
    private static InitDeclaratorList MakeInitDeclaratorList(InitDeclarator declarator) =>
        ImmutableArray.Create(declarator);

    [Rule("init_declarator_list: init_declarator_list ',' init_declarator")]
    private static InitDeclaratorList MakeInitDeclaratorList(InitDeclaratorList prev, ICToken _, InitDeclarator newDeclarator) =>
        prev.Add(newDeclarator);

    [Rule("init_declarator: declarator")]
    private static InitDeclarator MakeInitDeclarator(Declarator declarator) => new(declarator);

    [Rule("init_declarator: declarator '=' initializer")]
    private static InitDeclarator MakeInitDeclarator(Declarator declarator, IToken _, Initializer initializer) =>
        new(declarator, initializer);

    // 6.7.1 Storage-class specifiers
    [Rule("storage_class_specifier: 'typedef'")]
    private static StorageClassSpecifier MakeStorageClassSpecifier(IToken keyword) => new(keyword.Text);

    // TODO:
    // storage-class-specifier:
    //     extern
    //     static
    //     _Thread_local
    //     auto
    //     register

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
    private static ITypeSpecifier MakeSimpleTypeSpecifier(ICToken specifier) => new SimpleTypeSpecifier(specifier.Text);

    // TODO: [Rule("type_specifier: atomic_type_specifier")]

    [Rule("type_specifier: struct_or_union_specifier")]
    private static ITypeSpecifier MakeComplexTypeSpecifier(StructOrUnionSpecifier structOrUnionSpecifier) =>
        structOrUnionSpecifier;

    // TODO: [Rule("type_specifier: enum_specifier")]
    // TODO: [Rule("type_specifier: typedef_name")]

    // 6.7.2.1 Structure and union specifiers

    [Rule("struct_or_union_specifier: struct_or_union Identifier? '{' struct_declaration_list '}'")]
    private static StructOrUnionSpecifier MakeStructOrUnionSpecifier(
        ComplexTypeKind structOrUnion,
        IToken? identifier,
        IToken _,
        StructDeclarationList structDeclarationList,
        IToken __) => new StructOrUnionSpecifier(structOrUnion, identifier?.Text, structDeclarationList);

    // TODO: struct-or-union-specifier: struct-or-union identifier

    [Rule("struct_or_union: 'struct'")]
    private static ComplexTypeKind MakeStructComplexTypeKind(IToken _) => ComplexTypeKind.Struct;
    // TODO: struct-or-union: union

    [Rule("struct_declaration_list: struct_declaration")]
    private static StructDeclarationList MakeStructDeclarationList(StructDeclaration structDeclaration) =>
        ImmutableArray.Create(structDeclaration);

    [Rule("struct_declaration_list: struct_declaration_list struct_declaration")]
    private static StructDeclarationList MakeStructDeclarationList(
        StructDeclarationList prev,
        StructDeclaration structDeclaration) => prev.Add(structDeclaration);

    [Rule("struct_declaration: specifier_qualifier_list struct_declarator_list? ';'")]
    private static StructDeclaration MakeStructDeclaration(
        SpecifierQualifierList specifiersQualifiers,
        StructDeclaratorList? structDeclarators,
        IToken _) => new(specifiersQualifiers, structDeclarators);

    // TODO: struct-declaration: static_assert-declaration

    [Rule("specifier_qualifier_list: type_specifier specifier_qualifier_list?")]
    [Rule("specifier_qualifier_list: type_qualifier specifier_qualifier_list?")]
    private SpecifierQualifierList MakeSpecifierQualifierList(
        ISpecifierQualifierListItem item,
        SpecifierQualifierList? rest) => rest?.Insert(0, item) ?? ImmutableArray.Create(item);

    // TODO: specifier-qualifier-list: alignment-specifier specifier-qualifier-list?

    [Rule("struct_declarator_list: struct_declarator")]
    private static StructDeclaratorList MakeStructDeclaratorList(StructDeclarator structDeclarator) =>
        ImmutableArray.Create(structDeclarator);

    [Rule("struct_declarator_list: struct_declarator_list ',' struct_declarator")]
    private static StructDeclaratorList MakeStructDeclaratorList(
        StructDeclaratorList prev,
        IToken _,
        StructDeclarator next) => prev.Add(next);

    [Rule("struct_declarator: declarator")]
    private static StructDeclarator MakeStructDeclarator(Declarator declarator) => new StructDeclarator(declarator);

    // TODO: struct-declarator: declarator? : constant-expression

    // TODO: 6.7.2.2 Enumeration specifiers
    // TODO: 6.7.2.3 Tags
    // TODO: 6.7.2.4 Atomic type specifiers

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
    private static Declarator MakeDeclarator(Pointer? pointer, IDirectDeclarator directDeclarator) =>
        new(pointer, directDeclarator);

    [Rule("direct_declarator: Identifier")]
    private static IDirectDeclarator MakeDirectDeclarator(ICToken identifier) =>
        new IdentifierDirectDeclarator(identifier.Text);

    // TODO: direct_declarator: ( declarator )

    [Rule("direct_declarator: direct_declarator '[' type_qualifier_list? assignment_expression? ']'")]
    private static IDirectDeclarator MakeDirectDeclarator(
        IDirectDeclarator @base,
        IToken _,
        TypeQualifierList? typeQualifiers,
        Expression? expression,
        IToken __) => new ArrayDirectDeclarator(@base, typeQualifiers, expression);
    // TODO: direct_declarator: direct_declarator [ static type_qualifier_list? assignment_expression ]
    // TODO: direct_declarator: direct_declarator [ type_qualifier_list static assignment_expression ]
    // TODO: direct_declarator: direct_declarator [ type_qualifier_list? * ]

    [Rule("direct_declarator: direct_declarator '(' parameter_type_list ')'")]
    private static IDirectDeclarator MakeDirectDeclarator(
        IDirectDeclarator @base,
        ICToken _,
        ParameterTypeList parameters,
        ICToken __) => new ParameterListDirectDeclarator(@base, parameters);

    [Rule("direct_declarator: direct_declarator '(' identifier_list? ')'")]
    private static IDirectDeclarator MakeDirectDeclarator(
        IDirectDeclarator @base,
        ICToken _,
        IdentifierList? identifierList,
        ICToken __) => new IdentifierListDirectDeclarator(@base, identifierList);

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

    [Rule("parameter_declaration: declaration_specifiers abstract_declarator?")]
    private static ParameterDeclaration MakeParameterTypeList(
        DeclarationSpecifiers specifiers,
        AbstractDeclarator? declarator) => new(specifiers, AbstractDeclarator: declarator);

    [Rule("identifier_list: Identifier")]
    private static IdentifierList MakeIdentifierList(ICToken identifier) => ImmutableArray.Create(identifier.Text);

    [Rule("identifier_list: identifier_list ',' Identifier")]
    private static IdentifierList MakeIdentifierList(IdentifierList prev, ICToken _, ICToken identifier) =>
        prev.Add(identifier.Text);

    // 6.7.7 Type names

    // TODO:
    // type-name:
    //     specifier-qualifier-list abstract-declarator?

    [Rule("abstract_declarator: pointer")]
    private static AbstractDeclarator MakeAbstractDeclarator(Pointer pointer) => new(pointer);

    [Rule("abstract_declarator: pointer? direct_abstract_declarator")]
    private static AbstractDeclarator MakeAbstractDeclarator(
        Pointer? pointer,
        IDirectAbstractDeclarator directAbstractDeclarator) => new(pointer, directAbstractDeclarator);

    [Rule("direct_abstract_declarator: '(' abstract_declarator ')'")]
    private static IDirectAbstractDeclarator MakeDirectAbstractDeclarator(
        IToken _,
        AbstractDeclarator abstractDeclarator,
        IToken __) => new SimpleDirectAbstractDeclarator(abstractDeclarator);

    // HACK:
    // The original rule in the C11 standard says the following:
    //     direct_abstract_declarator: direct_abstract_declarator? '[' type_qualifier_list? assignment_expression? ']'
    //
    // But here, it's impossible to apply it as-is due to an issue with the Yoakke parser library
    // (https://github.com/LanguageDev/Yoakke/issues/121). Thus, it has been split in two:
    //    direct_abstract_declarator: '[' type_qualifier_list? assignment_expression? ']'
    //    direct_abstract_declarator: direct_abstract_declarator '[' type_qualifier_list? assignment_expression? ']'
    [Rule("direct_abstract_declarator: '[' type_qualifier_list? assignment_expression? ']'")]
    private static IDirectAbstractDeclarator MakeDirectAbstractDeclarator(
        IToken _,
        TypeQualifierList? typeQualifierList,
        Expression? assignmentExpression,
        IToken __) => new ArrayDirectAbstractDeclarator(null, typeQualifierList, assignmentExpression);

    [Rule("direct_abstract_declarator: direct_abstract_declarator '[' type_qualifier_list? assignment_expression? ']'")]
    private static IDirectAbstractDeclarator MakeDirectAbstractDeclarator(
        IDirectAbstractDeclarator @base,
        IToken _,
        TypeQualifierList? typeQualifierList,
        Expression? assignmentExpression,
        IToken __) => new ArrayDirectAbstractDeclarator(@base, typeQualifierList, assignmentExpression);

    // TODO:
    // direct-abstract-declarator:
    //     direct-abstract-declarator? [ static type-qualifier-list? assignment-expression ]
    //     direct-abstract-declarator? [ type-qualifier-list static assignment-expression ]
    //     direct-abstract-declarator? [ * ]
    //     direct-abstract-declarator? ( parameter-type-list? )

    // TODO: 6.7.8 Type definitions

    // 6.7.9 Initialization

    [Rule("initializer: assignment_expression")]
    private static AssignmentInitializer MakeInitializer(Expression assignmentExpression) =>
        new(assignmentExpression);

    // TODO:
    // initializer:
    //     { initializer-list }
    //     { initializer-list , }
    // initializer-list:
    //     designation? initializer
    //     initializer-list , designation? initializer
    // designation:
    //     designator-list =
    // designator-list:
    //     designator
    //     designator-list designator
    // designator:
    //     [ constant-expression ]
    //     . identifier

    // TODO: 6.7.10 Static assertions

    // 6.8 Statements and blocks
    // TODO: [Rule("statement: labeled_statement")]
    [Rule("statement: compound_statement")]
    [Rule("statement: expression_statement")]
    // TODO: [Rule("statement: selection_statement")]
    // TODO: [Rule("statement: iteration_statement")]
    [Rule("statement: jump_statement")]
    private static Statement MakeStatementIdentity(Statement statement) => statement;

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
    [Rule("block_item: statement")]
    private static IBlockItem MakeBlockItem(IBlockItem declaration) => declaration;

    // 6.8.3 Expression and null statements
    [Rule("expression_statement: expression? ';'")]
    private static ExpressionStatement MakeExpressionStatement(Expression expression, IToken _) => new(expression);

    // TODO: 6.8.4 Selection statements
    // TODO: 6.8.5 Iteration statements

    // 6.8.6 Jump statements
    [Rule("jump_statement: 'goto' Identifier ';'")]
    private static Statement MakeGoToStatement(ICToken _, ICToken identifier, ICToken __) =>
        new GoToStatement(identifier.Text);

    // [Rule("jump_statement: 'continue' ';'")]
    // [Rule("jump_statement: 'break' ';'")]
    [Rule("jump_statement: 'return' expression? ';'")]
    private static Statement MakeReturnStatement(ICToken _, Expression expression, ICToken __) =>
        new ReturnStatement(expression);

    /// 6.9 External definitions
    [Rule("translation_unit: external_declaration")]
    private static TranslationUnit MakeTranslationUnit(ExternalDeclaration declaration) =>
        new(ImmutableArray.Create(declaration));

    [Rule("translation_unit: translation_unit external_declaration")]
    private static TranslationUnit MakeTranslationUnit(TranslationUnit init, ExternalDeclaration newDeclaration) =>
        new(init.Declarations.Add(newDeclaration));

    [Rule("external_declaration: function_definition")]
    private static ExternalDeclaration MakeExternalDeclaration(FunctionDefinition function) => function;

    [Rule("external_declaration: declaration")]
    private static ExternalDeclaration MakeExternalDeclaration(Declaration declaration) =>
        new SymbolDeclaration(declaration);

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

    // 6.10 Preprocessing directives

    // TODO:
    // preprocessing-file:
    //     group?
    // group:
    //     group-part
    //     group group-part
    // group-part:
    //     if-section
    //     control-line
    //     text-line
    //     # non-directive
    // if-section:
    //     if-group elif-groups? else-group? endif-line
    // if-group:
    //     # if constant-expression new-line group?
    //     # ifdef identifier new-line group?
    //     # ifndef identifier new-line group?
    // elif-groups:
    //     elif-group
    //     elif-groups elif-group
    // elif-group:
    //     # elif constant-expression new-line group?
    // else-group:
    //     # else new-line group?
    // endif-line:
    //     # endif new-line
    // control-line:
    //     # include pp-tokens new-line
    //     # define identifier replacement-list new-line
    //     # define identifier lparen identifier-list? ) replacement-list new-line
    //     # define identifier lparen ... ) replacement-list new-line
    //     # define identifier lparen identifier-list , ... ) replacement-list new-line
    //     # undef identifier new-line
    //     # line pp-tokens new-line
    //     # error pp-tokens? new-line
    //     # pragma pp-tokens? new-line
    //     # new-line
    // text-line:
    //     pp-tokens? new-line
    // non-directive:
    //     pp-tokens new-line
    // lparen:
    //     a ( character not immediately preceded by white space
    // replacement-list:
    //     pp-tokens?
    // pp-tokens:
    //     preprocessing-token
    //     pp-tokens preprocessing-token
    // new-line:
    //     the new-line character

    // TODO: 6.10.1 Conditional inclusion
    // TODO: 6.10.2 Source file inclusion
    // TODO: 6.10.3 Macro replacement
    // TODO: 6.10.4 Line control
    // TODO: 6.10.5 Error directive
    // TODO: 6.10.6 Pragma directive
    // TODO: 6.10.7 Null directive
    // TODO: 6.10.8 Predefined macro names
    // TODO: 6.10.9 Pragma operator
}
