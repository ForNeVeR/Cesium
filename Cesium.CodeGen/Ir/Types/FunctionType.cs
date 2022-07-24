using Cesium.CodeGen.Contexts;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal record FunctionType(ParametersInfo? Parameters, IType ReturnType) : IType
{
    public TypeReference Resolve(TranslationUnitContext context) =>
        throw new NotSupportedException($"Function type {this} cannot be directly expressed in the byte code.");

    public TypeReference ResolvePointer(TranslationUnitContext context)
    {
        var pointer = new FunctionPointerType
        {
            ReturnType = ReturnType.Resolve(context)
        };

        if (Parameters is var (parameterInfos, isVoid, isVarArg))
        {
            if (isVoid && (parameterInfos.Count > 0 || isVarArg))
                throw new NotSupportedException(
                    $"Invalid function pointer type {this}: declared as void " +
                    "but has parameters or declared as vararg.");

            if (isVarArg)
                throw new NotImplementedException($"A pointer to a vararg function is not implemented, yet: {this}.");

            foreach (var (type, name) in parameterInfos)
            {
                pointer.Parameters.Add(new ParameterDefinition(type.Resolve(context))
                {
                    Name = name
                });
            }
        }

        return pointer;
    }

    public int SizeInBytes => throw new NotSupportedException($"Function type {this} has no defined size.");

    public override string ToString() =>
        $"FunctionType {{ {nameof(Parameters)} = {Parameters}, {nameof(ReturnType)} = {ReturnType} }}";
}
