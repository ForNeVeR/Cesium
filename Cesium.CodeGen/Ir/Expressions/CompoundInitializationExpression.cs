using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class CompoundInitializationExpression : IExpression
{
    private readonly IType _type;
    private readonly ArrayInitializerExpression _arrayInitializer;

    public CompoundInitializationExpression(IType type, ArrayInitializerExpression arrayInitializer)
    {
        _type = type;
        _arrayInitializer = arrayInitializer;
    }

    public void EmitTo(IEmitScope scope)
    {
        using var stream = new MemoryStream();
        foreach (var i in _arrayInitializer.Initializers)
        {
            if (i is null)
                throw new NotImplementedException("Empty initializer item reached");

            WriteInitializer(stream, i);
        }

        int targetSize = ((InPlaceArrayType)_type).GetSizeInBytes(scope.AssemblyContext.ArchitectureSet) ?? throw new NotImplementedException("Cannot calculate size of target array");
        if (stream.Position < targetSize)
        {
            stream.Write(new byte[targetSize - (int)stream.Position]);
        }

        var constantData = stream.ToArray();
        var fieldReference = scope.AssemblyContext.GetConstantPoolReference(constantData);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsflda, fieldReference));
    }

    private void WriteInitializer(MemoryStream stream, IExpression initializer)
    {
        if (initializer is not ConstantLiteralExpression constantLiteralExpression)
            throw new NotImplementedException("Nested initializers not yet supported");

        if (_type is not InPlaceArrayType inPlaceArrayType)
            throw new NotImplementedException("Nested initializers not yet supported");

        if (inPlaceArrayType.Base.EraseConstType() is not PrimitiveType primitiveType)
            throw new NotImplementedException($"Non-primitive type not yet supported");

        if (constantLiteralExpression.Constant is IntegerConstant integer)
        {
            WriteValue(integer.Value);
            return;
        }

        if (constantLiteralExpression.Constant is CharConstant charConstant)
        {
            WriteValue(charConstant.Value);
            return;
        }

        throw new NotImplementedException($"Non-integer constant not yet supported");

        void WriteValue(long value)
        {
            switch (primitiveType.Kind)
            {
                case PrimitiveTypeKind.Int:
                case PrimitiveTypeKind.SignedInt:
                    var data = BitConverter.GetBytes((int)value);
                    stream.Write(data);
                    break;
                case PrimitiveTypeKind.Long:
                case PrimitiveTypeKind.SignedLong:
                case PrimitiveTypeKind.SignedLongInt:
                    stream.Write(BitConverter.GetBytes(value));
                    break;
                case PrimitiveTypeKind.UnsignedLong:
                case PrimitiveTypeKind.UnsignedLongInt:
                    stream.Write(BitConverter.GetBytes(unchecked((ulong)value)));
                    break;
                case PrimitiveTypeKind.UnsignedInt:
                    stream.Write(BitConverter.GetBytes(unchecked((uint)value)));
                    break;
                case PrimitiveTypeKind.Short:
                case PrimitiveTypeKind.SignedShort:
                    stream.Write(BitConverter.GetBytes((short)value));
                    break;
                case PrimitiveTypeKind.UnsignedShort:
                    stream.Write(BitConverter.GetBytes(unchecked((ushort)value)));
                    break;
                case PrimitiveTypeKind.Char:
                case PrimitiveTypeKind.UnsignedChar:
                    stream.WriteByte((byte)value);
                    break;
                case PrimitiveTypeKind.SignedChar:
                    stream.WriteByte(unchecked((byte)(sbyte)value));
                    break;
                default:
                    throw new NotImplementedException($"Primitive type {primitiveType.Kind} not yet supported");
            }
        }
    }

    public IType GetExpressionType(IDeclarationScope scope) => _type;

    public IExpression Lower(IDeclarationScope scope) => new CompoundInitializationExpression(scope.ResolveType(_type), _arrayInitializer);
}
