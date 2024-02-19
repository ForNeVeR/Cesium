using Cesium.CodeGen.Contexts;
using Cesium.Core;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal record FunctionType(ParametersInfo? Parameters, IType ReturnType) : IType
{
    /// <inheritdoc />
    public TypeKind TypeKind => TypeKind.FunctionType;

    public TypeReference Resolve(TranslationUnitContext context) =>
        throw new AssertException($"Function type {this} cannot be directly expressed in the byte code.");

    /// <summary>Resolves a delegate type corresponding to this function's signature.</summary>
    /// <remarks>
    /// Most useful for interop, since every function gets resolved to a <see cref="Func{TResult}"/> or an
    /// <see cref="Action{T}"/> corresponding to it.
    /// </remarks>
    public TypeReference ResolveAsDelegateType(TranslationUnitContext context)
    {
        var returnType = ReturnType.Resolve(context);

        if (Parameters is null)
            throw new CompilationException("Function parameters should not be null.");

        var (parameterInfos, isVoid, isVarArg) = Parameters;
        if (isVarArg)
            throw new WipException(487, $"A vararg function is not implemented, yet: {this}.");

        if (parameterInfos.Count == 0 && !isVoid)
            throw new WipException(487, $"A function with an empty parameter list is not implemented, yet: {this}.");

        var arguments = parameterInfos.Select(p => p.Type.Resolve(context));
        return context.AssemblyContext.StandardDelegateType(returnType, arguments);
    }

    public TypeReference ResolvePointer(TranslationUnitContext context)
    {
        var pointer = new FunctionPointerType
        {
            ReturnType = ReturnType.Resolve(context)
        };

        if (Parameters is var (parameterInfos, isVoid, isVarArg))
        {
            if (isVoid && (parameterInfos.Count > 0 || isVarArg))
                throw new CompilationException(
                    $"Invalid function pointer type {this}: declared as void " +
                    "but has parameters or declared as vararg.");

            if (isVarArg)
                throw new WipException(196, $"A pointer to a vararg function is not implemented, yet: {this}.");

            foreach (var (type, name, index) in parameterInfos)
            {
                pointer.Parameters.Add(new ParameterDefinition(type.Resolve(context))
                {
                    Name = name
                });
            }
        }

        return pointer;
    }

    public int? GetSizeInBytes(TargetArchitectureSet arch) =>
        throw new AssertException($"Function type {this} has no defined size.");

    public override string ToString() =>
        $"FunctionType {{ {nameof(Parameters)} = {Parameters}, {nameof(ReturnType)} = {ReturnType} }}";
}
