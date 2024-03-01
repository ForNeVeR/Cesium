using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

/// <summary>Takes an address of an instance of the passed type as part of the offsetof expression.</summary>
internal sealed class InstanceForOffsetOfExpression : AddressableValue, IValueExpression
{
    private readonly StructType _resolvedType;

    public InstanceForOffsetOfExpression(StructType resolvedType)
    {
        _resolvedType = resolvedType;
    }

    public IExpression Lower(IDeclarationScope scope) => this;

    public void EmitTo(IEmitScope scope)
    {
        var typeRef = _resolvedType.Resolve(scope.Context);

        VariableDefinition var;

        if (scope.Method.Body.Variables.FirstOrDefault(v => v.VariableType.IsEqualTo(typeRef)) is
            { } existingVar)
        {
            var = existingVar;
        }
        else
        {
            var = new VariableDefinition(typeRef);
            scope.Method.Body.Variables.Add(var);
        }

        scope.AddInstruction(OpCodes.Ldloca, var);
    }

    public IType GetExpressionType(IDeclarationScope scope) => CTypeSystem.NativeInt;

    public IValue Resolve(IDeclarationScope scope) => this;

    public override void EmitGetValue(IEmitScope scope) => throw new NotSupportedException();

    public override IType GetValueType() => _resolvedType;

    protected override void EmitGetAddressUnchecked(IEmitScope scope) => EmitTo(scope);
}
