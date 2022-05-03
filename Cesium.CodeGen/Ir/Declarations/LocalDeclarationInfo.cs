using Cesium.Ast;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Declarations;

/// <summary>
/// A local declaration info, as opposed to <see cref="IScopedDeclarationInfo"/>, cannot be a typedef or have an
/// initializer, and is always a part of a more complex syntax construct: say, a parameter declaration or a function
/// definition.
/// </summary>
internal record LocalDeclarationInfo(
    IType Type,
    string? Identifier,
    ParametersInfo? Parameters,
    string? CliImportMemberName)
{
    public static LocalDeclarationInfo Of(IReadOnlyList<IDeclarationSpecifier> specifiers, Declarator? declarator)
    {
        var (type, cliImportMemberName) = ProcessSpecifiers(specifiers);
        if (declarator == null)
            return new LocalDeclarationInfo(type, null, null, null);

        var (pointer, directDeclarator) = declarator;
        if (pointer != null)
        {
            var (typeQualifiers, childPointer) = pointer;
            if (typeQualifiers != null || childPointer != null)
                throw new NotImplementedException($"Complex pointer type is not supported, yet: {pointer}.");

            type = new PointerType(type);
        }

        string? identifier = null;
        ParametersInfo? parameters = null;

        var currentDirectDeclarator = directDeclarator;
        while (currentDirectDeclarator != null)
        {
            switch (currentDirectDeclarator)
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

                case ArrayDirectDeclarator array:
                    var (_, typeQualifiers, size) = array;
                    if (typeQualifiers != null)
                        throw new NotImplementedException(
                            $"Array type qualifiers aren't supported, yet: {string.Join(", ", typeQualifiers)}");
                    if (size != null)
                        throw new NotImplementedException(
                            $"Array with specified size isn't supported, yet: {array}.");

                    type = new PointerType(type);
                    break;

                default: throw new NotImplementedException($"Direct declarator not supported, yet: {currentDirectDeclarator}.");
            }

            currentDirectDeclarator = currentDirectDeclarator.Base;
        }

        return new LocalDeclarationInfo(type, identifier, parameters, cliImportMemberName);
    }

    private static (IType, string? cliImportMemberName) ProcessSpecifiers(
        IReadOnlyList<IDeclarationSpecifier> specifiers)
    {
        IType? type = null;
        var isConst = false;
        string? cliImportMemberName = null;
        for (var i = 0; i < specifiers.Count; ++i)
        {
            var specifier = specifiers[i];
            switch (specifier)
            {
                case SimpleTypeSpecifier ts:
                    if (type != null)
                        throw new NotSupportedException(
                            $"Unsupported type definition after already resolved type {type}: {ts}.");

                    type = ProcessSimpleTypeSpecifiers(ts, specifiers, ref i);
                    break;

                case NamedTypeSpecifier nt:
                    if (type != null)
                        throw new NotSupportedException(
                            $"Unsupported type name after already resolved type {type}: {nt}.");

                    nt.Deconstruct(out var typeName);
                    type = new NamedType(typeName);
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

                case CliImportSpecifier cis:
                    if (cliImportMemberName != null)
                        throw new NotSupportedException(
                            $"Multiple CLI import specifiers on a declaration among {string.Join(", ", specifiers)}.");

                    cliImportMemberName = cis.MemberName;
                    break;

                case StorageClassSpecifier { Name: "typedef" }:
                    throw new NotSupportedException($"typedef not expected: {string.Join(", ", specifiers)}.");

                case StructOrUnionSpecifier typeSpecifier:
                {
                    if (type != null)
                        throw new NotSupportedException(
                            $"Cannot update type {type} with a struct specifier {typeSpecifier}.");

                    var (complexTypeKind, identifier, structDeclarations) = typeSpecifier;
                    if (complexTypeKind != ComplexTypeKind.Struct)
                        throw new NotImplementedException($"Complex type kind not supported, yet: {complexTypeKind}.");

                    if (identifier != null)
                        throw new NotImplementedException($"Named structures aren't supported, yet: {identifier}.");

                    type = new StructType(GetTypeMemberDeclarations(structDeclarations));
                    break;
                }

                default:
                    throw new NotImplementedException($"Declaration specifier {specifier} isn't supported, yet.");
            }
        }

        if (type == null)
            throw new NotSupportedException(
                $"Declaration specifiers missing type specifier: {string.Join(", ", specifiers)}");

        return (isConst ? new ConstType(type) : type, cliImportMemberName);
    }

    private static IEnumerable<LocalDeclarationInfo> GetTypeMemberDeclarations(
        IEnumerable<StructDeclaration> structDeclarations)
    {
        return structDeclarations.SelectMany(memberDeclarator =>
        {
            var (specifiersQualifiers, declarators) = memberDeclarator;
            if (declarators == null)
                throw new NotSupportedException(
                    "Empty declarator list on a struct member declaration:" +
                    $"{string.Join(", ", specifiersQualifiers)}.");

            var collection = specifiersQualifiers
                .Select<ISpecifierQualifierListItem, IDeclarationSpecifier>(x => x)
                .ToList();

            return declarators.Select<StructDeclarator, LocalDeclarationInfo>(d =>
            {
                d.Deconstruct(out var declarator);
                return Of(collection, declarator);
            });
        });
    }

    private static IType ProcessSimpleTypeSpecifiers(
        SimpleTypeSpecifier first,
        IReadOnlyList<IDeclarationSpecifier> specifiers,
        ref int i)
    {
        var allSpecifiers = new List<SimpleTypeSpecifier> { first };
        while (specifiers.Count > i + 1 && specifiers[i + 1] is SimpleTypeSpecifier next)
        {
            allSpecifiers.Add(next);
            ++i;
        }

        var typeNames = allSpecifiers.Select(ts =>
        {
            ts.Deconstruct(out var typeName);
            return typeName;
        }).ToList();

        // todo when c#11 is released replace to list pattern matching
        var p1 = typeNames.FirstOrDefault();
        var p2 = typeNames.Skip(1).FirstOrDefault();
        var p3 = typeNames.Skip(2).FirstOrDefault();
        var p4 = typeNames.Skip(3).FirstOrDefault();
        return new PrimitiveType(
            (p1, p2, p3, p4) switch
            {
                ("signed", "long", "long", "int") => PrimitiveTypeKind.SignedLongLongInt,
                ("unsigned", "long", "long", "int") => PrimitiveTypeKind.UnsignedLongLongInt,
                ("signed", "short", "int", _) => PrimitiveTypeKind.SignedShortInt,
                ("signed", "long", "int", _) => PrimitiveTypeKind.SignedLongInt,
                ("signed", "long", "long", _) => PrimitiveTypeKind.SignedLongLong,
                ("long", "long", "int", _) => PrimitiveTypeKind.LongLongInt,
                ("unsigned", "short", "int", _) => PrimitiveTypeKind.UnsignedShortInt,
                ("unsigned", "long", "int", _) => PrimitiveTypeKind.UnsignedLongInt,
                ("unsigned", "long", "long", _) => PrimitiveTypeKind.UnsignedLongLong,
                ("signed", "char", _, _) => PrimitiveTypeKind.SignedChar,
                ("signed", "short", _, _) => PrimitiveTypeKind.SignedShort,
                ("short", "int", _, _) => PrimitiveTypeKind.ShortInt,
                ("signed", "int", _, _) => PrimitiveTypeKind.SignedInt,
                ("signed", "long", _, _) => PrimitiveTypeKind.SignedLong,
                ("long", "int", _, _) => PrimitiveTypeKind.LongInt,
                ("long", "long", _, _) => PrimitiveTypeKind.LongLong,
                ("long", "double", _, _) => PrimitiveTypeKind.LongDouble,
                ("unsigned", "char", _, _) => PrimitiveTypeKind.UnsignedChar,
                ("unsigned", "short", _, _) => PrimitiveTypeKind.UnsignedShort,
                ("unsigned", "int", _, _) => PrimitiveTypeKind.UnsignedInt,
                ("unsigned", "long", _, _) => PrimitiveTypeKind.UnsignedLong,
                ("void", _, _, _) => PrimitiveTypeKind.Void,
                ("char", _, _, _) => PrimitiveTypeKind.Char,
                ("short", _, _, _) => PrimitiveTypeKind.Short,
                ("signed", _, _, _) => PrimitiveTypeKind.Signed,
                ("int", _, _, _) => PrimitiveTypeKind.Int,
                ("unsigned", _, _, _) => PrimitiveTypeKind.Unsigned,
                ("long", _, _, _) => PrimitiveTypeKind.Long,
                ("float", _, _, _) => PrimitiveTypeKind.Float,
                ("double", _, _, _) => PrimitiveTypeKind.Double,
                _ => throw new NotImplementedException(
                    $"Simple type specifiers are not supported: {string.Join(" ", typeNames)}"),
            });
    }
}
