using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal class ConsumeExpression : IExpression
{
    private readonly IExpression _expression;

    public ConsumeExpression(IExpression expression)
    {
        _expression = expression;
    }

    public IExpression Lower(IDeclarationScope scope) => this;

    public IType GetExpressionType(IDeclarationScope scope)
    {
        return scope.CTypeSystem.Void;
    }

    public void EmitTo(IEmitScope scope)
    {
        _expression.EmitTo(scope);
        var processor = scope.Method.Body.GetILProcessor();
        processor.Emit(OpCodes.Pop);
    }
}
