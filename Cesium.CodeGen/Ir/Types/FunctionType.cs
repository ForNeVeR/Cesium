using Cesium.CodeGen.Contexts;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal record FunctionType(ParametersInfo? Parameters, IType ReturnType) : IType
{
    public TypeReference Resolve(TranslationUnitContext context) =>
        throw new NotImplementedException();

    public int SizeInBytes => throw new NotImplementedException("Could not calculate size yet.");
}
