using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;

namespace Cesium.CodeGen.Ir.TopLevel;

internal class SymbolDeclaration : ITopLevelNode
{
    private readonly IList<InitializableDeclarationInfo> _declarations;
    public SymbolDeclaration(Ast.SymbolDeclaration ast)
    {
        ast.Deconstruct(out var declaration);
        _declarations = InitializableDeclarationInfo.Of(declaration).ToList();
    }

    public void EmitTo(TranslationUnitContext context)
    {
        foreach (var (declaration, initializer) in _declarations)
        {
            var (type, isConst, identifier, parametersInfo, cliImportMemberName) = declaration;
            if (identifier == null)
                throw new NotSupportedException($"Unnamed global symbol of type {type} is not supported.");

            // TODO[F]: Generate a global variable of type {type, isConst}.
            if (cliImportMemberName != null)
            {
                if (initializer != null)
                    throw new NotSupportedException(
                        $"Initializer expression for a CLI import isn't supported: {initializer}.");

                var method = context.MethodLookup(cliImportMemberName);
                if (method == null) throw new NotSupportedException($"Cannot find CLI-imported member {cliImportMemberName}.");

                // TODO[F]: Verify method signature: {parametersIInfo, type, isConst}.
                context.Functions.Add(identifier, method);
                return;
            }

            if (initializer != null)
            {
                throw new NotImplementedException(
                    $"Declaration {declaration} with initializer {initializer} not supported, yet.");
                // TODO[F]: Don't forget to lower the initializer.
            }

            throw new NotImplementedException($"Declaration not supported, yet: {declaration}.");
        }
    }
}
