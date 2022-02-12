using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.TopLevel;

internal class TopLevelDeclaration : ITopLevelNode
{
    private readonly IScopedDeclarationInfo _declaration;
    public TopLevelDeclaration(Ast.SymbolDeclaration ast)
    {
        ast.Deconstruct(out var declaration);
        _declaration = IScopedDeclarationInfo.Of(declaration);
    }

    public void EmitTo(TranslationUnitContext context)
    {
        switch (_declaration)
        {
            case ScopedIdentifierDeclaration declaration:
                EmitScopedIdentifier(context, declaration);
                break;
            case TypeDefDeclaration declaration:
                EmitTypeDef(context, declaration);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_declaration));
        }
    }

    private static void EmitScopedIdentifier(
        TranslationUnitContext context,
        ScopedIdentifierDeclaration scopedDeclaration)
    {
        scopedDeclaration.Deconstruct(out var items);

        foreach (var (declaration, initializer) in items)
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

    private static void EmitCliImportDeclaration(
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

    private static void EmitFunctionDeclaration(
        TranslationUnitContext context,
        string identifier,
        ParametersInfo parametersInfo,
        IType returnType)
    {
        var existingFunction = context.Functions.GetValueOrDefault(identifier);
        if (existingFunction != null)
        {
            // The function with the same name is already defined. Then, just verify that it has the same signature and
            // exit:
            existingFunction.VerifySignatureEquality(identifier, parametersInfo, returnType);
            return;
        }

        var typeSystem = context.TypeSystem;
        var method = context.ModuleType.DefineMethod(
            typeSystem,
            identifier,
            returnType.Resolve(typeSystem),
            parametersInfo);

        context.Functions.Add(identifier, new FunctionInfo(parametersInfo, returnType, method));
    }

    private static void EmitTypeDef(TranslationUnitContext context, TypeDefDeclaration declaration)
    {
        declaration.Deconstruct(out var types);
        foreach (var typeDef in types)
        {
            var (type, identifier, parametersInfo, cliImportMemberName) = typeDef;
            if (identifier == null)
                throw new NotSupportedException($"Anonymous typedef not supported: {type}.");

            if (parametersInfo != null)
                throw new NotImplementedException($"Function typedef not supported, yet: {identifier}.");

            if (cliImportMemberName != null)
                throw new NotSupportedException($"typedef for CLI import not supported: {cliImportMemberName}.");

            context.Types.Add(identifier, type.Resolve(context.TypeSystem));
        }
    }
}
