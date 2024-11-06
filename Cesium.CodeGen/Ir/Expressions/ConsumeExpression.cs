using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class ConsumeExpression(IExpression expression) : IExpression
{
    public IExpression Lower(IDeclarationScope scope) => new ConsumeExpression(expression.Lower(scope));

    public IType GetExpressionType(IDeclarationScope scope)
    {
        return CTypeSystem.Void;
    }

    public void EmitTo(IEmitScope scope)
    {
        if (expression is SetValueExpression sv)
        {
            sv.NoReturn().EmitTo(scope);

            return;
        }

        expression.EmitTo(scope);
        var processor = scope.Method.Body.GetILProcessor();
        processor.Emit(OpCodes.Pop);
    }
}
