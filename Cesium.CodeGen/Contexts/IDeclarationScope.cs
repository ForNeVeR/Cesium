using Cesium.Ast;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Contexts;

internal interface IDeclarationScope
{
    TargetArchitectureSet ArchitectureSet { get; }
    FunctionInfo? GetFunctionInfo(string identifier);
    void DeclareFunction(string identifier, FunctionInfo functionInfo);
    VariableInfo? GetGlobalField(string identifier);
    void AddVariable(StorageClass storageClass, string identifier, IType variable, IExpression? constant);
    VariableInfo? GetVariable(string identifier);

    /// <summary>
    /// Recursively resolve the passed type and all its members, replacing `NamedType` in any points with their actual instantiations in the current context.
    /// </summary>
    /// <param name="type">Type which should be resolved.</param>
    /// <returns>A <see cref="IType"/> which fully resolves.</returns>
    /// <exception cref="CompilationException">Throws a <see cref="CompilationException"/> if it's not possible to resolve some of the types.</exception>
    IType ResolveType(IType type);
    IType? TryGetType(string identifier);
    void AddTypeDefinition(string identifier, IType type);
    void AddTagDefinition(string identifier, IType type);
    ParameterInfo? GetParameterInfo(string name);

    /// <summary>
    /// Adds label to the scope.
    /// </summary>
    /// <param name="identifier">Label to add to the current scope.</param>
    void AddLabel(string identifier);

    /// <summary>
    /// Gets name of the virtual label which point to exit location from the scope.
    /// </summary>
    /// <returns>Name of the virtual label which can be used by break statement</returns>
    string? GetBreakLabel();

    /// <summary>
    /// Gets name of the virtual label which point to loop check location.
    /// </summary>
    /// <returns>Name of the virtual label which can be used by continue statement</returns>
    string? GetContinueLabel();

    /// <summary>
    /// Push "pragma" to the internal stack
    /// </summary>
    void PushPragma(IPragma pragma);

    /// <summary>
    /// Gets "pragma" from the internal stack
    /// </summary>
    T? GetPragma<T>() where T : IPragma;

    /// <summary>
    /// Removes "pragma" from the internal stack
    /// </summary>
    void RemovePragma<T>(Predicate<T> predicate) where T : IPragma;

    List<SwitchCase>? SwitchCases { get; }
}
