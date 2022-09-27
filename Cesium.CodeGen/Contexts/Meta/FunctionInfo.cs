using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;

namespace Cesium.CodeGen.Contexts.Meta;

internal record FunctionInfo(
    ParametersInfo? Parameters,
    IType ReturnType,
    MethodReference MethodReference,
    bool IsDefined = false)
{
    public void VerifySignatureEquality(string name, ParametersInfo? parameters, IType returnType)
    {
        if (!returnType.Equals(ReturnType))
            throw new CompilationException(
                $"Incorrect return type for function {name} declared as {ReturnType}: {returnType}.");

        var declaredWithVarargs = Parameters?.IsVarArg == true;
        var definedWithVarargs = parameters?.IsVarArg == true;
        if (declaredWithVarargs && !definedWithVarargs)
            throw new CompilationException(
                $"Function {name} declared with varargs but defined without varargs.");

        if (!declaredWithVarargs && definedWithVarargs)
            throw new CompilationException(
                $"Function {name} declared without varargs but defined with varargs.");

        if (declaredWithVarargs != definedWithVarargs)
            throw new CompilationException(
                $"Var arg declarations does not matched for functionn {name}.");

        var actualCount = parameters?.Parameters.Count ?? 0;
        var declaredCount = Parameters?.Parameters.Count ?? 0;
        if (actualCount != declaredCount)
            throw new CompilationException(
                $"Incorrect parameter count for function {name}: declared with {declaredCount} parameters, defined" +
                $"with {actualCount}.");

        var actualParams = parameters?.Parameters ?? Array.Empty<ParameterInfo>();
        var declaredParams = Parameters?.Parameters ?? Array.Empty<ParameterInfo>();
        foreach (var (a, b) in actualParams.Zip(declaredParams))
        {
            if (a != b)
                throw new CompilationException(
                    $"Incorrect type for parameter {a.Name}: declared as {b.Type}, defined as {a.Type}.");
        }
    }
}
