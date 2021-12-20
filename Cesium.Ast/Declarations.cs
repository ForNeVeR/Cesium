using System.Collections.Immutable;

namespace Cesium.Ast;

// 6.7 Declarations
public record Declaration(
    ImmutableArray<DeclarationSpecifier> Specifiers,
    ImmutableArray<InitDeclarator>? InitDeclarators) : IBlockItem;

public record InitDeclarator(Declarator Declarator);

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
