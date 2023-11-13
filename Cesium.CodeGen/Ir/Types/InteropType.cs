using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.Core;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

/// <summary>
/// Type that was imported from CLI for Cesium/CLI interop, most likely via <code>__cli_import</code>.
/// </summary>
internal record InteropType(TypeReference UnderlyingType) : IType
{
    public TypeReference Resolve(TranslationUnitContext context) => UnderlyingType;

    public int? GetSizeInBytes(TargetArchitectureSet arch)
    {
        switch (UnderlyingType)
        {
            case { FullName: TypeSystemEx.VoidPtrFullTypeName }:
                return PointerType.SizeInBytes(arch);
            case { IsGenericInstance: true }:
            {
                var parent = UnderlyingType.GetElementType();

                if (parent.FullName is TypeSystemEx.CPtrFullTypeName or TypeSystemEx.FuncPtrFullTypeName)
                    return PointerType.SizeInBytes(arch);
                break;
            }
        }

        throw new AssertException(
            $"{nameof(InteropType)} doesn't know how to get size of an underlying {UnderlyingType}.");
    }

    public MethodReference GetConvertCall(AssemblyContext context)
    {
        if (UnderlyingType.FullName == TypeSystemEx.VoidPtrFullTypeName)
            return context.VoidPtrConverter;

        if (UnderlyingType is GenericInstanceType typeInstance)
        {
            var parent = typeInstance.GetElementType();
            if (parent.FullName == TypeSystemEx.CPtrFullTypeName)
                return context.CPtrConverter(typeInstance.GenericArguments.Single());

            throw new WipException(WipException.ToDo, "Cannot import the converter method for FuncPtr, yet.");
        }

        throw new AssertException(
            $"{nameof(InteropType)} doesn't know how to get a converter call for an underlying {UnderlyingType}.");
    }
}
