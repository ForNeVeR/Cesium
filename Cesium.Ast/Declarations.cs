using System.Collections.Immutable;

namespace Cesium.Ast;

// 6.7 Declarations
public record Declaration(
    ImmutableArray<DeclarationSpecifier> Specifiers,
    ImmutableArray<InitDeclarator>? InitDeclarators) : IBlockItem;

public record DeclarationSpecifier;

public record InitDeclarator(Declarator Declarator);

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

// 6.8 Statements and blocks
public record Statement;
public record CompoundStatement(ImmutableArray<IBlockItem> Block) : Statement;

public interface IBlockItem {}

/// 6.9 External definitions
public record TranslationUnit(ImmutableArray<ExternalDeclaration> Declarations);

public record ExternalDeclaration;
public record FunctionDefinition(
    ImmutableArray<DeclarationSpecifier> Specifiers,
    Declarator Declarator,
    ImmutableArray<Declaration>? Declarations,
    CompoundStatement Statement) : ExternalDeclaration;
