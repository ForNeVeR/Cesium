using System.Runtime.CompilerServices;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Cesium.CodeGen.Ir.Types;

internal record InPlaceArrayType(IType Base, int Size) : IType
{
    public TypeReference Resolve(TranslationUnitContext context)
    {
        TypeReference baseType = Base.Resolve(context);
        if (baseType.IsPointer)
        {
            return baseType;
        }

        return baseType.MakePointerType();
    }

    public FieldDefinition CreateFieldOfType(TranslationUnitContext context, TypeDefinition ownerType, string fieldName)
    {
        var arch = context.AssemblyContext.ArchitectureSet;
        var size = GetSizeInBytes(arch);
        if (size == null)
            throw new CompilationException(
                $"Cannot statically determine a size of type {this} for architecture set \"{arch}\". " +
                $"This size is required to generate a field \"{fieldName}\" inside of a type \"{ownerType}\".");

        var itemType = Base.Resolve(context);
        var bufferType = CreateFixedBufferType(context.Module, itemType, fieldName, size.Value);
        ownerType.NestedTypes.Add(bufferType);

        return new FieldDefinition(fieldName, FieldAttributes.Public, bufferType)
        {
            CustomAttributes = { GenerateCustomFieldAttribute() }
        };

        CustomAttribute GenerateCustomFieldAttribute()
        {
            var fixedBufferCtor = typeof(FixedBufferAttribute).GetConstructor(new[] { typeof(Type), typeof(int) });
            if (fixedBufferCtor == null)
                throw new AssertException(
                    "Cannot find a constructor with signature (Type, Int32) in type FixedBufferAttribute.");

            return new CustomAttribute(context.Module.ImportReference(fixedBufferCtor))
            {
                ConstructorArguments =
                {
                    new CustomAttributeArgument(context.Module.ImportReference(context.AssemblyContext.MscorlibAssembly.GetType("System.Type")), itemType),
                    new CustomAttributeArgument(context.TypeSystem.Int32, size.Value)
                }
            };
        }
    }

    public void EmitInitializer(IEmitScope scope)
    {
        var method = scope.Method.Body.GetILProcessor();
        var expression = GetSizeInBytesExpression(scope.AssemblyContext.ArchitectureSet);
        expression.EmitTo(scope);
        method.Emit(OpCodes.Conv_U);
        if (scope is GlobalConstructorScope)
        {
            var allocateGlobalFieldMethod = scope.Context.GetRuntimeHelperMethod("AllocateGlobalField");
            method.Emit(OpCodes.Call, allocateGlobalFieldMethod);
        }
        else
        {
            method.Emit(OpCodes.Localloc);
        }
    }

    public int? GetSizeInBytes(TargetArchitectureSet arch) =>
        Base.GetSizeInBytes(arch) * Size;

    public IExpression GetSizeInBytesExpression(TargetArchitectureSet arch)
    {
        var constSize = GetSizeInBytes(arch);
        if (constSize != null) return ConstantLiteralExpression.OfInt32(constSize.Value);

        return new ArithmeticBinaryOperatorExpression(
            Base.GetSizeInBytesExpression(arch),
            BinaryOperator.Multiply,
            ConstantLiteralExpression.OfInt32(Size)
        );
    }

    private static TypeDefinition CreateFixedBufferType(
        ModuleDefinition module,
        TypeReference fieldType,
        string fieldName,
        int sizeInBytes)
    {
        // An example of what C# does for fixed int x[20]:
        //
        // [StructLayout(LayoutKind.Sequential, Size = 80)]
        // [CompilerGenerated]
        // [UnsafeValueType]
        // public struct <x>e__FixedBuffer
        // {
        //     public int FixedElementField;
        // }

        var compilerGeneratedCtor = typeof(CompilerGeneratedAttribute).GetConstructor(Array.Empty<Type>());
        var compilerGeneratedAttribute = new CustomAttribute(module.ImportReference(compilerGeneratedCtor));

        var unsafeValueTypeCtor = typeof(UnsafeValueTypeAttribute).GetConstructor(Array.Empty<Type>());
        var unsafeValueTypeAttribute = new CustomAttribute(module.ImportReference(unsafeValueTypeCtor));

        return new TypeDefinition(
            "",
            $"<SyntheticBuffer>{fieldName}",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.NestedPublic,
            module.ImportReference(typeof(ValueType)))
        {
            PackingSize = 0,
            ClassSize = sizeInBytes,
            CustomAttributes = { compilerGeneratedAttribute, unsafeValueTypeAttribute },
            Fields = { new FieldDefinition("FixedElementField", FieldAttributes.Public, fieldType) }
        };
    }
}
