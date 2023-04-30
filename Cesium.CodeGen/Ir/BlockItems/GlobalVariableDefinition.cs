using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

/// <summary>Either an assembly-global or a file-level ("static") variable.</summary>
internal record GlobalVariableDefinition(
    StorageClass StorageClass,
    IType Type,
    string Identifier,
    IExpression? Initializer) : IBlockItem
{
    public List<IBlockItem>? NextNodes { get; set; }
    public IBlockItem? Parent { get; set; }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        return this with { Initializer = Initializer?.Lower(scope) };
    }

    public void EmitTo(IEmitScope scope)
    {
        switch (StorageClass)
        {
            case StorageClass.Static: // file-level
                scope.Context.AddTranslationUnitLevelField(Identifier, Type);
                break;
            case StorageClass.Auto: // assembly-level
                scope.AssemblyContext.AddAssemblyLevelField(Identifier, Type);
                break;
            default:
                throw new CompilationException($"Global variable of storage class {StorageClass} is not supported.");
        }

        var field = scope.ResolveGlobalField(Identifier);
        if (Initializer != null)
        {
            Initializer.EmitTo(scope);
            scope.StSFld(field);
        }
        else if (Type is InPlaceArrayType arrayType)
        {
            arrayType.EmitInitializer(scope);
            scope.StSFld(field);
        }
    }
}
