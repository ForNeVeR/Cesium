using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class TypeCastExpression : IExpression
{
    public IType TargetType { get; }
    public IExpression Expression { get; }

    public TypeCastExpression(IType targetType, IExpression expression)
    {
        TargetType = targetType;
        Expression = expression;
    }

    public TypeCastExpression(CastExpression castExpression)
    {
        var ls = castExpression.TypeName.AbstractDeclarator is null
            ? Declarations.LocalDeclarationInfo.Of(castExpression.TypeName.SpecifierQualifierList, (Declarator?)null)
            : Declarations.LocalDeclarationInfo.Of(castExpression.TypeName.SpecifierQualifierList, castExpression.TypeName.AbstractDeclarator);
        TargetType = ls.Type;
        Expression = ExpressionEx.ToIntermediate(castExpression.Target);
    }

    public void EmitTo(IEmitScope scope)
    {
        Expression.EmitTo(scope);

        var ts = scope.CTypeSystem;
        if (TargetType.Equals(ts.SignedChar))
            Add(OpCodes.Conv_I1);
        else if (TargetType.Equals(ts.Short))
            Add(OpCodes.Conv_I2);
        else if (TargetType.Equals(ts.Int))
            Add(OpCodes.Conv_I4);
        else if (TargetType.Equals(ts.Long))
            Add(OpCodes.Conv_I8);
        else if (TargetType.Equals(ts.Char))
            Add(OpCodes.Conv_U1);
        else if (TargetType.Equals(ts.UnsignedChar))
            Add(OpCodes.Conv_U1);
        else if (TargetType.Equals(ts.UnsignedShort))
            Add(OpCodes.Conv_U2);
        else if (TargetType.Equals(ts.UnsignedInt))
            Add(OpCodes.Conv_U4);
        else if (TargetType.Equals(ts.UnsignedLong))
            Add(OpCodes.Conv_U8);
        else if (TargetType.Equals(ts.Float))
            Add(OpCodes.Conv_R4);
        else if (TargetType.Equals(ts.Double))
            Add(OpCodes.Conv_R8);
        else if (TargetType is PointerType || TargetType.Equals(ts.NativeInt) || TargetType.Equals(ts.NativeUInt))
            Add(OpCodes.Conv_I);
        else if (TargetType is InteropType iType)
        {
            var type = iType.UnderlyingType;
            if (type.FullName == TypeSystemEx.VoidPtrFullTypeName
                || type.IsGenericInstance && type.GetElementType().FullName == TypeSystemEx.CPtrFullTypeName)
            {
                Add(OpCodes.Conv_I); // TODO: Should only emit if required.
                scope.Method.Body.Instructions.Add(
                    Instruction.Create(OpCodes.Call, iType.GetConvertCall(scope.AssemblyContext)));
            }
            else throw new WipException(WipException.ToDo, $"Cast to {iType.UnderlyingType} is not implemented, yet.");
        }
        else
            throw new AssertException($"Type {TargetType} is not supported.");

        void Add(OpCode op) => scope.Method.Body.Instructions.Add(Instruction.Create(op));
    }

    public IType GetExpressionType(IDeclarationScope scope) => TargetType;

    public IExpression Lower(IDeclarationScope scope)
    {
        var resolvedType = scope.ResolveType(TargetType);

        return resolvedType is PrimitiveType { Kind: PrimitiveTypeKind.Void }
            ? new ConsumeExpression(Expression.Lower(scope)).Lower(scope)
            : new TypeCastExpression(resolvedType, Expression.Lower(scope));
    }
}
