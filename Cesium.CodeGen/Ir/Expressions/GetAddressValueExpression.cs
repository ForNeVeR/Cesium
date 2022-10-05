using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal class GetAddressValueExpression : IExpression
{
    private readonly IAddressableValue _value;

    public GetAddressValueExpression(IAddressableValue value)
    {
        _value = value;
    }

    public IExpression Lower(IDeclarationScope scope) => this;

    public void EmitTo(IEmitScope scope)
    {
        _value.EmitGetAddress(scope);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Conv_U));
    }

    public IType GetExpressionType(IDeclarationScope scope) => _value.GetValueType().MakePointerType();
}
