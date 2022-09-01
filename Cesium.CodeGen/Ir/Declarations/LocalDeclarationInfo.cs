using Cesium.Ast;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.CodeGen.Ir.Declarations;

/// <summary>
/// A local declaration info, as opposed to <see cref="IScopedDeclarationInfo"/>, cannot be a typedef or have an
/// initializer, and is always a part of a more complex syntax construct: say, a parameter declaration or a function
/// definition.
/// </summary>
internal record LocalDeclarationInfo(
    IType Type,
    string? Identifier,
    string? CliImportMemberName)
{
    public static LocalDeclarationInfo Of(IReadOnlyList<IDeclarationSpecifier> specifiers, Declarator? declarator)
    {
        var (type, cliImportMemberName) = ProcessSpecifiers(specifiers);
        if (declarator == null)
            return new LocalDeclarationInfo(type, null, null);

        var (pointer, directDeclarator) = declarator;
        if (pointer != null)
        {
            var (typeQualifiers, childPointer) = pointer;
            if (typeQualifiers != null || childPointer != null)
                throw new WipException(215, $"Complex pointer type is not supported, yet: {pointer}.");

            type = new PointerType(type);
        }

        (type, var identifier) = ProcessDirectDeclarator(directDeclarator, type);

        return new LocalDeclarationInfo(type, identifier, cliImportMemberName);
    }

    private static (IType, string? CliImportMemberName) ProcessSpecifiers(
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
                        throw new CompilationException(
                            $"Unsupported type definition after already resolved type {type}: {ts}.");

                    type = ProcessSimpleTypeSpecifiers(ts, specifiers, ref i);
                    break;

                case NamedTypeSpecifier nt:
                    if (type != null)
                        throw new CompilationException(
                            $"Unsupported type name after already resolved type {type}: {nt}.");

                    nt.Deconstruct(out var typeName);
                    type = new NamedType(typeName);
                    break;

                case TypeQualifier tq:
                    switch (tq.Name)
                    {
                        case "const":
                            if (isConst)
                                throw new CompilationException(
                                    $"Multiple const specifiers: {string.Join(", ", specifiers)}.");
                            isConst = true;
                            break;
                        default:
                            throw new WipException(216, $"Type qualifier {tq} is not supported, yet.");
                    }

                    break;

                case CliImportSpecifier cis:
                    if (cliImportMemberName != null)
                        throw new CompilationException(
                            $"Multiple CLI import specifiers on a declaration among {string.Join(", ", specifiers)}.");

                    cliImportMemberName = cis.MemberName;
                    break;

                case StorageClassSpecifier { Name: "typedef" }:
                    throw new CompilationException($"typedef not expected: {string.Join(", ", specifiers)}.");

                case StructOrUnionSpecifier typeSpecifier:
                {
                    if (type != null)
                        throw new CompilationException(
                            $"Cannot update type {type} with a struct specifier {typeSpecifier}.");

                    var (complexTypeKind, identifier, structDeclarations) = typeSpecifier;
                    if (complexTypeKind != ComplexTypeKind.Struct)
                        throw new WipException(217, $"Complex type kind not supported, yet: {complexTypeKind}.");

                    if (identifier != null)
                        throw new WipException(218, $"Named structures aren't supported, yet: {identifier}.");

                    type = new StructType(GetTypeMemberDeclarations(structDeclarations).ToList());
                    break;
                }

                default:
                    throw new WipException(219, $"Declaration specifier {specifier} isn't supported, yet.");
            }
        }

        if (type == null)
            throw new CompilationException(
                $"Declaration specifiers missing type specifier: {string.Join(", ", specifiers)}");

        return (isConst ? new ConstType(type) : type, cliImportMemberName);
    }

    private static (IType, string? Identifier) ProcessDirectDeclarator(IDirectDeclarator directDeclarator, IType type)
    {
        string? identifier = null;

        var currentDirectDeclarator = directDeclarator;
        while (currentDirectDeclarator != null)
        {
            switch (currentDirectDeclarator)
            {
                case IdentifierListDirectDeclarator list:
                {
                    var (_, identifiers) = list;
                    if (identifiers != null)
                        throw new WipException(
                            220,
                            "Non-empty identifier list inside of a direct declarator is not supported, yet:" +
                            $" {string.Join(", ", identifiers)}");

                    // An absent identifier list is `()` in a declaration like `int main()`. It means that there's an
                    // empty parameter list, actually.
                    type = ProcessFunctionParameters(type, null);

                    break;
                }

                case IdentifierDirectDeclarator identifierD:
                    if (identifier != null)
                        throw new CompilationException(
                            $"Second identifier \"{identifierD.Identifier}\" given for the declaration \"{identifier}\".");
                    identifier = identifierD.Identifier;
                    break;

                case ParameterListDirectDeclarator parametersD:
                    var (_ /* base */, parameters) = parametersD;
                    type = ProcessFunctionParameters(type, parameters);
                    break;

                case ArrayDirectDeclarator array:
                    var (_, typeQualifiers, sizeExpr) = array;
                    if (typeQualifiers != null)
                        throw new WipException(
                            221,
                            $"Array type qualifiers aren't supported, yet: {string.Join(", ", typeQualifiers)}");

                    // TODO[#126]: should check that size required in scoped declaration and not needed in parameter declaration
                    if (sizeExpr == null)
                        type = new PointerType(type);
                    else
                    {
                        if (sizeExpr is not ConstantExpression constantExpression ||
                            constantExpression.Constant.Kind != CTokenType.IntLiteral ||
                            !int.TryParse(constantExpression.Constant.Text, out var size))
                            throw new CompilationException($"Array size specifier is not integer {sizeExpr}.");

                        type = new InPlaceArrayType(type, size);
                    }

                    break;

                case DeclaratorDirectDeclarator ddd:
                    ddd.Deconstruct(out var nestedDeclarator);
                    var (nestedPointer, nestedDirectDeclarator) = nestedDeclarator;
                    if (nestedPointer != null)
                    {
                        var (nestedTypeQualifiers, nestedChildPointer) = nestedPointer;
                        if (nestedTypeQualifiers != null || nestedChildPointer != null)
                            throw new WipException(
                                222,
                                $"Nested pointer of kind {nestedPointer} is not supported, yet.");

                        type = new PointerType(type);
                    }

                    // The only kind of nested direct declarator we support is this one:
                    if (nestedDirectDeclarator is IdentifierDirectDeclarator idd)
                    {
                        if (idd.Base != null)
                            throw new CompilationException(
                                $"Not supported nested direct declarator with base: {idd}.");

                        idd.Deconstruct(out var nestedIdentifier);
                        if (identifier != null && nestedIdentifier != null)
                            throw new CompilationException(
                                $"Identifier conflict: nested identifier \"{nestedIdentifier}\"" +
                                $" tries to override \"{identifier}.\"");

                        identifier = nestedIdentifier;
                    }
                    else
                    {
                        throw new CompilationException(
                            $"Not supported nested direct declarator: {nestedDirectDeclarator}.");
                    }

                    break;

                default: throw new WipException(223, $"Direct declarator not supported, yet: {currentDirectDeclarator}.");
            }

            currentDirectDeclarator = currentDirectDeclarator.Base;
        }

        return (type, identifier);
    }

    private static IEnumerable<LocalDeclarationInfo> GetTypeMemberDeclarations(
        IEnumerable<StructDeclaration> structDeclarations)
    {
        return structDeclarations.SelectMany(memberDeclarator =>
        {
            var (specifiersQualifiers, declarators) = memberDeclarator;
            if (declarators == null)
                throw new CompilationException(
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

        // TODO[#236]: when C#11 is released, replace this using list pattern matching.
        var p1 = typeNames.FirstOrDefault();
        var p2 = typeNames.Skip(1).FirstOrDefault();
        var p3 = typeNames.Skip(2).FirstOrDefault();
        var p4 = typeNames.Skip(3).FirstOrDefault();
        return new PrimitiveType(
            (p1, p2, p3, p4) switch
            {
                ("signed", "long", "long", "int") => PrimitiveTypeKind.SignedLongLongInt,
                ("unsigned", "long", "long", "int") => PrimitiveTypeKind.UnsignedLongLongInt,
                ("signed", "short", "int", null) => PrimitiveTypeKind.SignedShortInt,
                ("signed", "long", "int", null) => PrimitiveTypeKind.SignedLongInt,
                ("signed", "long", "long", null) => PrimitiveTypeKind.SignedLongLong,
                ("long", "long", "int", null) => PrimitiveTypeKind.LongLongInt,
                ("unsigned", "short", "int", null) => PrimitiveTypeKind.UnsignedShortInt,
                ("unsigned", "long", "int", null) => PrimitiveTypeKind.UnsignedLongInt,
                ("unsigned", "long", "long", null) => PrimitiveTypeKind.UnsignedLongLong,
                ("signed", "char", null, null) => PrimitiveTypeKind.SignedChar,
                ("signed", "short", null, null) => PrimitiveTypeKind.SignedShort,
                ("short", "int", null, null) => PrimitiveTypeKind.ShortInt,
                ("signed", "int", null, null) => PrimitiveTypeKind.SignedInt,
                ("signed", "long", null, null) => PrimitiveTypeKind.SignedLong,
                ("long", "int", null, null) => PrimitiveTypeKind.LongInt,
                ("long", "long", null, null) => PrimitiveTypeKind.LongLong,
                ("long", "double", null, null) => PrimitiveTypeKind.LongDouble,
                ("unsigned", "char", null, null) => PrimitiveTypeKind.UnsignedChar,
                ("unsigned", "short", null, null) => PrimitiveTypeKind.UnsignedShort,
                ("unsigned", "int", null, null) => PrimitiveTypeKind.UnsignedInt,
                ("unsigned", "long", null, null) => PrimitiveTypeKind.UnsignedLong,
                ("void", null, null, null) => PrimitiveTypeKind.Void,
                ("char", null, null, null) => PrimitiveTypeKind.Char,
                ("short", null, null, null) => PrimitiveTypeKind.Short,
                ("signed", null, null, null) => PrimitiveTypeKind.Signed,
                ("int", null, null, null) => PrimitiveTypeKind.Int,
                ("unsigned", null, null, null) => PrimitiveTypeKind.Unsigned,
                ("long", null, null, null) => PrimitiveTypeKind.Long,
                ("float", null, null, null) => PrimitiveTypeKind.Float,
                ("double", null, null, null) => PrimitiveTypeKind.Double,
                _ => throw new WipException(
                    224,
                    $"Simple type specifiers are not supported: {string.Join(" ", typeNames)}"),
            });
    }

    private static IType ProcessFunctionParameters(IType returnType, ParameterTypeList? parameters) =>
        new FunctionType(ParametersInfo.Of(parameters), returnType);
}
