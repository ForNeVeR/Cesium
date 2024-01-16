using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Cesium.CodeGen.Ir.Types;

internal sealed record InPlaceArrayType(IType Base, int Size) : IType
{
    public TypeReference Resolve(TranslationUnitContext context) => Base switch
    {
        InPlaceArrayType => Base.Resolve(context),
        _ => Base.Resolve(context).MakePointerType()
    };

    public FieldDefinition CreateFieldOfType(TranslationUnitContext context, TypeDefinition ownerType, string fieldName)
    {
        var arch = context.AssemblyContext.ArchitectureSet;
        int size = GetSizeInBytes(arch) ?? throw new CompilationException(
                $"Cannot statically determine a size of type {this} for architecture set \"{arch}\". " +
                $"This size is required to generate a field \"{fieldName}\" inside of a type \"{ownerType}\".");
        var itemType = Base.Resolve(context);
        var bufferType = CreateFixedBufferType(context, itemType, fieldName, size);
        ownerType.NestedTypes.Add(bufferType);

        return new FieldDefinition(fieldName, FieldAttributes.Public, bufferType)
        {
            CustomAttributes = { GenerateCustomFieldAttribute() }
        };

        CustomAttribute GenerateCustomFieldAttribute()
        {
            var typeType = context.Module.ImportReference(context.AssemblyContext.MscorlibAssembly.GetType("System.Type"));
            var fixedBufferAttributeType = context.AssemblyContext.MscorlibAssembly.GetType("System.Runtime.CompilerServices.FixedBufferAttribute") ?? throw new AssertException(
                    "Cannot find a type System.Runtime.CompilerServices.FixedBufferAttribute.");
            var fixedBufferCtor = new MethodReference(".ctor", context.TypeSystem.Void, fixedBufferAttributeType);
            fixedBufferCtor.Parameters.Add(new ParameterDefinition(typeType));
            fixedBufferCtor.Parameters.Add(new ParameterDefinition(context.TypeSystem.Int32));

            return new CustomAttribute(context.Module.ImportReference(fixedBufferCtor))
            {
                ConstructorArguments =
                {
                    new CustomAttributeArgument(typeType, itemType),
                    new CustomAttributeArgument(context.TypeSystem.Int32, size)
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

        return new BinaryOperatorExpression(
            Base.GetSizeInBytesExpression(arch),
            BinaryOperator.Multiply,
            ConstantLiteralExpression.OfInt32(Size)
        );
    }

    private static TypeDefinition CreateFixedBufferType(
        TranslationUnitContext context,
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

        ModuleDefinition module = context.Module;
        var compilerGeneratedAttributeType = context.AssemblyContext.MscorlibAssembly.GetType("System.Runtime.CompilerServices.CompilerGeneratedAttribute") ?? throw new AssertException(
                "Cannot find a type System.Runtime.CompilerServices.CompilerGeneratedAttribute.");
        var compilerGeneratedCtor = new MethodReference(".ctor", context.TypeSystem.Void, compilerGeneratedAttributeType);
        var compilerGeneratedAttribute = new CustomAttribute(module.ImportReference(compilerGeneratedCtor));

        var unsafeValueTypeAttributeType = context.AssemblyContext.MscorlibAssembly.GetType("System.Runtime.CompilerServices.UnsafeValueTypeAttribute") ?? throw new AssertException(
                "Cannot find a type System.Runtime.CompilerServices.UnsafeValueTypeAttribute.");
        var unsafeValueTypeCtor = new MethodReference(".ctor", context.TypeSystem.Void, unsafeValueTypeAttributeType);
        var unsafeValueTypeAttribute = new CustomAttribute(module.ImportReference(unsafeValueTypeCtor));

        return new TypeDefinition(
            "",
            $"<SyntheticBuffer>{fieldName}",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.NestedPublic,
            module.ImportReference(context.AssemblyContext.MscorlibAssembly.GetType("System.ValueType")))
        {
            PackingSize = 0,
            ClassSize = sizeInBytes,
            CustomAttributes = { compilerGeneratedAttribute, unsafeValueTypeAttribute },
            Fields = { new FieldDefinition("FixedElementField", FieldAttributes.Public, fieldType) }
        };
    }
}
