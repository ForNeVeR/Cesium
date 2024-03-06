using System.Globalization;
using Cesium.Ast;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.CodeGen.Ir.Declarations;

/// <summary>
/// A local declaration info, as opposed to <see cref="IScopedDeclarationInfo"/>, cannot be a typedef or have an
/// initializer, and is always a part of a more complex syntax construct: say, a parameter declaration or a function
/// definition.
/// </summary>
internal sealed record LocalDeclarationInfo(
    IType Type,
    string? Identifier,
    string? CliImportMemberName)
{
    public static LocalDeclarationInfo Of(IReadOnlyList<IDeclarationSpecifier> specifiers, Declarator? declarator, Initializer? initializer = null)
    {
        var (type, cliImportMemberName) = ProcessSpecifiers(specifiers);
        if (declarator == null)
        {
            if (type is StructType structType)
            {
                return new LocalDeclarationInfo(type, structType.Identifier, null);
            }

            if (type is EnumType enumType)
            {
                return new LocalDeclarationInfo(type, enumType.Identifier, null);
            }

            return new LocalDeclarationInfo(type, null, null);
        }

        var (pointer, directDeclarator) = declarator;
        type = ProcessPointer(pointer, type);
        (type, var identifier) = ProcessDirectDeclarator(directDeclarator, type, initializer);

        return new LocalDeclarationInfo(type, identifier, cliImportMemberName);
    }

    public static LocalDeclarationInfo Of(
        IReadOnlyList<IDeclarationSpecifier> specifiers,
        AbstractDeclarator abstractDeclarator)
    {
        var (type, cliImportMemberName) = ProcessSpecifiers(specifiers);

        var (pointer, directAbstractDeclarator) = abstractDeclarator;
        type = ProcessPointer(pointer, type);
        type = ProcessDirectAbstractDeclarator(directAbstractDeclarator, type);

        return new LocalDeclarationInfo(type, Identifier: null, cliImportMemberName);
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

                case StorageClassSpecifier { Name: "static" }:
                    throw new CompilationException($"static not expected: {string.Join(", ", specifiers)}.");

                case StructOrUnionSpecifier typeSpecifier:
                {
                    if (type != null)
                        throw new CompilationException(
                            $"Cannot update type {type} with a struct specifier {typeSpecifier}.");

                    var (complexTypeKind, identifier, structDeclarations) = typeSpecifier;
                    if (complexTypeKind is ComplexTypeKind.Struct or ComplexTypeKind.Union)
                        type = new StructType(GetTypeMemberDeclarations(structDeclarations).ToList(), complexTypeKind == ComplexTypeKind.Union, identifier);
                    else
                        throw new AssertException($"Unknown complex type kind: {complexTypeKind}.");

                    break;
                }

                case EnumSpecifier enumTypeSpecifier:
                {
                    if (type != null)
                        throw new CompilationException(
                            $"Cannot update type {type} with a enum specifier {enumTypeSpecifier}.");

                    var ( identifier, enumDeclarations) = enumTypeSpecifier;
                    if (identifier is null && enumDeclarations is null)
                        throw new CompilationException(
                            $"Incomplete enum specifier {enumTypeSpecifier}.");

                    type = new EnumType(GetEnumMemberDeclarations(enumDeclarations).ToList(), identifier);
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

    private static IType ProcessPointer(Pointer? pointer, IType type)
    {
        if (pointer == null) return type;

        var (typeQualifiers, childPointer) = pointer;
        if (typeQualifiers != null)
            if (typeQualifiers.Value.Length == 1 && typeQualifiers.Value[0].Name != "const")
                throw new WipException(215, $"Complex pointer type is not supported, yet: {pointer}.");

        type = new PointerType(type);
        if (childPointer != null)
            type = ProcessPointer(childPointer, type);

        return type;
    }

    private static (IType, string? Identifier) ProcessDirectDeclarator(IDirectDeclarator directDeclarator, IType type, Initializer? initializer = null)
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
                    {
                        if (initializer != null && initializer is ArrayInitializer arrayInitializer &&
                            arrayInitializer.Initializers.Length > 0)
                        {
                            var size = arrayInitializer.Initializers.Length;
                            type = CreateArrayType(type, size);
                        }
                        else
                        {
                            type = new PointerType(type);
                        }
                    }
                    else
                    {
                        if (sizeExpr is not ConstantLiteralExpression constantExpression ||
                            constantExpression.Constant.Kind != CTokenType.IntLiteral ||
                            !int.TryParse(constantExpression.Constant.Text, out var size))
                            throw new CompilationException($"Array size specifier is not integer {sizeExpr}.");

                        type = CreateArrayType(type, size);
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

    private static IType ProcessDirectAbstractDeclarator(
        IDirectAbstractDeclarator? directAbstractDeclarator,
        IType type)
    {
        var current = directAbstractDeclarator;
        while (current != null)
        {
            switch(current)
            {
                case ArrayDirectAbstractDeclarator arr:
                    current = arr.Base;

                    if (arr.Size is not ConstantLiteralExpression constantExpression ||
                            constantExpression.Constant.Kind != CTokenType.IntLiteral ||
                            !int.TryParse(constantExpression.Constant.Text, CultureInfo.InvariantCulture, out var size))
                        throw new CompilationException($"Array size specifier is not integer {arr.Size}.");

                    type = CreateArrayType(type, size);
                    break;
                case SimpleDirectAbstractDeclarator simple: // does it exist?
                    current = simple.Base;
                    break;
                default:
                    throw new AssertException($"Unknown direct abstract declarator: {current}.");
            }
        }

        return type;
    }

    private static IType CreateArrayType(IType type, int size)
    {
        if (type is InPlaceArrayType inplaceArrayType)
        {
            return new InPlaceArrayType(new InPlaceArrayType(inplaceArrayType.Base, size), inplaceArrayType.Size);
        }

        return new InPlaceArrayType(type, size);
    }

    private static IEnumerable<LocalDeclarationInfo> GetTypeMemberDeclarations(
        IEnumerable<StructDeclaration> structDeclarations)
    {
        return structDeclarations.SelectMany(memberDeclarator =>
        {
            var (specifiersQualifiers, declarators) = memberDeclarator;

            var collection = specifiersQualifiers
                .Select<ISpecifierQualifierListItem, IDeclarationSpecifier>(x => x)
                .ToList();

            if (declarators == null) // maybe its anon structure or anon union?
            {
                if (collection.Any(s => s is StructOrUnionSpecifier structOrUnion)) // yes, its anon structure or union
                    return [Of(collection, null, null)];
                else // nope
                    throw new CompilationException(
                       "Empty declarator list on a struct member declaration:" +
                       $"{string.Join(", ", specifiersQualifiers)}.");
            }

            return declarators.Select<StructDeclarator, LocalDeclarationInfo>(d =>
            {
                d.Deconstruct(out var declarator);
                return Of(collection, declarator);
            });
        });
    }

    private static IEnumerable<InitializableDeclarationInfo> GetEnumMemberDeclarations(
        IEnumerable<EnumDeclaration>? structDeclarations)
    {
        if (structDeclarations is null)
        {
            return Array.Empty<InitializableDeclarationInfo>();
        }

        return structDeclarations.Select(memberDeclarator =>
        {
            var (identifier, declarators) = memberDeclarator;
            return new InitializableDeclarationInfo(
                new LocalDeclarationInfo(null!, identifier, null),
                declarators?.ToIntermediate());
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

        return new PrimitiveType(
            typeNames switch
            {
                ["signed", "long", "long", "int"] => PrimitiveTypeKind.SignedLongLongInt,
                ["unsigned", "long", "long", "int"] => PrimitiveTypeKind.UnsignedLongLongInt,
                ["signed", "short", "int"] => PrimitiveTypeKind.SignedShortInt,
                ["signed", "long", "int"] => PrimitiveTypeKind.SignedLongInt,
                ["signed", "long", "long"] => PrimitiveTypeKind.SignedLongLong,
                ["long", "long", "int"] => PrimitiveTypeKind.LongLongInt,
                ["unsigned", "short", "int"] => PrimitiveTypeKind.UnsignedShortInt,
                ["unsigned", "long", "int"] => PrimitiveTypeKind.UnsignedLongInt,
                ["unsigned", "long", "long"] => PrimitiveTypeKind.UnsignedLongLong,
                ["signed", "char"] => PrimitiveTypeKind.SignedChar,
                ["signed", "short"] => PrimitiveTypeKind.SignedShort,
                ["short", "int"] => PrimitiveTypeKind.ShortInt,
                ["signed", "int"] => PrimitiveTypeKind.SignedInt,
                ["signed", "long"] => PrimitiveTypeKind.SignedLong,
                ["long", "int"] => PrimitiveTypeKind.LongInt,
                ["long", "long"] => PrimitiveTypeKind.LongLong,
                ["long", "double"] => PrimitiveTypeKind.LongDouble,
                ["unsigned", "char"] => PrimitiveTypeKind.UnsignedChar,
                ["unsigned", "short"] => PrimitiveTypeKind.UnsignedShort,
                ["unsigned", "int"] => PrimitiveTypeKind.UnsignedInt,
                ["unsigned", "long"] => PrimitiveTypeKind.UnsignedLong,
                ["void"] => PrimitiveTypeKind.Void,
                ["char"] => PrimitiveTypeKind.Char,
                ["short"] => PrimitiveTypeKind.Short,
                ["signed"] => PrimitiveTypeKind.Signed,
                ["int"] => PrimitiveTypeKind.Int,
                ["unsigned"] => PrimitiveTypeKind.Unsigned,
                ["long"] => PrimitiveTypeKind.Long,
                ["float"] => PrimitiveTypeKind.Float,
                ["double"] => PrimitiveTypeKind.Double,
                ["__nint"] => PrimitiveTypeKind.NativeInt,
                ["__nuint"] => PrimitiveTypeKind.NativeUInt,
                ["_Bool"] => PrimitiveTypeKind.Bool,
                _ => throw new WipException(
                    224,
                    $"Simple type specifiers are not supported: {string.Join(" ", typeNames)}"),
            });
    }

    private static IType ProcessFunctionParameters(IType returnType, ParameterTypeList? parameters) =>
        new FunctionType(ParametersInfo.Of(parameters), returnType);
}
