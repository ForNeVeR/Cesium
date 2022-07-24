using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;

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
                throw new NotSupportedException($"Unknown kind of declaration: {_declaration}.");
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

            var field = EmitGlobalVariable(context, identifier, type);
            // TODO[#75]: Generate a global variable of type {type, isConst}.
            if (initializer != null)
            {
                context.AssemblyContext.AddFieldInitialization(field, initializer);
                //throw new NotImplementedException(
                //    $"Declaration {declaration} with initializer {initializer} not supported, yet.");
                // TODO[#75]: Don't forget to lower the initializer.
                //initializer.EmitTo();
            }
            continue;
            throw new NotImplementedException($"Declaration not supported, yet: {declaration}.");
        }
    }

    private static Mono.Cecil.FieldDefinition EmitGlobalVariable(
        TranslationUnitContext context,
        string memberName,
        IType variableType)
    {
        var field = new Mono.Cecil.FieldDefinition(
            memberName,
            Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.Static,
            variableType.Resolve(context));
        context.GlobalType.Fields.Add(field);
        return field;
    }

    private static void EmitCliImportDeclaration(
        TranslationUnitContext context,
        string name,
        FunctionType functionType,
        string memberName)
    {
        // TODO[#48]: use function type arguments to resolve overloads
        var method = context.MethodLookup(memberName);
        if (method == null)
            throw new NotSupportedException($"Cannot find CLI-imported member {memberName}.");

        ValidateImportDeclaration(context, method, functionType, name);

        var (parametersInfo, returnType) = functionType;
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

    private static void ValidateImportDeclaration(
        TranslationUnitContext context,
        MethodReference method,
        FunctionType funcType,
        string funcName
        )
    {
        var (declParameters, declReturn) = funcType;

        var declReturnReified = declReturn.Resolve(context);
        if (declReturnReified.FullName != method.ReturnType.FullName)
            throw new NotSupportedException($"Returns types for imported function {funcName} do not match: {declReturnReified.Name} in declaration, {method.ReturnType.Name} in source.");

        // TODO[#87]: Use source method arguments definitions if import declaration contains empty parameter list
        if (declParameters == null)
            return;

        var declParamCount = declParameters switch
        {
            {IsVoid: true} => 0,
            {IsVarArg: true} => declParameters.Parameters.Count + 1,
            _ => declParameters.Parameters.Count
        };

        if(method.Parameters.Count != declParamCount)
            throw new NotSupportedException($"Number of arguments for imported function {funcName} do not match: {declParamCount} in declaration, {method.Parameters.Count} in source.");

        for (var i = 0; i < declParameters.Parameters.Count; i++)
        {
            var declParam = declParameters.Parameters[i];
            var declParamType = declParam.Type.Resolve(context);

            var srcParam = method.Parameters[i];
            var srcParamType = srcParam.ParameterType;

            if(declParamType.FullName != srcParamType.FullName)
                throw new NotSupportedException($"Type of argument #{i} for imported function {funcName} does not match: {declParamType} in declaration, {srcParamType} in source.");
        }

        if (declParameters.IsVarArg)
        {
            var lastSrcParam = method.Parameters.Last();
            var paramsAttrType = context.Module.ImportReference(typeof(ParamArrayAttribute));
            if(lastSrcParam.ParameterType.IsArray == false || lastSrcParam.CustomAttributes.Any(x => x.AttributeType == paramsAttrType) == false)
                throw new NotSupportedException($"Signature for imported function {funcName} does not match: accepts variadic arguments in declaration, but not in source.");
        }

        // sic! no backwards check: if the last argument is a params array in source, and a plain array in declaration, it's safe to pass it as is
    }
}
