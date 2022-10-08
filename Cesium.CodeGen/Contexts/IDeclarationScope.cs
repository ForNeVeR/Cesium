using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Contexts;

internal interface IDeclarationScope
{
    CTypeSystem CTypeSystem { get; }
    IReadOnlyDictionary<string, FunctionInfo> Functions { get; }
    IReadOnlyDictionary<string, IType> Variables { get; }
    IReadOnlyDictionary<string, IType> GlobalFields { get; }
    void AddVariable(string identifier, IType variable);
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
}
