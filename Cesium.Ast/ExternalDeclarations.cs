using System.Collections.Immutable;

namespace Cesium.Ast;

/// 6.9 External definitions
public sealed record TranslationUnit(ImmutableArray<ExternalDeclaration> Declarations);

public abstract record ExternalDeclaration;
public sealed record FunctionDefinition(
    ImmutableArray<IDeclarationSpecifier> Specifiers,
    Declarator Declarator,
    ImmutableArray<Declaration>? Declarations,
    CompoundStatement Statement) : ExternalDeclaration;
public sealed record SymbolDeclaration(Declaration Declaration) : ExternalDeclaration;

public sealed record PInvokeDeclaration(string Declaration, string? Prefix = null) : ExternalDeclaration;
