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

                case DeclaratorDirectDeclarator ddd:
                    ddd.Deconstruct(out var nestedDeclarator);
                    var (nestedPointer, nestedDirectDeclarator) = nestedDeclarator;
                    if (nestedPointer != null)
                    {
                        var (nestedTypeQualifiers, nestedChildPointer) = nestedPointer;
                        if (nestedTypeQualifiers != null || nestedChildPointer != null)
                            throw new NotImplementedException(
                                $"Nested pointer of kind {nestedPointer} is not supported, yet.");

                        type = new PointerType(type);
                    }

                    // TODO[#72]: Rewrite this to append a pointer to the current type.
                    // TODO[#72]: "The current type", though, should be a function type already at this moment.
                    // TODO[#72]: This means that LocalDeclarationInfo should get rid of "Parameters" and they will
                    //            become a part of the underlying type.
                    throw new NotImplementedException("TODO: This code is wrong.");
                    currentDirectDeclarator = nestedDirectDeclarator;
                    continue;

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

        return typeNames switch
        {
            { Count: 1 } => new PrimitiveType(typeNames.Single() switch
            {
                "char" => PrimitiveTypeKind.Char,
                "int" => PrimitiveTypeKind.Int,
                "void" => PrimitiveTypeKind.Void,
                var unknown =>
                    throw new NotImplementedException($"Not supported yet type specifier: {unknown}.")
            }),
            { Count: 2 } when typeNames[0] == "unsigned" && typeNames[1] == "char" =>
                new PrimitiveType(PrimitiveTypeKind.UnsignedChar),
            _ => throw new NotImplementedException(
                $"Simple type specifiers are not supported: {string.Join(" ", typeNames)}")
        };
    }
}
