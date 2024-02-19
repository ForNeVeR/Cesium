using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class ConsumeExpression : IExpression
{
    private readonly IExpression _expression;

    public ConsumeExpression(IExpression expression)
    {
        _expression = expression;
    }

    public IExpression Lower(IDeclarationScope scope) => new ConsumeExpression(_expression.Lower(scope));

    public IType GetExpressionType(IDeclarationScope scope)
    {
        return CTypeSystem.Void;
    }

    public void EmitTo(IEmitScope scope)
    {
        if (_expression is SetValueExpression sv)
        {
            sv.NoReturn().EmitTo(scope);

            return;
        }

        _expression.EmitTo(scope);
        var processor = scope.Method.Body.GetILProcessor();
        processor.Emit(OpCodes.Pop);
    }
}
