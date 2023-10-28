using Cesium.CodeGen.Contexts;
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
        using (var stream = new MemoryStream())
        {
            foreach (var i in _arrayInitializer.Initializers)
            {
                if (i is null)
                {
                    throw new NotImplementedException("Empty initializer item reached");
                }

                WriteInitializer(stream, i);
            }

            var targetSize = ((InPlaceArrayType)_type).GetSizeInBytes(scope.AssemblyContext.ArchitectureSet);
            if (targetSize is null)
            {
                throw new NotImplementedException("Cannot calculate size of target array");
            }

            if (stream.Position < targetSize)
            {
                stream.Write(new byte[targetSize.Value - (int)stream.Position]);
            }

            var constantData = stream.ToArray();
            var fieldReference = scope.AssemblyContext.GetConstantPoolReference(constantData);
            scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsflda, fieldReference));
        }
    }

    private void WriteInitializer(MemoryStream stream, IExpression initializer)
    {
        if (initializer is not ConstantLiteralExpression constantLiteralExpression)
        {
            throw new NotImplementedException("Nested initializers not yet supported");
        }

        if (_type is not InPlaceArrayType inPlaceArrayType)
        {
            throw new NotImplementedException("Nested initializers not yet supported");
        }

        var elementType = inPlaceArrayType.Base;
        var constant = constantLiteralExpression.Constant;
        if (constant is IntegerConstant integer)
        {
            if (elementType is PrimitiveType primitiveType)
            {
                switch (primitiveType.Kind)
                {
                    case PrimitiveTypeKind.Int:
                    case PrimitiveTypeKind.SignedInt:
                        var data = BitConverter.GetBytes(integer.Value);
                        stream.Write(data);
                        break;
                    case PrimitiveTypeKind.UnsignedInt:
                        stream.Write(BitConverter.GetBytes(unchecked((uint)integer.Value)));
                        break;
                    case PrimitiveTypeKind.Short:
                    case PrimitiveTypeKind.SignedShort:
                        stream.Write(BitConverter.GetBytes((short)integer.Value));
                        break;
                    case PrimitiveTypeKind.UnsignedShort:
                        stream.Write(BitConverter.GetBytes(unchecked((ushort)integer.Value)));
                        break;
                    case PrimitiveTypeKind.Char:
                    case PrimitiveTypeKind.UnsignedChar:
                        stream.WriteByte((byte)integer.Value);
                        break;
                    case PrimitiveTypeKind.SignedChar:
                        stream.WriteByte(unchecked((byte)((sbyte)integer.Value)));
                        break;
                    default:
                        throw new NotImplementedException($"Primitive type {primitiveType.Kind} not yet supported");
                }
            }
            else
            {
                throw new NotImplementedException($"Non-primitive type not yet supported");
            }
        }
        else
        {
            throw new NotImplementedException($"Non-integer constant not yet supported");
        }
    }

    public IType GetExpressionType(IDeclarationScope scope) => _type;

    public IExpression Lower(IDeclarationScope scope) => new CompoundInitializationExpression(scope.ResolveType(_type), _arrayInitializer);
}
