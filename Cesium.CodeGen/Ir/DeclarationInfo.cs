using Cesium.Ast;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir;

internal record DeclarationInfo(IType Type, bool IsConst, string Identifier, ParametersInfo Parameters)
{
    public static DeclarationInfo Of(IList<IDeclarationSpecifier> specifiers, IDirectDeclarator directDeclarator)
    {
        var (type, isConst) = GetPrimitiveInfo(specifiers);
        string? identifier = null;
        ParametersInfo? parameters = null;

        var declarator = directDeclarator;
        while (declarator != null)
        {
            switch (declarator)
            {
                case IdentifierListDirectDeclarator list:
                {
                    var (_, identifiers) = list;
                    if (identifiers != null)
                        throw new NotImplementedException(
                            "Non-empty identifier list inside of a direct declarator is not supported, yet:" +
                            $" {string.Join(", ", identifiers)}");
                    break;
                }

                case IdentifierDirectDeclarator identifierD:
                    if (identifier != null)
                        throw new NotSupportedException(
                            $"Second identifier \"{identifierD.Identifier}\" given for the declaration \"{identifier}\".");
                    identifier = identifierD.Identifier;
                    break;

                case ParameterListDirectDeclarator parametersD:
                    if (parameters != null)
                        throw new NotSupportedException(
                            $"Second parameters list declarator for an entity already having one: {parametersD}.");

                    parameters = ParametersInfo.Of(parametersD.Parameters);
                    break;

                default: throw new NotImplementedException($"Direct declarator not supported, yet: {declarator}.");
            }

            declarator = declarator.Base;
        }

        if (identifier == null)
            throw new NotImplementedException($"Declaration without name is not supported, yet: {directDeclarator}.");
        parameters ??= new(Array.Empty<ParameterInfo>(), false, false);

        return new DeclarationInfo(type, isConst, identifier, parameters);
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
