using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class TypeCastExpression : IExpression
{
    private IType _targetType;
    private IExpression _expression;

    public TypeCastExpression(IType targetType, IExpression expression)
    {
        _targetType = targetType;
        _expression = expression;
    }

    public TypeCastExpression(CastExpression castExpression)
    {
        var ls = castExpression.TypeName.AbstractDeclarator is null
            ? Declarations.LocalDeclarationInfo.Of(castExpression.TypeName.SpecifierQualifierList, (Declarator?)null)
            : Declarations.LocalDeclarationInfo.Of(castExpression.TypeName.SpecifierQualifierList, castExpression.TypeName.AbstractDeclarator);
        _targetType = ls.Type;
        _expression = ExpressionEx.ToIntermediate(castExpression.Target);
    }

    public void EmitTo(IEmitScope scope)
    {
        _expression.EmitTo(scope);

        var ts = scope.CTypeSystem;
        if (_targetType.Equals(ts.SignedChar))
            Add(OpCodes.Conv_I1);
        else if (_targetType.Equals(ts.Short))
            Add(OpCodes.Conv_I2);
        else if (_targetType.Equals(ts.Int))
            Add(OpCodes.Conv_I4);
        else if (_targetType.Equals(ts.Long))
            Add(OpCodes.Conv_I8);
        else if (_targetType.Equals(ts.Char))
            Add(OpCodes.Conv_U1);
        else if (_targetType.Equals(ts.UnsignedShort))
            Add(OpCodes.Conv_U2);
        else if (_targetType.Equals(ts.UnsignedInt))
            Add(OpCodes.Conv_U4);
        else if (_targetType.Equals(ts.UnsignedLong))
            Add(OpCodes.Conv_U8);
        else if (_targetType.Equals(ts.Float))
            Add(OpCodes.Conv_R4);
        else if (_targetType.Equals(ts.Double))
            Add(OpCodes.Conv_R8);
        else if (_targetType is PointerType || _targetType.Equals(ts.NativeInt) || _targetType.Equals(ts.NativeUInt))
            Add(OpCodes.Conv_I);
        else
            throw new AssertException($"Type {_targetType} is not supported.");

        void Add(OpCode op) => scope.Method.Body.Instructions.Add(Instruction.Create(op));
    }

    public IType GetExpressionType(IDeclarationScope scope) => _targetType;

    public IExpression Lower(IDeclarationScope scope) => this;
}
