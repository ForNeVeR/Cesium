using Cesium.Ast;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Cesium.CodeGen.Extensions;

public static class DeclaratorEx
{
    public static TypeReference CalculateType(
        this Declarator declarator,
        string name,
        IEnumerable<IDeclarationSpecifier> specifiers,
        ModuleDefinition module)
    {
        if (declarator.Pointer is not null && declarator.Pointer != new Pointer())
        {
            throw new NotImplementedException($"Complex pointer type not supported, yet: {declarator.Pointer}");
        }

        var isPointer = declarator.Pointer is not null;
        var isConst = false; // TODO: enforce declaration constness.
        TypeReference? typeReference = null;
        foreach (var specifier in specifiers)
        {
            switch (specifier)
            {
                case TypeSpecifier ts:
                    if (typeReference != null)
                    {
                        throw new NotSupportedException(
                            $"Cannot process type specifier {ts} " +
                            $"because declaration {name} already has type {typeReference} (double type specifiers?).");
                    }

                    typeReference = ts.GetTypeReference(module);
                    break;
                case TypeQualifier { Name: "const" }:
                    if (isConst)
                    {
                        throw new NotSupportedException(
                            $"Cannot add constness to a declaration {name} (double const specifier?).");
                    }

                    isConst = true;
                    break;
                default:
                    throw new NotImplementedException($"Declaration specifier not supported: {specifier}");
            }
        }

        if (typeReference == null)
            throw new NotSupportedException("Cannot determine type of the declaration {name}.");

        return isPointer ? typeReference.MakePointerType() : typeReference;
    }
}
