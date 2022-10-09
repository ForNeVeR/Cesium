using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using System.Diagnostics.CodeAnalysis;

namespace Cesium.CodeGen.Contexts;

internal interface IDeclarationScope
{
    CTypeSystem CTypeSystem { get; }
    bool TryGetFunctionInfo(string identifier, [NotNullWhen(true)] out FunctionInfo? functionInfo);
    IReadOnlyDictionary<string, IType> GlobalFields { get; }
    void AddVariable(string identifier, IType variable);
    bool TryGetVariable(string identifier, [NotNullWhen(true)]out IType? type);

    /// <summary>
    /// Recursively resolve the passed type and all its members, replacing `NamedType` in any points with their actual instantiations in the current context.
    /// </summary>
    /// <param name="type">Type which should be resolved.</param>
    /// <returns>A <see cref="IType"/> which fully resolves.</returns>
    /// <exception cref="CompilationException">Throws a <see cref="CompilationException"/> if it's not possible to resolve some of the types.</exception>
    IType ResolveType(IType type);
    void AddTypeDefinition(string identifier, IType type);
    ParameterInfo? GetParameterInfo(string name);

    /// <summary>
    /// Adds label to the scope.
    /// </summary>
    /// <param name="identifier">Label to add to the current scope.</param>
    void AddLabel(string identifier);

    /// <summary>
    // Registers child declaration scope.
    /// </summary>
    /// <param name="childScope">Child scope to add to the current one.</param>
    void RegisterChildScope(IDeclarationScope childScope);

    /// <summary>
    /// Gets name of the virtual label which point to exit location from the scope.
    /// </summary>
    /// <returns>Name of the virtual label which can be used by break statement</returns>
    string? GetBreakLabel();
}
