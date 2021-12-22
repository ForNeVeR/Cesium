using System.Collections.Immutable;

namespace Cesium.Ast;

// 6.7 Declarations
public record Declaration(
    ImmutableArray<DeclarationSpecifier> Specifiers,
    ImmutableArray<InitDeclarator>? InitDeclarators) : IBlockItem;

public record InitDeclarator(Declarator Declarator, Initializer? Initializer = null);

public abstract record DeclarationSpecifier;
// 6.7.2 Type specifiers
public record TypeSpecifier(string TypeName) : DeclarationSpecifier;

// 6.7.3 Type qualifiers
public record TypeQualifier(string Name);

// 6.7.6 Declarators
public record Declarator(Pointer? Pointer, DirectDeclarator DirectDeclarator);
public record DirectDeclarator(
    string Name,
    ParameterTypeList? ParameterList = null,
    ImmutableArray<string>? IdentifierList = null);

public record Pointer(ImmutableArray<TypeQualifier>? TypeQualifiers = null, Pointer? ChildPointer = null);

public record ParameterTypeList(ImmutableArray<ParameterDeclaration> Parameters, bool IsVararg = false);

public record ParameterDeclaration(ImmutableArray<DeclarationSpecifier> Specifiers, Declarator? Declarator = null);

// 6.7.9 Initialization
public abstract record Initializer;
public record AssignmentInitializer(Expression Expression) : Initializer;

// CLI extensions
public record CliImportSpecifier(string MemberName) : DeclarationSpecifier;
