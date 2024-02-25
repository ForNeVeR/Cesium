using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

/// <remarks>
/// In contrary to how <see cref="AddressableValue"/> behaves, an LValueField's <see cref="EmitGetValue"/> should be
/// directed to <see cref="EmitGetAddress"/> for case of an inline array, because such a variable should behave as a pointer in most contexts.
/// </remarks>
internal abstract class LValueField : ILValue
{
    public abstract IType GetValueType();

    public void EmitGetValue(IEmitScope scope)
    {
        if (GetValueType() is InPlaceArrayType)
        {
            EmitGetAddress(scope);
            return;
        }

        EmitGetValueUnchecked(scope);
    }

    /// <remarks>This method doesn't have to check for in-place arrays.</remarks>
    private void EmitGetValueUnchecked(IEmitScope scope)
    {
        var field = GetField(scope);
        if (field.Resolve().IsStatic)
        {
            scope.LdSFld(field);
        }
        else
        {
            EmitGetFieldOwner(scope);
            if (field is StructType.AnonStructFieldReference unionField)
                unionField.EmitPath(scope);
            scope.LdFld(field);
        }
    }

    public void EmitGetAddress(IEmitScope scope)
    {
        var field = GetField(scope);
        if (field.Resolve().IsStatic)
        {
            if (GetValueType() is InPlaceArrayType)
            {
                // Special treatment of global in-place arrays: they are not fields of special type but mere pointers to
                // memory. As the field itself points to memory, we should just load it here, no need for lsflda.
                scope.LdSFld(field);
            }
            else
            {
                scope.LdSFldA(field);
            }
        }
        else
        {
            EmitGetFieldOwner(scope);
            scope.LdFldA(field);
        }
    }

    public void EmitSetValue(IEmitScope scope, IExpression value)
    {
        var field = GetField(scope);

        EmitGetFieldOwner(scope);
        if (field is StructType.AnonStructFieldReference unionField)
            unionField.EmitPath(scope);

        value.EmitTo(scope);
        if (value is CompoundInitializationExpression)
        {
            if (GetValueType() is not InPlaceArrayType type)
            {
                throw new CompilationException("Compound initialization is only supported for in-place arrays.");
            }

            scope.AddInstruction(OpCodes.Ldflda, field);
            var expression = type.GetSizeInBytesExpression(scope.AssemblyContext.ArchitectureSet);
            expression.EmitTo(scope);
            scope.AddInstruction(OpCodes.Conv_U);

            var initializeCompoundMethod = scope.Context.GetRuntimeHelperMethod("InitializeCompound");
            scope.AddInstruction(OpCodes.Call, initializeCompoundMethod);
        }
        else
        {
            EmitSetValueInstructionUnchecked(scope);
        }
    }

    /// <remarks>This method doesn't have to check for in-place arrays.</remarks>
    private void EmitSetValueInstructionUnchecked(IEmitScope scope)
    {
        var field = GetField(scope);
        if (field.Resolve().IsStatic)
        {
            scope.StSFld(field);
        }
        else
        {
            scope.StFld(field);
        }
    }

    protected abstract void EmitGetFieldOwner(IEmitScope scope);

    protected abstract FieldReference GetField(IEmitScope scope);
}
