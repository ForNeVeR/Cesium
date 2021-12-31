using System.Collections.Immutable;

namespace Cesium.Ast;

/// 6.9 External definitions
public record TranslationUnit(ImmutableArray<ExternalDeclaration> Declarations);

public abstract record ExternalDeclaration;
public record FunctionDefinition(
    ImmutableArray<IDeclarationSpecifier> Specifiers,
    Declarator Declarator,
    ImmutableArray<Declaration>? Declarations,
    CompoundStatement Statement) : ExternalDeclaration;
public record SymbolDeclaration(Declaration Declaration) : ExternalDeclaration;
