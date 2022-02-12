using System.Collections.Immutable;

namespace Cesium.Ast;

// 6.7 Declarations
public record Declaration(
    ImmutableArray<IDeclarationSpecifier> Specifiers,
    ImmutableArray<InitDeclarator>? InitDeclarators) : IBlockItem;

public record InitDeclarator(Declarator Declarator, Initializer? Initializer = null);

public interface IDeclarationSpecifier { }

// 6.7.1 Storage-class specifiers
public record StorageClassSpecifier(string Name) : IDeclarationSpecifier;

// 6.7.2 Type specifiers
public interface ITypeSpecifier : ISpecifierQualifierListItem { }

public record SimpleTypeSpecifier(string TypeName) : ITypeSpecifier;
public record StructOrUnionSpecifier(
    ComplexTypeKind TypeKind,
    string? Identifier,
    ImmutableArray<StructDeclaration> StructDeclarations) : ITypeSpecifier;

// 6.7.2.1 Structure and union specifiers
public enum ComplexTypeKind
{
    Struct
}

public record StructDeclaration(
    ImmutableArray<ISpecifierQualifierListItem> SpecifiersQualifiers,
    ImmutableArray<StructDeclarator>? Declarators);

public interface ISpecifierQualifierListItem : IDeclarationSpecifier {}

public record StructDeclarator(Declarator Declarator);

// 6.7.3 Type qualifiers
public record TypeQualifier(string Name) : ISpecifierQualifierListItem;

// 6.7.7 Type names
public record AbstractDeclarator(Pointer? Pointer = null, IDirectAbstractDeclarator? DirectAbstractDeclarator = null);
public interface IDirectAbstractDeclarator
{
    IDirectAbstractDeclarator? Base { get; }
}
public record SimpleDirectAbstractDeclarator(AbstractDeclarator Declarator) : IDirectAbstractDeclarator
{
    public IDirectAbstractDeclarator? Base => null;
};
public record ArrayDirectAbstractDeclarator(
    IDirectAbstractDeclarator? Base,
    ImmutableArray<TypeQualifier>? TypeQualifiers,
    Expression? Size) : IDirectAbstractDeclarator;

// 6.7.6 Declarators
public record Declarator(Pointer? Pointer, IDirectDeclarator DirectDeclarator);
public interface IDirectDeclarator
{
    IDirectDeclarator? Base { get; }
}
public record IdentifierDirectDeclarator(string Identifier) : IDirectDeclarator
{
    public IDirectDeclarator? Base => null;
}
public record ArrayDirectDeclarator(
    IDirectDeclarator Base,
    ImmutableArray<TypeQualifier>? TypeQualifiers,
    Expression? Size) : IDirectDeclarator;
public record ParameterListDirectDeclarator(IDirectDeclarator Base, ParameterTypeList Parameters) : IDirectDeclarator;
public record IdentifierListDirectDeclarator(
    IDirectDeclarator Base,
    ImmutableArray<string>? Identifiers) : IDirectDeclarator;

public record Pointer(ImmutableArray<TypeQualifier>? TypeQualifiers = null, Pointer? ChildPointer = null);

public record ParameterTypeList(ImmutableArray<ParameterDeclaration> Parameters, bool HasEllipsis = false);

public record ParameterDeclaration(
    ImmutableArray<IDeclarationSpecifier> Specifiers,
    Declarator? Declarator = null,
    AbstractDeclarator? AbstractDeclarator = null);

// 6.7.9 Initialization
public abstract record Initializer;
public record AssignmentInitializer(Expression Expression) : Initializer;

// CLI extensions
public record CliImportSpecifier(string MemberName) : IDeclarationSpecifier;
