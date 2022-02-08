using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;

namespace Cesium.CodeGen.Contexts.Meta;

internal record FunctionInfo(
    ParametersInfo? Parameters,
    IType ReturnType,
    MethodReference MethodReference,
    bool IsDefined = false);
