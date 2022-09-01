using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;

namespace Cesium.CodeGen.Contexts;

internal interface IDeclarationScope
{

    CTypeSystem CTypeSystem { get; }
    IReadOnlyDictionary<string, FunctionInfo> Functions { get; }
    IReadOnlyDictionary<string, IType> Variables { get; }
    IReadOnlyDictionary<string, IType> GlobalFields { get; }
    void AddVariable(string identifier, IType variable);
    ParameterInfo? GetParameterInfo(string name);
}
