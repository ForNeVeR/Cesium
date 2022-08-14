using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Values;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Cesium.CodeGen.Ir.Expressions;

internal class UnaryOperatorExpression : IExpression
{
    private readonly UnaryOperator _operator;
    private readonly IExpression _target;

    internal UnaryOperatorExpression(UnaryOperator @operator, IExpression target)
    {
        _operator = @operator;
        _target = target;
    }

    public UnaryOperatorExpression(Ast.UnaryOperatorExpression expression)
    {
        var (@operator, target) = expression;
        _operator = GetOperatorKind(@operator);
        _target = target.ToIntermediate();
    }

    public IExpression Lower() => new UnaryOperatorExpression(_operator, _target.Lower());

    public void EmitTo(IDeclarationScope scope)
    {
        switch (_operator)
        {
            case UnaryOperator.AddressOf:
                EmitGetAddress(_target);
                break;
            default:
                _target.EmitTo(scope);
                scope.Method.Body.Instructions.Add(GetInstruction());
                break;
        }

        Instruction GetInstruction() => _operator switch
        {
            UnaryOperator.Negation => Instruction.Create(OpCodes.Neg),
            UnaryOperator.BitwiseNot => Instruction.Create(OpCodes.Not),
            _ => throw new NotSupportedException($"Unsupported unary operator: {_operator}.")
        };

        void EmitGetAddress(IExpression target)
        {
            if (target is not IValueExpression expression)
                throw new NotSupportedException($"Required a value expression to get address, got {target} instead.");

            var value = expression.Resolve(scope);
            if (value is not ILValue lvalue)
                throw new NotSupportedException($"Required an lvalue to get address, got {value} instead.");

            lvalue.EmitGetAddress(scope);
            scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Conv_U));
        }
    }

    public TypeReference GetExpressionType(IDeclarationScope scope) => _operator switch
    {
        UnaryOperator.AddressOf => _target.GetExpressionType(scope).MakePointerType(), // address-of returns T*
        _ => _target.GetExpressionType(scope), // other operators return T
    };

    private static UnaryOperator GetOperatorKind(string @operator) => @operator switch
    {
        "-" => UnaryOperator.Negation,
        "~" => UnaryOperator.BitwiseNot,
        "&" => UnaryOperator.AddressOf,
        _ => throw new NotSupportedException($"Unary operator not supported, yet: {@operator}."),
    };
}
