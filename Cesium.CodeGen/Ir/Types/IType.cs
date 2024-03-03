using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.Core;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Types;

internal enum TypeKind
{
    Unresolved,
    PrimitiveType,
    Enum,
    Struct,
    Union,
    FunctionType,
    InPlaceArray,
    Pointer,
    Const,
    InteropType,
}

/// <summary>An interface representing a C type.</summary>
/// <remarks>
/// Can be of two flavors: an <see cref="IGeneratedType"/> or a plain type that doesn't require any byte code to be
/// generated (a basic type, a pointer or a function pointer.
/// </remarks>
internal interface IType
{
    TypeReference Resolve(TranslationUnitContext context);

    /// <summary>
    /// Gets kind of a type.
    /// </summary>
    TypeKind TypeKind { get; }

    /// <remarks>
    /// For cases when a type gets resolved differently for a type member context. For example, a pointer will
    /// recursively get resolved as a <c>CPtr</c> on a wide architecture.
    /// </remarks>
    TypeReference ResolveForTypeMember(TranslationUnitContext context) => Resolve(context);

    FieldDefinition CreateFieldOfType(TranslationUnitContext context, TypeDefinition ownerType, string fieldName) =>
        new(fieldName, FieldAttributes.Public, ResolveForTypeMember(context));

    /// <summary>Determines the size of an object of this type in bytes, if possible.</summary>
    /// <param name="arch">Target architecture set.</param>
    /// <returns>The size if it was possible to determine. <c>null</c> otherwise.</returns>
    /// <remarks>
    /// <para>
    ///     Certain types (mostly pointers and derivatives) may not be able to determine their size in dynamic
    ///     architecture.
    /// </para>
    /// <para>
    ///     In this case, only <see cref="GetSizeInBytesExpression"/> would be available to get the size dynamically.
    /// </para>
    /// </remarks>
    int? GetSizeInBytes(TargetArchitectureSet arch);

    /// <summary>
    /// Generates an expression to determine the type's size in bytes in runtime. This expression will leave the only
    /// value of size (32-bit integer) on the execution stack.
    /// </summary>
    /// <remarks>For simple types, will emit a constant.</remarks>
    IExpression GetSizeInBytesExpression(TargetArchitectureSet arch)
    {
        int size = GetSizeInBytes(arch) ?? throw new AssertException(
                $"Cannot determine static size of type {this}, " +
                $"and {nameof(GetSizeInBytesExpression)} method for dynamic calculation is not overridden.");
        return new ConstantLiteralExpression(new IntegerConstant(size));
    }
}

/// <summary>A generated type, i.e. a type that has some bytecode to be generated once.</summary>
internal interface IGeneratedType : IType
{
    TypeDefinition StartEmit(string name, TranslationUnitContext context);
    void FinishEmit(TypeDefinition definition, string name, TranslationUnitContext context);

    public bool IsAlreadyEmitted(TranslationUnitContext context);

    /// <summary>Fully emits the type.</summary>
    void EmitType(TranslationUnitContext context);
}
