using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.BinaryOperators;

internal class LogicalBinaryOperatorExpression : BinaryOperatorExpression
{
    private LogicalBinaryOperatorExpression(IExpression left, BinaryOperator @operator, IExpression right)
        : base(left, @operator, right)
    {
        if(!Operator.IsLogical())
            throw new AssertException($"Operator {Operator} is not logical.");
    }

    public LogicalBinaryOperatorExpression(Ast.LogicalBinaryOperatorExpression expression)
        : base(expression)
    {
    }

    public override IExpression Lower(IDeclarationScope scope) => new LogicalBinaryOperatorExpression(Left.Lower(scope), Operator, Right.Lower(scope));

    public override void EmitTo(IEmitScope scope)
    {
        switch (Operator)
        {
            case BinaryOperator.LogicalAnd:
                EmitLogicalAnd(scope);
                return;
            case BinaryOperator.LogicalOr:
                EmitLogicalOr(scope);
                return;
            default:
                throw new AssertException($"Operator {Operator} is not binary logical operator");
        }
    }

    public override IType GetExpressionType(IDeclarationScope scope) => scope.CTypeSystem.Bool;

    private void EmitLogicalAnd(IEmitScope scope)
    {
        var bodyProcessor = scope.Method.Body.GetILProcessor();
        var fastExitLabel = bodyProcessor.Create(OpCodes.Ldc_I4_0);

        Left.EmitTo(scope);
        bodyProcessor.Emit(OpCodes.Ldc_I4_0);
        bodyProcessor.Emit(OpCodes.Beq, fastExitLabel);

        Right.EmitTo(scope);
        bodyProcessor.Emit(OpCodes.Ldc_I4_0);
        bodyProcessor.Emit(OpCodes.Ceq);

        var exitLabel = bodyProcessor.Create(OpCodes.Nop);
        bodyProcessor.Emit(OpCodes.Br, exitLabel);

        bodyProcessor.Append(fastExitLabel);
        bodyProcessor.Append(exitLabel);
    }

    private void EmitLogicalOr(IEmitScope scope)
    {
        var bodyProcessor = scope.Method.Body.GetILProcessor();
        var fastExitLabel = bodyProcessor.Create(OpCodes.Ldc_I4_1);

        Left.EmitTo(scope);
        bodyProcessor.Emit(OpCodes.Ldc_I4_0);
        bodyProcessor.Emit(OpCodes.Ceq);
        bodyProcessor.Emit(OpCodes.Ldc_I4_1);
        bodyProcessor.Emit(OpCodes.Beq, fastExitLabel);

        Right.EmitTo(scope);
        bodyProcessor.Emit(OpCodes.Ldc_I4_0);
        bodyProcessor.Emit(OpCodes.Ceq);
        bodyProcessor.Emit(OpCodes.Ldc_I4_1);
        bodyProcessor.Emit(OpCodes.Ceq);

        var exitLabel = bodyProcessor.Create(OpCodes.Nop);
        bodyProcessor.Emit(OpCodes.Br, exitLabel);

        bodyProcessor.Append(fastExitLabel);
        bodyProcessor.Append(exitLabel);
    }
}
