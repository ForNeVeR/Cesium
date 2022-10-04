using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal class ConditionalExpression : IExpression
{
    private readonly IExpression _condition;
    private readonly IExpression _trueExpression;
    private readonly IExpression _falseExpression;

    private ConditionalExpression(IExpression condition, IExpression trueExpression, IExpression falseExpression)
    {
        _condition = condition;
        _trueExpression = trueExpression;
        _falseExpression = falseExpression;
    }

    public ConditionalExpression(Ast.ConditionalExpression expression)
    {
        _condition = expression.Condition.ToIntermediate();
        _trueExpression = expression.TrueExpression.ToIntermediate();
        _falseExpression = expression.FalseExpression.ToIntermediate();
    }

    public IExpression Lower(IDeclarationScope scope)
    {
        var condition = _condition.Lower(scope);
        var conditionType = _condition.GetExpressionType(scope);

        if (!(scope.CTypeSystem.IsNumeric(conditionType) || conditionType is PointerType))
        {
            throw new CompilationException("Conditional expression must have a condition of scalar type.");
        }

        var trueExpression = _trueExpression.Lower(scope);
        var trueExpressionType = trueExpression.GetExpressionType(scope);

        var falseExpression = _falseExpression.Lower(scope);
        var falseExpressionType = falseExpression.GetExpressionType(scope);

        // Check if both expressions are compatible and convert them to the same type if needed.
        if (scope.CTypeSystem.IsNumeric(trueExpressionType) &&
            scope.CTypeSystem.IsNumeric(falseExpressionType))
        {
            // Both operands have arithmetic type. Convert them to the same type by usual arithmetic conversions.
            var commonType = scope.CTypeSystem.GetCommonNumericType(trueExpressionType, falseExpressionType);
            if (!trueExpressionType.IsEqualTo(commonType))
            {
                trueExpression = new TypeCastExpression(commonType, trueExpression).Lower(scope);
            }

            if (!falseExpressionType.IsEqualTo(commonType))
            {
                falseExpression = new TypeCastExpression(commonType, falseExpression).Lower(scope);
            }
        }
        else if (trueExpressionType.IsEqualTo(scope.CTypeSystem.Void) &&
                 falseExpressionType.IsEqualTo(scope.CTypeSystem.Void))
        {
            // Both operands have void type. No conversion is needed.
        }
        else
        {
            // TODO[#208]: Support operands of same struct or union type; pointers to compatible types;
            // pointer and null pointer constant; pointer to an object type and pointer to void.
            throw new WipException(208, "Conditional expression with this type is not supported yet.");
        }

        return new ConditionalExpression(condition, trueExpression, falseExpression);
    }

    public void EmitTo(IEmitScope scope)
    {
        var bodyProcessor = scope.Method.Body.GetILProcessor();

        _condition.EmitTo(scope);

        var falseLabel = bodyProcessor.Create(OpCodes.Nop);
        bodyProcessor.Emit(OpCodes.Brfalse, falseLabel);

        _trueExpression.EmitTo(scope);

        var endLabel = bodyProcessor.Create(OpCodes.Nop);
        bodyProcessor.Emit(OpCodes.Br, endLabel);

        bodyProcessor.Append(falseLabel);
        _falseExpression.EmitTo(scope);

        bodyProcessor.Append(endLabel);
    }

    public IType GetExpressionType(IDeclarationScope scope)
    {
        var trueExpressionType = _trueExpression.GetExpressionType(scope);
        var falseExpressionType = _falseExpression.GetExpressionType(scope);

        // Arithmetic types.
        if (scope.CTypeSystem.IsNumeric(trueExpressionType) &&
            scope.CTypeSystem.IsNumeric(falseExpressionType))
        {
            return scope.CTypeSystem.GetCommonNumericType(trueExpressionType, falseExpressionType);
        }

        // Void types.
        if (trueExpressionType.IsEqualTo(scope.CTypeSystem.Void) &&
            falseExpressionType.IsEqualTo(scope.CTypeSystem.Void))
        {
            return scope.CTypeSystem.Void;
        }

        // TODO[#208]: Support operands of same struct or union type; pointers to compatible types;
        // pointer and null pointer constant; pointer to an object type and pointer to void.
        throw new WipException(208, "Conditional expression with this type is not supported yet.");
    }
}
