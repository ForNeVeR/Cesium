using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Cesium.CodeGen.Ir.Types;

internal sealed class PointerType : IType, IEquatable<PointerType>
{
    /// <inheritdoc />
    public TypeKind TypeKind => TypeKind.Pointer;

    public IType Base { get; }

    public PointerType(IType @base)
    {
        this.Base = @base;
    }

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

    /// <inheritdoc />
    public override bool Equals(object? other)
    {
        if (other is PointerType)
        {
            return Equals((PointerType)other);
        }
        return false;
    }

    /// <inheritdoc />
    public bool Equals(PointerType? other)
    {
        if (other is null) return false;
        if (TypeKind != other.TypeKind) return false;
        if (Base is StructType baseStructType && other.Base is StructType otherStructType)
        {
            return baseStructType.Identifier == otherStructType.Identifier
                && baseStructType.Members.Count == otherStructType.Members.Count;
        }

        return this.Base.Equals(other.Base);
    }

    public override int GetHashCode()
    {
        var hash = 123123 ^ (int)TypeKind;
        if (Base is StructType baseStructType)
        {
            return hash ^ (baseStructType.Identifier?.GetHashCode() ?? 0) ^ baseStructType.Members.Count;
        }

        return hash ^ Base.GetHashCode();
    }
}
