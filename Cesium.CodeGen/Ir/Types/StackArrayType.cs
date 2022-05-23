using Cesium.CodeGen.Contexts;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Cesium.CodeGen.Ir.Types;

internal record StackArrayType(IType Base, int Size) : PointerType(Base)
{
    public override TypeReference Resolve(TranslationUnitContext context)
    {
        return Base.Resolve(context).MakePointerType();
    }

    public void EmitInitializer(IDeclarationScope scope)
    {
        if (Base is not PrimitiveType)
            throw new NotImplementedException($"Array of complex type specifiers aren't supported, yet: {Base}");

        var arraySizeInBytes = SizeInBytes;

        var method = scope.Method.Body.GetILProcessor();
        method.Emit(OpCodes.Ldc_I4, arraySizeInBytes);
        method.Emit(OpCodes.Conv_U);
        method.Emit(OpCodes.Localloc);
    }

    public override int SizeInBytes => Base.SizeInBytes * Size;
}