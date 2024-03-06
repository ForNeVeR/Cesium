using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;

namespace Cesium.CodeGen.Contexts.Meta;

// TODO[#489]: This is confusing, make immutable.
internal record FunctionInfo(
    ParametersInfo? Parameters,
    IType ReturnType,
    StorageClass StorageClass,
    bool IsDefined,
    MethodReference? MethodReference = null) : ICloneable
{
    public ParametersInfo? Parameters { get; private set; } = Parameters;
    public IType ReturnType { get; private set; } = ReturnType;
    public StorageClass StorageClass { get; private set; } = StorageClass;
    public bool IsDefined { get; private set; } = IsDefined;
    public MethodReference? MethodReference { get; private set; } = MethodReference;
    public string? CliImportMember { get; init; }

    private FunctionInfo() : this(null, null!, default, false) {}

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
            if (!a.Type.IsEqualTo(b.Type))
                throw new CompilationException(
                    $"Incorrect type for parameter {a.Name}: declared as {b.Type}, defined as {a.Type}.");
        }
    }

    object ICloneable.Clone()
        => new FunctionInfo
        {
            Parameters = Parameters,
            IsDefined = IsDefined,
            StorageClass = StorageClass,
            CliImportMember = CliImportMember,
            ReturnType = ReturnType,
            MethodReference = MethodReference
        };

    internal FunctionInfo ShallowClone => (FunctionInfo)((ICloneable)this).Clone();

    internal class FunctionInfoBuilder
    {
        private readonly FunctionInfo _functionInfo;

        private FunctionInfoBuilder(FunctionInfo functionInfo)
            => _functionInfo = functionInfo.ShallowClone;

        internal static FunctionInfoBuilder ToBuild(FunctionInfo functionInfo) => new(functionInfo);

        internal FunctionInfoBuilder Parameters(ParametersInfo? parameterInfo)
        {
            _functionInfo.Parameters = parameterInfo;
            return this;
        }
        internal FunctionInfoBuilder ReturnType(IType parameterInfo)
        {
            _functionInfo.ReturnType = parameterInfo;
            return this;
        }
        internal FunctionInfoBuilder StorageClass(StorageClass storageClass)
        {
            _functionInfo.StorageClass = storageClass;
            return this;
        }
        internal FunctionInfoBuilder IsDefined(bool isDefined)
        {
            _functionInfo.IsDefined = isDefined;
            return this;
        }
        internal FunctionInfoBuilder MethodReference(MethodReference? methodReference)
        {
            _functionInfo.MethodReference = methodReference;
            return this;
        }

        internal FunctionInfo Build() => _functionInfo;
    }
}
