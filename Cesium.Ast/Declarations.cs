using System.Collections.Immutable;

namespace Cesium.Ast;
using SpecifierQualifierList = ImmutableArray<ISpecifierQualifierListItem>;

// 6.7 Declarations
public sealed record Declaration(
    ImmutableArray<IDeclarationSpecifier> Specifiers,
    ImmutableArray<InitDeclarator>? InitDeclarators) : IBlockItem;

public sealed record InitDeclarator(Declarator Declarator, Initializer? Initializer = null);

public interface IDeclarationSpecifier { }

// 6.7.1 Storage-class specifiers
public sealed record StorageClassSpecifier(string Name) : IDeclarationSpecifier;

// 6.7.2 Type specifiers
public interface ITypeSpecifier : ISpecifierQualifierListItem { }

public sealed record SimpleTypeSpecifier(string TypeName) : ITypeSpecifier;
public sealed record StructOrUnionSpecifier(
    ComplexTypeKind TypeKind,
    string? Identifier,
    ImmutableArray<StructDeclaration> StructDeclarations) : ITypeSpecifier;
public sealed record EnumSpecifier(
    string? Identifier,
    ImmutableArray<EnumDeclaration>? StructDeclarations) : ITypeSpecifier;

public sealed record NamedTypeSpecifier(string TypeDefName) : ITypeSpecifier;

// 6.7.2.1 Structure and union specifiers
public enum ComplexTypeKind
{
    Struct,
    Union
}

public sealed record StructDeclaration(
    ImmutableArray<ISpecifierQualifierListItem> SpecifiersQualifiers,
    ImmutableArray<StructDeclarator>? Declarators);

public interface ISpecifierQualifierListItem : IDeclarationSpecifier {}

public sealed record StructDeclarator(Declarator Declarator);

public sealed record EnumDeclaration(string Identifier, Expression? Constant);

// 6.7.3 Type qualifiers
public sealed record TypeQualifier(string Name) : ISpecifierQualifierListItem;

// 6.7.7 Type names
public sealed record TypeName(SpecifierQualifierList SpecifierQualifierList, AbstractDeclarator? AbstractDeclarator = null);
public sealed record AbstractDeclarator(Pointer? Pointer = null, IDirectAbstractDeclarator? DirectAbstractDeclarator = null);
public interface IDirectAbstractDeclarator
{
    IDirectAbstractDeclarator? Base { get; }
}
public sealed record SimpleDirectAbstractDeclarator(AbstractDeclarator Declarator) : IDirectAbstractDeclarator
{
    public IDirectAbstractDeclarator? Base => null;
};

public sealed record ArrayDirectAbstractDeclarator(
    IDirectAbstractDeclarator? Base,
    ImmutableArray<TypeQualifier>? TypeQualifiers,
    Expression? Size) : IDirectAbstractDeclarator;

// 6.7.6 Declarators
public sealed record Declarator(Pointer? Pointer, IDirectDeclarator DirectDeclarator);
public interface IDirectDeclarator
{
    IDirectDeclarator? Base { get; }
}
public record IdentifierDirectDeclarator(string Identifier) : IDirectDeclarator
{
    public IDirectDeclarator? Base => null;
}
public sealed record ArrayDirectDeclarator(
    IDirectDeclarator Base,
    ImmutableArray<TypeQualifier>? TypeQualifiers,
    Expression? Size) : IDirectDeclarator;
public sealed record ParameterListDirectDeclarator(IDirectDeclarator Base, ParameterTypeList Parameters) : IDirectDeclarator;
public sealed record IdentifierListDirectDeclarator(
    IDirectDeclarator Base,
    ImmutableArray<string>? Identifiers) : IDirectDeclarator;

public record DeclaratorDirectDeclarator(Declarator Declarator) : IDirectDeclarator
{
    public IDirectDeclarator? Base => null;
}

public sealed record Pointer(ImmutableArray<TypeQualifier>? TypeQualifiers = null, Pointer? ChildPointer = null);

public sealed record ParameterTypeList(ImmutableArray<ParameterDeclaration> Parameters, bool HasEllipsis = false);

public sealed record ParameterDeclaration(
    ImmutableArray<IDeclarationSpecifier> Specifiers,
    Declarator? Declarator = null,
    AbstractDeclarator? AbstractDeclarator = null);

// 6.7.9 Initialization
public abstract record Initializer(Designation? Designation);
public sealed record AssignmentInitializer(Expression Expression) : Initializer(Designation: null);
public sealed record ArrayInitializer(ImmutableArray<Initializer> Initializers) : Initializer(Designation: null);

public sealed record Designation(ImmutableArray<Designator> Designators);

public abstract record Designator;
public sealed record BracketsDesignator(Expression Expression) : Designator;
public sealed record IdentifierDesignator(string FieldName) : Designator;

// CLI extensions
public sealed record CliImportSpecifier(string MemberName) : IDeclarationSpecifier;
