using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

/// <summary>Either an assembly-global or a file-level ("static") variable.</summary>
internal record GlobalVariableDefinition(
    StorageClass StorageClass,
    IType Type,
    string Identifier,
    IExpression? Initializer) : IBlockItem
{
    public IBlockItem Lower(IDeclarationScope scope)
    {
        scope.AddVariable(StorageClass, Identifier, Type);

        return this with { Initializer = Initializer?.Lower(scope) };
    }

    public void EmitTo(IEmitScope scope)
    {
        var field = scope.ResolveGlobalField(Identifier);
        if (Initializer != null)
        {
            if (Type is InPlaceArrayType arrayType && Initializer is CompoundInitializationExpression)
            {
                arrayType.EmitInitializer(scope);
                scope.StSFld(field);
                Initializer.EmitTo(scope);
                // for compound initialization copy memory.s
                scope.AddInstruction(OpCodes.Ldsflda, field);
                var expression = arrayType.GetSizeInBytesExpression(scope.AssemblyContext.ArchitectureSet);
                expression.EmitTo(scope);
                scope.AddInstruction(OpCodes.Conv_U);

                var initializeCompoundMethod = scope.Context.GetRuntimeHelperMethod("InitializeCompound");
                scope.AddInstruction(OpCodes.Call, initializeCompoundMethod);
            }
            else
            {
                Initializer.EmitTo(scope);
                scope.StSFld(field);
            }
        }
        else if (Type is InPlaceArrayType arrayType)
        {
            arrayType.EmitInitializer(scope);
            scope.StSFld(field);
        }
    }

    public bool TryUnsafeSubstitute(IBlockItem original, IBlockItem replacement) => false;
}
