using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;

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
            var (type, identifier, parametersInfo, cliImportMemberName) = declaration;
            if (identifier == null)
                throw new NotSupportedException($"Unnamed global symbol of type {type} is not supported.");

            if (cliImportMemberName != null)
            {
                if (initializer != null)
                    throw new NotSupportedException(
                        $"Initializer expression for a CLI import isn't supported: {initializer}.");

                EmitCliImportDeclaration(context, identifier, parametersInfo, type, cliImportMemberName);
                continue;
            }

            if (parametersInfo != null)
            {
                if (initializer != null)
                    throw new NotSupportedException(
                        $"Initializer expression for a function declaration isn't supported: {initializer}.");

                EmitFunctionDeclaration(context, identifier, parametersInfo, type);
                continue;
            }

            // TODO[#75]: Generate a global variable of type {type, isConst}.
            if (initializer != null)
            {
                throw new NotImplementedException(
                    $"Declaration {declaration} with initializer {initializer} not supported, yet.");
                // TODO[#75]: Don't forget to lower the initializer.
            }

            throw new NotImplementedException($"Declaration not supported, yet: {declaration}.");
        }
    }

    private void EmitCliImportDeclaration(
        TranslationUnitContext context,
        string name,
        ParametersInfo? parametersInfo,
        IType returnType,
        string memberName)
    {
        var method = context.MethodLookup(memberName);
        if (method == null) throw new NotSupportedException($"Cannot find CLI-imported member {memberName}.");

        // TODO[#93]: Verify method signature: {parametersIInfo, type}.
        context.Functions.Add(name, new FunctionInfo(parametersInfo, returnType, method, IsDefined: true));
    }

    private void EmitFunctionDeclaration(
        TranslationUnitContext context,
        string identifier,
        ParametersInfo parametersInfo,
        IType returnType)
    {
        var typeSystem = context.TypeSystem;
        var method = context.ModuleType.DefineMethod(
            typeSystem,
            identifier,
            returnType.Resolve(typeSystem),
            parametersInfo);

        context.Functions.Add(
            identifier,
            new FunctionInfo(parametersInfo, returnType, method));
    }
}
