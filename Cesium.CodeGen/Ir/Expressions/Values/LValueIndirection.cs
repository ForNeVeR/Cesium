// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Values;

/// <summary>Indirect store or load: will assign a value by pointer of some kind.</summary>
internal sealed class LValueIndirection : ILValue
{
    private readonly PointerType _pointerType;

    internal IExpression PointerExpression { get; }

    public LValueIndirection(IExpression expression, PointerType pointerType)
    {
        PointerExpression = expression;
        _pointerType = pointerType;
    }

    public void EmitGetValue(IEmitScope scope)
    {
        PointerExpression.EmitTo(scope);
        var (load, _) = GetOpcodes(_pointerType, scope.Context);
        scope.Method.Body.Instructions.Add(load);
    }

    public void EmitGetAddress(IEmitScope scope) => PointerExpression.EmitTo(scope);

    public void EmitSetValue(IEmitScope scope, IExpression value)
    {
        PointerExpression.EmitTo(scope);
        value.EmitTo(scope);
        var (_, maybeStore) = GetOpcodes(_pointerType, scope.Context);
        if (maybeStore is not {} store)
            throw new CompilationException($"Type {_pointerType} doesn't support the store operation.");

        scope.Method.Body.Instructions.Add(maybeStore);
    }

    public IType GetValueType() => _pointerType.Base;

    internal static (Instruction load, Instruction? store) GetOpcodes(PointerType pointerType, TranslationUnitContext context)
    {
        var baseType = SimplifyBaseType(pointerType.Base);
        return baseType switch
        {
            PrimitiveType primitiveType => (Instruction.Create(PrimitiveTypeInfo.Opcodes[primitiveType.Kind].load), Instruction.Create(PrimitiveTypeInfo.Opcodes[primitiveType.Kind].store)),
            PointerType => (Instruction.Create(OpCodes.Ldind_I), Instruction.Create(OpCodes.Stind_I)),
            InPlaceArrayType => (Instruction.Create(OpCodes.Ldind_I), null),
            StructType => (Instruction.Create(OpCodes.Ldobj, baseType.Resolve(context)), Instruction.Create(OpCodes.Stobj, baseType.Resolve(context))),
            _ => throw new WipException(256, $"Unsupported type for indirection operator: {pointerType}")
    };
    }

    private static IType SimplifyBaseType(IType type) => type switch
    {
        ConstType constType => constType.Base,
        _ => type
    };
}
