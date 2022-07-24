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
            var (type, identifier, cliImportMemberName) = declaration;
            if (identifier == null)
                throw new NotSupportedException($"Unnamed global symbol of type {type} is not supported.");

            if (cliImportMemberName != null)
            {
                if (initializer != null)
                    throw new NotSupportedException(
                        $"Initializer expression for a CLI import isn't supported: {initializer}.");

                if (type is not FunctionType cliFunction)
                    throw new NotSupportedException($"CLI initializer should be a function for identifier {identifier}.");

                EmitCliImportDeclaration(context, identifier, cliFunction, cliImportMemberName);
                continue;
            }

            if (type is FunctionType functionType)
            {
                if (initializer != null)
                    throw new NotSupportedException(
                        $"Initializer expression for a function declaration isn't supported: {initializer}.");

                EmitFunctionDeclaration(context, identifier, functionType);
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
        FunctionType functionType,
        string memberName)
    {
        var method = context.MethodLookup(memberName);
        if (method == null) throw new NotSupportedException($"Cannot find CLI-imported member {memberName}.");

        var (parametersInfo, returnType) = functionType;
        // TODO[#93]: Verify method signature: {parametersInfo, type}.
        context.Functions.Add(name, new FunctionInfo(parametersInfo, returnType, method, IsDefined: true));
    }

    private static void EmitFunctionDeclaration(
        TranslationUnitContext context,
        string identifier,
        FunctionType functionType)
    {
        var (parametersInfo, returnType) = functionType;
        var existingFunction = context.Functions.GetValueOrDefault(identifier);
        if (existingFunction != null)
        {
            // The function with the same name is already defined. Then, just verify that it has the same signature and
            // exit:
            existingFunction.VerifySignatureEquality(identifier, parametersInfo, returnType);
            return;
        }

        var method = context.ModuleType.DefineMethod(
            context,
            identifier,
            returnType.Resolve(context),
            parametersInfo);

        context.Functions.Add(identifier, new FunctionInfo(parametersInfo, returnType, method));
    }

    private static void EmitTypeDef(TranslationUnitContext context, TypeDefDeclaration declaration)
    {
        declaration.Deconstruct(out var types);
        foreach (var typeDef in types)
        {
            var (type, identifier, cliImportMemberName) = typeDef;
            if (identifier == null)
                throw new NotSupportedException($"Anonymous typedef not supported: {type}.");

            if (cliImportMemberName != null)
                throw new NotSupportedException($"typedef for CLI import not supported: {cliImportMemberName}.");

            if (type is IGeneratedType t)
                context.GenerateType(t, identifier);
            else
               context.AddPlainType(type, identifier);
        }
    }
}
