using Cesium.Ast;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir;

internal record DeclarationInfo(IType Type, bool IsConst)
{
    public static DeclarationInfo Of(IList<IDeclarationSpecifier> specifiers, IDirectDeclarator directDeclarator)
    {
        var (type, isConst) = GetPrimitiveInfo(specifiers);
        throw new NotImplementedException("TODO");

        return new DeclarationInfo(type, isConst);
    }

    private static (PrimitiveType, bool isConst) GetPrimitiveInfo(IList<IDeclarationSpecifier> specifiers)
    {
        PrimitiveType? type = null;
        bool isConst = false;
        foreach (var specifier in specifiers)
        {
            switch (specifier)
            {
                case TypeSpecifier ts:
                    if (type != null)
                        throw new NotSupportedException(
                            $"Unsupported type definition after already resolved type {type}: {ts}.");

                    type = new PrimitiveType(ts.TypeName switch
                    {
                        "char" => PrimitiveTypeKind.Char,
                        "int" => PrimitiveTypeKind.Int,
                        "void" => PrimitiveTypeKind.Void,
                        var unknown =>
                            throw new NotImplementedException($"Not supported yet type specifier: {unknown}.")
                    });
                    break;

                case TypeQualifier tq:
                    switch (tq.Name)
                    {
                        case "const":
                            if (isConst)
                                throw new NotSupportedException(
                                    $"Multiple const specifiers: {string.Join(", ", specifiers)}.");
                            isConst = true;
                            break;
                        default:
                            throw new NotSupportedException($"Type qualifier {tq} is not supported, yet.");
                    }

                    break;

                default:
                    throw new NotImplementedException($"Declaration specifier {specifier} isn't supported, yet.");
            }
        }

        if (type == null)
            throw new NotSupportedException(
                $"Declaration specifiers missing type specifier: {string.Join(", ", specifiers)}");

        return (type, isConst);
    }

    private static IType Apply(IType type, Pointer? pointer) => pointer switch
    {
        null => type,
        _ when pointer == new Pointer() => new PointerType(type),
        _ => throw new NotImplementedException($"Complex pointer type not supported, yet: {pointer}.")
    };

    private static IType Apply(IType type, IDirectDeclarator declarator)
    {
        type = declarator switch
        {
            ArrayDirectDeclarator { TypeQualifiers: null, Size: null } => new PointerType(type),
            IdentifierDirectDeclarator or IdentifierListDirectDeclarator or ParameterListDirectDeclarator => type,
            _ => throw new NotImplementedException($"Declarator {declarator} isn't supported, yet")
        };

        return declarator.Base == null ? type : Apply(type, declarator.Base);
    }
}
