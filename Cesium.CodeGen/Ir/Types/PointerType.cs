using Cesium.CodeGen.Contexts;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Cesium.CodeGen.Ir.Types;

internal record PointerType(IType Base) : IType
{
    public virtual TypeReference Resolve(TranslationUnitContext context) => Base.Resolve(context).MakePointerType();

    public virtual int SizeInBytes => throw new NotImplementedException("Could not calculate size yet.");

    // explicit impl while Size not implemented
    public override string ToString()
        => $"PointerType {{ Base = {Base} }}";
}