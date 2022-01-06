using System.Collections.Immutable;

namespace Cesium.Ast;

// 6.7 Declarations
public record Declaration(
    ImmutableArray<IDeclarationSpecifier> Specifiers,
    ImmutableArray<InitDeclarator>? InitDeclarators) : IBlockItem;

public record InitDeclarator(Declarator Declarator, Initializer? Initializer = null);

public interface IDeclarationSpecifier { }
// 6.7.2 Type specifiers
public record TypeSpecifier(string TypeName) : IDeclarationSpecifier;

// 6.7.3 Type qualifiers
public record TypeQualifier(string Name) : IDeclarationSpecifier;

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
    Expression? SizeExpression) : IDirectAbstractDeclarator;

// 6.7.6 Declarators
public record Declarator(Pointer? Pointer, DirectDeclarator DirectDeclarator);
public record DirectDeclarator(
    string Name,
    ParameterTypeList? ParameterList = null,
    ImmutableArray<string>? IdentifierList = null);

public record Pointer(ImmutableArray<TypeQualifier>? TypeQualifiers = null, Pointer? ChildPointer = null);

public record ParameterTypeList(ImmutableArray<ParameterDeclaration> Parameters, bool IsVararg = false);

public record ParameterDeclaration(
    ImmutableArray<IDeclarationSpecifier> Specifiers,
    Declarator? Declarator = null,
    AbstractDeclarator? AbstractDeclarator = null);

// 6.7.9 Initialization
public abstract record Initializer;
public record AssignmentInitializer(Expression Expression) : Initializer;

// CLI extensions
public record CliImportSpecifier(string MemberName) : IDeclarationSpecifier;
