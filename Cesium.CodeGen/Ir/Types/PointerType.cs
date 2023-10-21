using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Cesium.CodeGen.Ir.Types;

internal record PointerType(IType Base) : IType
{
    public virtual TypeReference Resolve(TranslationUnitContext context)
    {
        if (Base is FunctionType ft)
            return ft.ResolvePointer(context);

        return Base.Resolve(context).MakePointerType();
    }

    public int? GetSizeInBytes(TargetArchitectureSet arch) =>
        arch switch
        {
            TargetArchitectureSet.Dynamic => null,
            TargetArchitectureSet.Bit32 => 4,
            TargetArchitectureSet.Bit64 => 8,
            _ => throw new AssertException($"Unknown architecture set: {arch}.")
        };

    public IExpression GetSizeInBytesExpression(TargetArchitectureSet arch)
    {
        var constSize = GetSizeInBytes(arch);
        if (constSize != null)
            return ConstantLiteralExpression.OfInt32(constSize.Value);

        if (arch != TargetArchitectureSet.Dynamic)
            throw new AssertException($"Architecture {arch} shouldn't enter dynamic pointer size calculation.");

        return new SizeOfOperatorExpression(this);
    }
}
