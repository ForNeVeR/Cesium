using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;

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

    public Instruction GetConvertInstruction(AssemblyContext context)
    {
        if (UnderlyingType.FullName == TypeSystemEx.VoidPtrFullTypeName)
            return Instruction.Create(OpCodes.Call, context.VoidPtrConverter);

        if (UnderlyingType is GenericInstanceType typeInstance)
        {
            var parent = typeInstance.GetElementType();
            return parent.FullName switch
            {
                TypeSystemEx.CPtrFullTypeName =>
                    Instruction.Create(OpCodes.Call, context.CPtrConverter(typeInstance.GenericArguments.Single())),
                TypeSystemEx.FuncPtrFullTypeName =>
                    Instruction.Create(
                        OpCodes.Newobj,
                        context.FuncPtrConstructor(typeInstance.GenericArguments.Single())),
                _ => throw new AssertException($"No conversion available for interop type {parent}.")
            };
        }

        throw new AssertException(
            $"{nameof(InteropType)} doesn't know how to get a converter call for an underlying {UnderlyingType}.");
    }
}
