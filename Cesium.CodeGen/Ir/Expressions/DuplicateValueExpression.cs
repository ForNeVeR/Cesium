using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions.Values;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal class DuplicateValueExpression : IExpression
{
    private IValue _value;

    internal DuplicateValueExpression(IValue value)
    {
        _value = value;
    }

    public IExpression Lower(IDeclarationScope scope) => this;

    public void EmitTo(IEmitScope scope)
    {
        _value.EmitGetValue(scope);
        var processor = scope.Method.Body.GetILProcessor();
        processor.Emit(OpCodes.Pop);
    }

    public IType GetExpressionType(IDeclarationScope scope) => _value.GetValueType();
}
