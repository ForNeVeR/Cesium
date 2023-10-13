using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Cesium.Ast;
using Yoakke.SynKit.C.Syntax;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Parser;
using Yoakke.SynKit.Parser.Attributes;
using Range = Yoakke.SynKit.Text.Range;

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
using LiteralExpressionList = ImmutableArray<IToken<CTokenType>>;

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

    // 6.4.5 String literals

    // TODO:
    // string_literal:
    //      encoding-prefix? " s-char-sequence? "
    [Rule("string_literal: StringLiteral")]
    private static LiteralExpressionList MakeStringLiteral(ICToken stringLiteral) => ImmutableArray.Create(stringLiteral);


    [Rule("string_literal: string_literal StringLiteral")]
    private static LiteralExpressionList MakeStringLiteral(
        LiteralExpressionList prev,
        ICToken stringLiteral) => prev.Add(stringLiteral);

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
    [Rule("expression: constant_expression")] // 6.6 Constant expressions
    private static Expression CreateExpressionIdentity(Expression expression) => expression;

    [Rule("constant_expression: conditional_expression")] // 6.6 Constant expressions
    private static Expression MakeConstantExpression(Expression expression) => expression;

    // 6.5.1 Primary expressions
    [Rule("primary_expression: constant")]
    private static Expression MakeConstantExpression(ICToken constant) => new ConstantLiteralExpression(constant);

    [Rule("primary_expression: Identifier")]
    private static Expression MakeIdentifierExpression(IToken identifier) => new IdentifierExpression(identifier.Text);

    [Rule("primary_expression: StringLiteral")]
    private static Expression MakeStringLiteralExpression(ICToken stringLiteral) =>
        new ConstantLiteralExpression(stringLiteral);

    [Rule("primary_expression: string_literal")]
    private static Expression MakeStringLiteralListExpression(LiteralExpressionList literalExpressionList) =>
        new StringLiteralListExpression(literalExpressionList);

    [Rule("primary_expression: '(' expression ')'")]
    private static Expression MakeParens(IToken _, Expression expression, IToken __) => new ParenExpression(expression);

    // TODO[#207]:
    // primary-expression:
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
        IToken __)
    {
        if (function is ParenExpression { Contents: ConstantLiteralExpression { Constant: CToken { Kind: CTokenType.Identifier, Text: var name } } } &&
            arguments != null && arguments.Value.Length > 0)
        {
            return new TypeCastOrNamedFunctionCallExpression(name, arguments.Value);
        }

        return new FunctionCallExpression(function, arguments);
    }

    [Rule("postfix_expression: postfix_expression '.' Identifier")]
    private static Expression MakeMemberAccessExpression(
        Expression target,
        IToken _,
        IToken identifier) => new MemberAccessExpression(target, new IdentifierExpression(identifier.Text));

    [Rule("postfix_expression: postfix_expression '->' Identifier")]
    private static Expression MakePointerMemberAccessExpression(
        Expression target,
        IToken _,
        IToken identifier) => new PointerMemberAccessExpression(target, new IdentifierExpression(identifier.Text));

    [Rule("postfix_expression: postfix_expression '++'")]
    [Rule("postfix_expression: postfix_expression '--'")]
    private static Expression MakePostfixIncrementDecrementExpression(
        Expression target,
        ICToken operation) => new PostfixIncrementDecrementExpression(operation, target);

    // TODO[#207]:
    // postfix-expression:
    //     postfix-expression ++
    //     postfix-expression --
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

    // TODO[#207]: 6.5.3 Unary operators
    // unary-expression:
    //    postfix-expression
    [Rule("unary_expression: '++' unary_expression")]
    [Rule("unary_expression: '--' unary_expression")]
    private static Expression MakePrefixIncrementExpression(ICToken prefixOperator, Expression target) =>
        new PrefixIncrementDecrementExpression(prefixOperator, target);

    // TODO[#207]:
    // unary-expression:
    //    * unary-expression
    //    unary-operator cast-expression
    //    sizeof unary-expression
    //    sizeof ( type-name )
    //    _Alignof ( type-name )

    [Rule("unary_expression: '*' cast_expression")]
    private static Expression MakeIndirectionExpression(ICToken _, Expression target) =>
        new IndirectionExpression(target);

    [Rule("unary_expression: unary_operator unary_expression")]
    private static Expression MakeUnaryOperatorExpression(ICToken @operator, Expression target) =>
        @operator.Kind == CTokenType.Subtract && target is ConstantLiteralExpression constantExpression && constantExpression.Constant.Kind is not CTokenType.Identifier
        ? new ConstantLiteralExpression(MergeTokens(@operator, constantExpression.Constant))
        : new UnaryOperatorExpression(@operator.Text, target);

    [Rule("unary_operator: '&'")]
    // TODO[#207]: [Rule("unary_operator: '+'")]
    [Rule("unary_operator: '-'")]
    [Rule("unary_operator: '~'")]
    [Rule("unary_operator: '!'")]
    private static ICToken MakeUnaryOperator(ICToken @operator) => @operator;

    // 6.5.4 Cast operators
    [Rule("cast_expression: '(' type_name ')' cast_expression")]
    private static Expression MakeCastExpression(ICToken _, TypeName typeName, ICToken __, Expression target) =>
        new CastExpression(typeName, target);

    // 6.5.5 Multiplicative operators
    [Rule("multiplicative_expression: multiplicative_expression '*' cast_expression")]
    [Rule("multiplicative_expression: multiplicative_expression '/' cast_expression")]
    [Rule("multiplicative_expression: multiplicative_expression '%' cast_expression")]
    private static Expression MakeMultiplicativeExpression(Expression a, ICToken @operator, Expression b) =>
        new ArithmeticBinaryOperatorExpression(a, @operator.Text, b);

    // 6.5.6 Additive operators
    [Rule("additive_expression: additive_expression '+' multiplicative_expression")]
    [Rule("additive_expression: additive_expression '-' multiplicative_expression")]
    private static Expression MakeAdditiveExpression(Expression a, ICToken @operator, Expression b) =>
        new ArithmeticBinaryOperatorExpression(a, @operator.Text, b);

    // 6.5.7 Bitwise shift operators
    [Rule("shift_expression: shift_expression '<<' additive_expression")]
    [Rule("shift_expression: shift_expression '>>' additive_expression")]
    private static Expression MakeShiftExpression(Expression a, ICToken @operator, Expression b) =>
        new BitwiseBinaryOperatorExpression(a, @operator.Text, b);

    // 6.5.8 Relational operators
    [Rule("relational_expression: relational_expression '<' additive_expression")]
    [Rule("relational_expression: relational_expression '>' additive_expression")]
    [Rule("relational_expression: relational_expression '<=' additive_expression")]
    [Rule("relational_expression: relational_expression '>=' additive_expression")]
    private static Expression MakeRelationalExpression(Expression a, ICToken @operator, Expression b) =>
        new ComparisonBinaryOperatorExpression(a, @operator.Text, b);

    // 6.5.9 Equality operators
    [Rule("equality_expression: equality_expression '==' additive_expression")]
    [Rule("equality_expression: equality_expression '!=' additive_expression")]
    private static Expression MakeEqualityExpression(Expression a, ICToken @operator, Expression b) =>
        new ComparisonBinaryOperatorExpression(a, @operator.Text, b);

    // 6.5.10 Bitwise AND operator
    [Rule("AND_expression: AND_expression '&' equality_expression")]
    private static Expression MakeBitwiseAndExpression(Expression a, ICToken @operator, Expression b) =>
        new BitwiseBinaryOperatorExpression(a, @operator.Text, b);

    // 6.5.11 Bitwise exclusive OR operator
    [Rule("exclusive_OR_expression: exclusive_OR_expression '^' AND_expression")]
    private static Expression MakeBitwiseXorExpression(Expression a, ICToken @operator, Expression b) =>
        new BitwiseBinaryOperatorExpression(a, @operator.Text, b);

    // 6.5.12 Bitwise inclusive OR operator
    [Rule("inclusive_OR_expression: inclusive_OR_expression '|' exclusive_OR_expression")]
    private static Expression MakeBitwiseOrExpression(Expression a, ICToken @operator, Expression b) =>
        new BitwiseBinaryOperatorExpression(a, @operator.Text, b);

    // 6.5.13 Logical AND operator
    [Rule("logical_AND_expression: logical_AND_expression '&&' inclusive_OR_expression")]
    private static Expression MakeLogicalAndExpression(Expression a, ICToken @operator, Expression b) =>
        new LogicalBinaryOperatorExpression(a, @operator.Text, b);

    // 6.5.14 Logical OR operator
    [Rule("logical_OR_expression: logical_OR_expression '||' logical_AND_expression")]
    private static Expression MakeLogicalOrExpression(Expression a, ICToken @operator, Expression b) =>
        new LogicalBinaryOperatorExpression(a, @operator.Text, b);

    // 6.5.15 Conditional operator
    [Rule("conditional_expression: logical_OR_expression '?' expression ':' conditional_expression")]
    private static Expression MakeConditionalExpression(Expression a, ICToken _, Expression b, ICToken __, Expression c) =>
        new ConditionalExpression(a, b, c);

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
    [Rule("expression: expression ',' assignment_expression")]
    private static Expression MakeCommaExpression(Expression left, IToken _, Expression right) =>
        new CommaExpression(left, right);

    // TODO[#207]: 6.6 Constant expressions

    // 6.7 Declarations

    // TODO[#107]: Custom parsing is required here due to the reasons outlined in the issue.
    //
    // declaration: declaration_specifiers init_declarator_list? ';'
    [CustomParser("declaration")]
    private ParseResult<IBlockItem> CustomParseDeclaration(int offset)
    {
        var declarationSpecifiersResult = CustomParseOneOrMore(parseDeclarationSpecifier, offset);
        if (declarationSpecifiersResult.IsError) return declarationSpecifiersResult.Error;
        offset = declarationSpecifiersResult.Ok.Offset;
        var declarationSpecifiers = declarationSpecifiersResult.Ok.Value;

        var initDeclaratorList = parseInitDeclaratorList(offset);
        if (initDeclaratorList.IsError && declarationSpecifiers.Count > 1)
        {
            // Try backtracking: drop the last declaration specifier and parse again:
            var preLastDeclarationSpecifier = declarationSpecifiers[^2];
            initDeclaratorList = parseInitDeclaratorList(preLastDeclarationSpecifier.Offset);
            if (initDeclaratorList.IsOk)
                declarationSpecifiers.RemoveAt(declarationSpecifiers.Count - 1);
        }

        if (initDeclaratorList.IsOk)
            offset = initDeclaratorList.Ok.Offset;

        if (TokenStream.TryLookAhead(offset, out var t) && t.Text == ";")
        {
            ++offset;
            return ParseResult.Ok(
                MakeDeclaration(
                    MakeDeclarationSpecifiers(declarationSpecifiers.Select(ds => ds.Item)),
                    initDeclaratorList.IsOk ? initDeclaratorList.Ok.Value : null,
                    t),
                offset,
                initDeclaratorList.FurthestError);
        }

        return ParseResult.Error(";", t, t!.Range.Start, ";");
    }

    private static IBlockItem MakeDeclaration(
        DeclarationSpecifiers specifiers,
        InitDeclaratorList? initDeclarators,
        IToken _)
    {
        // NOTE: this is the "lexer hack" to deal with syntax ambiguity. The same syntax may be either a function call
        // or a variable declaration, depending on the context, and we have no this information in the parser, yet.
        if (specifiers.Length == 1 && specifiers.Single() is NamedTypeSpecifier specifier
            && initDeclarators?.Length == 1)
        {
            var ((pointer, directDeclarator), initializer) = initDeclarators.Value.Single();
            if (pointer == null && initializer == null && directDeclarator is DeclaratorDirectDeclarator ddd)
            {
                ddd.Deconstruct(out var nestedDeclarator);
                var (nestedPointer, nestedDirectDeclarator) = nestedDeclarator;
                if (nestedPointer == null && nestedDirectDeclarator is IdentifierDirectDeclarator idd)
                {
                    idd.Deconstruct(out var identifier);
                    return new AmbiguousBlockItem(specifier.TypeDefName, identifier);
                }
            }
        }

        return new Declaration(specifiers, initDeclarators);
    }

    // TODO[#107]: This is a synthetic set of rules which is absent from the C standard, but required to simplify the
    // implementation. Get rid of this, eventually.
    [Rule("declaration_specifiers: declaration_specifier+")]
    private static DeclarationSpecifiers MakeDeclarationSpecifiers(IEnumerable<IDeclarationSpecifier> specifiers) =>
        specifiers.ToImmutableArray();

    [Rule("declaration_specifier: cli_import_specifier")] // Extension, see CParser.CliExtensions.cs
    [Rule("declaration_specifier: storage_class_specifier")]
    [Rule("declaration_specifier: type_specifier")]
    [Rule("declaration_specifier: type_qualifier")]
    private static IDeclarationSpecifier MakeDeclarationSpecifier(IDeclarationSpecifier specifier) => specifier;

    // TODO[#207]: [Rule("declaration_specifier: function_specifier")]
    // TODO[#207]: [Rule("declaration_specifier: alignment_specifier")]

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
    [Rule("storage_class_specifier: 'extern'")]
    [Rule("storage_class_specifier: 'static'")]
    // TODO[#211]:
    // storage-class-specifier:
    //     _Thread_local
    //     auto
    //     register
    private static StorageClassSpecifier MakeStorageClassSpecifier(IToken keyword) => new(keyword.Text);

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
    [Rule("type_specifier: '__nint'")]
    [Rule("type_specifier: '__nuint'")]
    private static ITypeSpecifier MakeSimpleTypeSpecifier(ICToken specifier) => new SimpleTypeSpecifier(specifier.Text);

    // TODO[#211]: [Rule("type_specifier: atomic_type_specifier")]

    [Rule("type_specifier: struct_or_union_specifier")]
    private static ITypeSpecifier MakeComplexTypeSpecifier(StructOrUnionSpecifier structOrUnionSpecifier) =>
        structOrUnionSpecifier;

    [Rule("type_specifier: enum_specifier")]
    private static ITypeSpecifier MakeTypeSpecifier(EnumSpecifier enumDeclaration) =>
        enumDeclaration;

    [Rule("type_specifier: typedef_name")]
    private static ITypeSpecifier MakeNamedTypeSpecifier(IToken typeDefName) =>
        new NamedTypeSpecifier(typeDefName.Text);

    // 6.7.2.1 Structure and union specifiers

    [Rule("struct_or_union_specifier: struct_or_union Identifier? '{' struct_declaration_list '}'")]
    private static StructOrUnionSpecifier MakeStructOrUnionSpecifier(
        ComplexTypeKind structOrUnion,
        IToken? identifier,
        IToken _,
        StructDeclarationList structDeclarationList,
        IToken __) => new(structOrUnion, identifier?.Text, structDeclarationList);

    [Rule("struct_or_union_specifier: struct_or_union Identifier")]
    private static StructOrUnionSpecifier MakeStructOrUnionSpecifier(
        ComplexTypeKind structOrUnion,
        IToken identifier) => new(structOrUnion, identifier?.Text, ImmutableArray<StructDeclaration>.Empty);

    // TODO[#211]: struct-or-union-specifier: struct-or-union identifier

    [Rule("struct_or_union: 'struct'")]
    private static ComplexTypeKind MakeStructComplexTypeKind(IToken _) => ComplexTypeKind.Struct;
    // TODO[#211]: struct-or-union: union

    [Rule("struct_declaration_list: struct_declaration")]
    private static StructDeclarationList MakeStructDeclarationList(StructDeclaration structDeclaration) =>
        ImmutableArray.Create(structDeclaration);

    [Rule("struct_declaration_list: struct_declaration_list struct_declaration")]
    private static StructDeclarationList MakeStructDeclarationList(
        StructDeclarationList prev,
        StructDeclaration structDeclaration) => prev.Add(structDeclaration);

    // TODO[#107]: Custom parsing is required here due to the reasons outlined in the issue.
    //
    // struct_declaration: specifier_qualifier_list struct_declarator_list? ';'
    [CustomParser("struct_declaration")]
    private ParseResult<StructDeclaration> CustomParseStructDeclaration(int offset)
    {
        var specifiersQualifiersResult = CustomParseOneOrMore(parseSpecifierQualifierListItem, offset);
        if (specifiersQualifiersResult.IsError) return specifiersQualifiersResult.Error;
        offset = specifiersQualifiersResult.Ok.Offset;
        var specifiersQualifiers = specifiersQualifiersResult.Ok.Value;

        var structDeclaratorList = parseStructDeclaratorList(offset);
        if (structDeclaratorList.IsError && specifiersQualifiers.Count > 1)
        {
            // Try backtracking: drop the last declaration specifier and parse again:
            var preLastSpecifierQualifier = specifiersQualifiers[^2];
            structDeclaratorList = parseStructDeclaratorList(preLastSpecifierQualifier.Offset);
            if (structDeclaratorList.IsOk)
                specifiersQualifiers.RemoveAt(specifiersQualifiers.Count - 1);
        }

        if (structDeclaratorList.IsOk)
            offset = structDeclaratorList.Ok.Offset;

        if (TokenStream.TryLookAhead(offset, out var t) && t.Text == ";")
        {
            ++offset;
            return ParseResult.Ok(
                MakeStructDeclaration(
                    MakeSpecifierQualifierList(specifiersQualifiers.Select(pair => pair.Item)),
                    structDeclaratorList.IsOk ? structDeclaratorList.Ok.Value : null,
                    t
                ),
                offset,
                structDeclaratorList.FurthestError);
        }

        return ParseResult.Error(";", t, t!.Range.Start, ";");
    }

    private static StructDeclaration MakeStructDeclaration(
        SpecifierQualifierList specifiersQualifiers,
        StructDeclaratorList? structDeclarators,
        IToken _) => new(specifiersQualifiers, structDeclarators);

    [Rule("enum_specifier: 'enum' Identifier")]
    private static EnumSpecifier MakeEnumSpecifier(IToken _, IToken identifier) =>
        new EnumSpecifier(identifier.Text, null);

    [Rule("enum_specifier: 'enum' Identifier '{' enumerator_list '}'")]
    private static EnumSpecifier MakeEnumSpecifier(IToken _, IToken identifier, IToken openBracket, ImmutableArray<EnumDeclaration> enumeratorList, IToken closeBracket) =>
        new EnumSpecifier(identifier.Text, enumeratorList);

    [Rule("enumerator_list: (enumerator (',' enumerator)*)")]
    private static ImmutableArray<EnumDeclaration> MakeEnumeratorList(Punctuated<EnumDeclaration, ICToken> declarations) =>
        declarations.Values.ToImmutableArray();

    [Rule("enumerator: Identifier")]
    private static EnumDeclaration MakeEnumerator(IToken identifier) =>
        new EnumDeclaration(identifier.Text, null);

    [Rule("enumerator: Identifier '=' constant_expression")]
    private static EnumDeclaration MakeEnumerator(IToken identifier, IToken _, Expression expression) =>
        new EnumDeclaration(identifier.Text, expression);

    // TODO[#211]: struct-declaration: static_assert-declaration

    // TODO[#107]: This is a synthetic set of rules which is absent from the C standard, but required to simplify the
    // implementation. Get rid of this, eventually.
    //
    // The actual rules are:
    // specifier_qualifier_list: type_specifier specifier_qualifier_list?
    // specifier_qualifier_list: type_qualifier specifier_qualifier_list?
    [Rule("specifier_qualifier_list: specifier_qualifier_list_item+")]
    private static SpecifierQualifierList MakeSpecifierQualifierList(
        IEnumerable<ISpecifierQualifierListItem> specifiersQualifiers) =>
        specifiersQualifiers.ToImmutableArray();

    [Rule("specifier_qualifier_list_item: type_specifier")]
    [Rule("specifier_qualifier_list_item: type_qualifier")]
    private static ISpecifierQualifierListItem MakeSpecifierQualifierListItem(ISpecifierQualifierListItem item) => item;

    // TODO[#211]: specifier-qualifier-list: alignment-specifier specifier-qualifier-list?

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

    // TODO[#211]: struct-declarator: declarator? : constant-expression

    // TODO[#211]: 6.7.2.2 Enumeration specifiers
    // TODO[#211]: 6.7.2.3 Tags
    // TODO[#211]: 6.7.2.4 Atomic type specifiers

    // 6.7.3 Type qualifiers
    [Rule("type_qualifier: 'const'")]
    [Rule("type_qualifier: 'restrict'")]
    [Rule("type_qualifier: 'volatile'")]
    [Rule("type_qualifier: '_Atomic'")]
    private static TypeQualifier MakeTypeQualifier(ICToken name) => new(name.Text);

    // TODO[#211]: 6.7.4 Function specifiers
    // TODO[#211]: 6.7.5 Alignment specifier

    // 6.7.6 Declarators
    [Rule("declarator: pointer? direct_declarator")]
    private static Declarator MakeDeclarator(Pointer? pointer, IDirectDeclarator directDeclarator) =>
        new(pointer, directDeclarator);

    [Rule("direct_declarator: Identifier")]
    private static IDirectDeclarator MakeDirectDeclarator(ICToken identifier) =>
        new IdentifierDirectDeclarator(identifier.Text);

    [Rule("direct_declarator: '(' declarator ')'")]
    private static IDirectDeclarator MakeDirectDeclarator(IToken _, Declarator declarator, IToken __) =>
        new DeclaratorDirectDeclarator(declarator);

    [Rule("direct_declarator: direct_declarator '[' type_qualifier_list? assignment_expression? ']'")]
    private static IDirectDeclarator MakeDirectDeclarator(
        IDirectDeclarator @base,
        IToken _,
        TypeQualifierList? typeQualifiers,
        Expression? expression,
        IToken __) => new ArrayDirectDeclarator(@base, typeQualifiers, expression);
    // TODO[#211]: direct_declarator: direct_declarator [ static type_qualifier_list? assignment_expression ]
    // TODO[#211]: direct_declarator: direct_declarator [ type_qualifier_list static assignment_expression ]
    // TODO[#211]: direct_declarator: direct_declarator [ type_qualifier_list? * ]

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

    // TODO[#107]: Custom parsing is required here due to the reasons outlined in the issue.
    //
    // parameter_declaration: declaration_specifiers declarator
    [CustomParser("parameter_declaration")]
    private ParseResult<ParameterDeclaration> CustomParseParameterDeclaration(int offset)
    {
        var specifiersAndDeclarator = CustomParseSpecifiersAndDeclarator(offset);
        if (specifiersAndDeclarator.IsError) return specifiersAndDeclarator.Error;
        offset = specifiersAndDeclarator.Ok.Offset;

        var (specifiers, declarator) = specifiersAndDeclarator.Ok.Value;

        return ParseResult.Ok(
            MakeParameterDeclaration(specifiers, declarator),
            offset,
            specifiersAndDeclarator.FurthestError);
    }

    private static ParameterDeclaration MakeParameterDeclaration(
        DeclarationSpecifiers specifiers,
        Declarator declarator) => new(specifiers, declarator);

    [Rule("parameter_declaration: declaration_specifiers abstract_declarator?")]
    private static ParameterDeclaration MakeParameterDeclaration(
        DeclarationSpecifiers specifiers,
        AbstractDeclarator? declarator) => new(specifiers, AbstractDeclarator: declarator);

    [Rule("identifier_list: Identifier")]
    private static IdentifierList MakeIdentifierList(ICToken identifier) => ImmutableArray.Create(identifier.Text);

    [Rule("identifier_list: identifier_list ',' Identifier")]
    private static IdentifierList MakeIdentifierList(IdentifierList prev, ICToken _, ICToken identifier) =>
        prev.Add(identifier.Text);

    // 6.7.7 Type names

    [Rule("type_name: specifier_qualifier_list abstract_declarator?")]
    private static TypeName MakeTypeName(SpecifierQualifierList specifierQualifierList, AbstractDeclarator? abstractDeclarator) => new(specifierQualifierList, abstractDeclarator);

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

    // TODO[#211]:
    // direct-abstract-declarator:
    //     direct-abstract-declarator? [ static type-qualifier-list? assignment-expression ]
    //     direct-abstract-declarator? [ type-qualifier-list static assignment-expression ]
    //     direct-abstract-declarator? [ * ]
    //     direct-abstract-declarator? ( parameter-type-list? )

    // 6.7.8 Type definitions
    [Rule("typedef_name: Identifier")]
    private static IToken MakeTypeDefName(IToken identifier) => identifier;

    // 6.7.9 Initialization

    [Rule("initializer: assignment_expression")]
    private static Initializer MakeInitializer(Expression assignmentExpression) =>
        new AssignmentInitializer(assignmentExpression);

    [Rule("initializer: '{' initializer_list '}' ")]
    private static Initializer MakeInitializer(IToken _, ImmutableArray<Initializer> initializers, IToken __) =>
        new ArrayInitializer(initializers);

    [Rule("initializer: '{' initializer_list ',' '}' ")]
    private static Initializer MakeInitializer(IToken _, ImmutableArray<Initializer> initializers, IToken __, IToken ___) =>
        new ArrayInitializer(initializers);

    [Rule("initializer_list: initializer")]
    private static ImmutableArray<Initializer> MakeInitializerList(Initializer initializer) =>
        ImmutableArray.Create<Initializer>(initializer);

    [Rule("initializer_list: initializer_list ',' initializer")]
    private static ImmutableArray<Initializer> MakeInitializerList(ImmutableArray<Initializer> initializers, IToken _, Initializer initializer) =>
        initializers.Add(initializer);

    // TODO[#211]:
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

    // TODO[#211]: 6.7.10 Static assertions

    // 6.8 Statements and blocks
    [Rule("statement: compound_statement")]
    [Rule("statement: labeled_statement")]
    [Rule("statement: expression_statement")]
    [Rule("statement: selection_statement")]
    [Rule("statement: iteration_statement")]
    [Rule("statement: jump_statement")]
    private static Statement MakeStatementIdentity(Statement statement) => statement;

    // 6.8.1 Labeled statements
    [Rule("labeled_statement: Identifier ':' statement")]
    private static Statement MakeLabelStatement(IToken identifier, IToken _, Statement block) =>
        new LabelStatement(identifier.Text, block);

    [Rule("labeled_statement: 'case' constant_expression ':' statement")]
    private static Statement MakeCaseStatement(IToken _, Expression constant, IToken __, Statement block) =>
        new CaseStatement(constant, block);

    [Rule("labeled_statement: 'default' ':' statement")]
    private static Statement MakeDefaultStatement(IToken _, IToken __, Statement block) =>
        new CaseStatement(null, block);

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
    private static ExpressionStatement MakeExpressionStatement(Expression? expression, IToken _) => new(expression);

    // 6.8.4 Selection statements
    [Rule("selection_statement: 'if' '(' expression ')' statement")]
    private static Statement MakeIfStatement(
        IToken _,
        IToken __,
        Expression expression,
        IToken ___,
        IBlockItem statement) =>
        // TODO[#115]: This direct cast should't be necessary. It is here because of the "lexer hack".
        new IfElseStatement(expression, (Statement)statement, null);

    [Rule("selection_statement: 'if' '(' expression ')' statement 'else' statement")]
    private static Statement MakeIfElseStatement(
        IToken _,
        IToken __,
        Expression expression,
        IToken ___,
        IBlockItem trueBranch,
        IToken ____,
        IBlockItem falseBranch)
        // TODO[#115]: These direct casts should't be necessary. They are here because of the "lexer hack".
        => new IfElseStatement(expression, (Statement)trueBranch, (Statement)falseBranch);

    [Rule("selection_statement: 'switch' '(' expression ')' statement")]
    private static Statement MakeSwitchStatement(
        IToken _, // switch
        IToken __, // (
        Expression expression,
        IToken ___, // )
        Statement body)
        => new SwitchStatement(expression, body);

    // 6.8.5 Iteration statements
    [Rule("iteration_statement: 'while' '(' expression ')' statement")]
    private static Statement MakeWhileStatement(
        ICToken _,
        ICToken __,
        Expression testExpression,
        ICToken ___,
        IBlockItem body)
        => new WhileStatement(testExpression, body);

    [Rule("iteration_statement: 'do' statement 'while' '(' expression ')' ';'")]
    private static Statement MakeDoWhileStatement(
        ICToken _,
        IBlockItem body,
        ICToken __,
        ICToken ___,
        Expression testExpression,
        ICToken ____,
        ICToken _____)
        => new DoWhileStatement(testExpression, body);

    [Rule("iteration_statement: 'for' '(' expression? ';' expression? ';' expression? ')' statement")]
    private static Statement MakeForStatement(
        ICToken _,
        ICToken __,
        Expression initExpression,
        ICToken ___,
        Expression testExpression,
        ICToken ____,
        Expression updateExpression,
        ICToken _____,
        IBlockItem body)
        => new ForStatement(null, initExpression, testExpression, updateExpression, body);

    [Rule("iteration_statement: 'for' '(' declaration expression? ';' expression? ')' statement")]
    private static Statement MakeForStatement(
        ICToken _,
        ICToken __,
        IBlockItem initDeclaration,
        Expression testExpression,
        ICToken ____,
        Expression updateExpression,
        ICToken _____,
        IBlockItem body)
    {
        return new ForStatement(initDeclaration, null, testExpression, updateExpression, body);
    }

    // 6.8.6 Jump statements
    [Rule("jump_statement: 'goto' Identifier ';'")]
    private static Statement MakeGoToStatement(ICToken _, ICToken identifier, ICToken __) =>
        new GoToStatement(identifier.Text);

    [Rule("jump_statement: 'continue' ';'")]
    private static Statement MakeContinueStatement(ICToken _, ICToken __)
        => new ContinueStatement();

    [Rule("jump_statement: 'break' ';'")]
    private static Statement MakeBreakStatement(ICToken _, ICToken __)
        => new BreakStatement();

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
    private static ExternalDeclaration MakeExternalDeclaration(IBlockItem declaration) =>
        // TODO[#115]: This direct cast should't be necessary. It is here because of the "lexer hack".
        new SymbolDeclaration((Declaration)declaration);

    // 6.9.1 Function definitions

    // TODO[#107]: Custom parsing is required here due to the reasons outlined in the issue.
    //
    // function_definition: declaration_specifiers declarator declaration_list? compound_statement
    [CustomParser("function_definition")]
    private ParseResult<FunctionDefinition> CustomParseFunctionDefinition(int offset)
    {
        var specifiersAndDeclarator = CustomParseSpecifiersAndDeclarator(offset);
        if (specifiersAndDeclarator.IsError) return specifiersAndDeclarator.Error;
        offset = specifiersAndDeclarator.Ok.Offset;

        var declarationList = parseDeclarationList(offset);
        if (declarationList.IsOk) offset = declarationList.Ok.Offset;

        var statement = parseCompoundStatement(offset);
        if (statement.IsError) return statement.Error;
        offset = statement.Ok.Offset;

        var (specifiers, declarator) = specifiersAndDeclarator.Ok.Value;
        return ParseResult.Ok(
            MakeFunctionDefinition(
                specifiers,
                declarator,
                declarationList.IsOk ? declarationList.Ok.Value : null,
                statement.Ok.Value),
            offset,
            statement.FurthestError);
    }

    private static FunctionDefinition MakeFunctionDefinition(
        DeclarationSpecifiers specifiers,
        Declarator declarator,
        ImmutableArray<Declaration>? declarationList,
        CompoundStatement statement) => new(specifiers, declarator, declarationList, statement);

    [Rule("declaration_list: declaration")]
    private static ImmutableArray<Declaration> MakeDeclarationList(IBlockItem declaration) =>
        // TODO[#115]: This direct cast should't be necessary. It is here because of the "lexer hack".
        ImmutableArray.Create((Declaration)declaration);

    [Rule("declaration_list: declaration_list declaration")]
    private static ImmutableArray<Declaration> MakeDeclarationList(
        ImmutableArray<Declaration> declarations,
        IBlockItem newDeclaration) =>
        // TODO[#115]: This direct cast should't be necessary. It is here because of the "lexer hack".
        declarations.Add((Declaration)newDeclaration);

    // TODO[#78]: 6.9.2 External object definitions

    private ParseResult<(DeclarationSpecifiers, Declarator)> CustomParseSpecifiersAndDeclarator(int offset)
    {
        var declarationSpecifiersResult = CustomParseOneOrMore(parseDeclarationSpecifier, offset);
        if (declarationSpecifiersResult.IsError) return declarationSpecifiersResult.Error;
        offset = declarationSpecifiersResult.Ok.Offset;
        var declarationSpecifiers = declarationSpecifiersResult.Ok.Value;

        var declarator = parseDeclarator(offset);
        if (declarator.IsError && declarationSpecifiers.Count > 1)
        {
            // Try backtracking: drop the last declaration specifier and parse again:
            var preLastDeclarationSpecifier = declarationSpecifiers[^2];
            declarationSpecifiers.RemoveAt(declarationSpecifiers.Count - 1);
            offset = preLastDeclarationSpecifier.Offset;

            declarator = parseDeclarator(offset);
            if (declarator.IsError) return declarator.Error;
        }

        if (declarator.IsError) return declarator.Error;
        offset = declarator.Ok.Offset;

        return ParseResult.Ok(
            (MakeDeclarationSpecifiers(declarationSpecifiers.Select(pair => pair.Item)), declarator.Ok.Value),
            offset,
            declarator.FurthestError);
    }

    /// <remarks>
    /// HACK: Usually, this would be a call to <code>parseDeclarationSpecifiers(offset)</code> instead of, say,
    /// <code>CustomParseOneOrMore(parseDeclarationSpecifier, offset)</code>. But here, we have to parse them
    /// one by one and remember the offset of every one, to be able to backtrack if necessary.
    /// </remarks>
    private static ParseResult<List<(T Item, int Offset)>> CustomParseOneOrMore<T>(
        Func<int, ParseResult<T>> singleItemParser,
        int offset)
    {
        var firstItem = singleItemParser(offset);
        if (firstItem.IsError) return firstItem.Error;
        offset = firstItem.Ok.Offset;

        var declarationSpecifiers = new List<(T Item, int Offset)> { (firstItem.Ok.Value, offset) };
        while (true)
        {
            var declarationSpecifier = singleItemParser(offset);
            if (declarationSpecifier.IsError) break;
            offset = declarationSpecifier.Ok.Offset;

            declarationSpecifiers.Add((declarationSpecifier.Ok.Value, offset));
        }

        return ParseResult.Ok(declarationSpecifiers, offset);
    }

    private static ICToken MergeTokens(ICToken token1, ICToken token2) =>
        new Token<CTokenType>(new Range(token1.Range, token2.Range), token1.Text + token2.Text, token2.Kind);
}
