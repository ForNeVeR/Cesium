using System.Runtime.CompilerServices;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
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
        var itemType = Base.Resolve(context);
        var bufferType = CreateFixedBufferType(context.Module, itemType, fieldName);
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
                    new CustomAttributeArgument(context.TypeSystem.Int32, SizeInBytes)
                }
            };
        }
    }

    public void EmitInitializer(IEmitScope scope)
    {
        var arraySizeInBytes = SizeInBytes;

        var method = scope.Method.Body.GetILProcessor();
        method.Emit(OpCodes.Ldc_I4, arraySizeInBytes);
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

    public int SizeInBytes => Base.SizeInBytes * Size;

    private TypeDefinition CreateFixedBufferType(
        ModuleDefinition module,
        TypeReference fieldType,
        string fieldName)
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
            ClassSize = SizeInBytes,
            CustomAttributes = { compilerGeneratedAttribute, unsafeValueTypeAttribute },
            Fields = { new FieldDefinition("FixedElementField", FieldAttributes.Public, fieldType) }
        };
    }
}
