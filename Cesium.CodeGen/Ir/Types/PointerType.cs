using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Cesium.CodeGen.Ir.Types;

internal sealed record PointerType(IType Base) : IType
{
    public static int? SizeInBytes(TargetArchitectureSet arch) => arch switch
    {
        TargetArchitectureSet.Dynamic => null,
        TargetArchitectureSet.Bit32 => 4,
        TargetArchitectureSet.Bit64 => 8,
        TargetArchitectureSet.Wide => 8,
        _ => throw new AssertException($"Unknown architecture set: {arch}.")
    };

    public TypeReference Resolve(TranslationUnitContext context)
    {
        if (Base is InPlaceArrayType)
            throw new CompilationException("Cannot resolve a pointer to an inline array.");

        if (Base is FunctionType ft)
            return ft.ResolvePointer(context);

        return Base.Resolve(context).MakePointerType();
    }

    public TypeReference ResolveForTypeMember(TranslationUnitContext context) =>
        context.AssemblyContext.ArchitectureSet switch
        {
            TargetArchitectureSet.Wide => Base switch
            {
                FunctionType rawFunc => context.AssemblyContext.RuntimeFuncPtr(rawFunc.ResolveAsDelegateType(context)),
                PrimitiveType { Kind: PrimitiveTypeKind.Void } => context.AssemblyContext.RuntimeVoidPtr,
                _ => context.AssemblyContext.RuntimeCPtr(Base.ResolveForTypeMember(context)),
            },
            _ => Resolve(context)
        };

    public int? GetSizeInBytes(TargetArchitectureSet arch) => SizeInBytes(arch);

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
