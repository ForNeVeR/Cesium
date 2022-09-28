using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class FunctionDeclaration : IBlockItem
{
    private readonly string identifier;
    private readonly FunctionType functionType;
    private readonly string? cliImportMemberName;

    public FunctionDeclaration(string identifier, FunctionType functionType, string? cliImportMemberName)
    {
        this.identifier = identifier;
        this.functionType = functionType;
        this.cliImportMemberName = cliImportMemberName;
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        return this;
    }

    public void EmitTo(IEmitScope scope)
    {
        if (cliImportMemberName != null)
        {
            EmitCliImportDeclaration(scope, cliImportMemberName);
            return;
        }

        EmitFunctionDeclaration(scope);
    }

    private void EmitCliImportDeclaration(
        IEmitScope scope,
        string cliImportMemberName)
    {
        var (parametersInfo, returnType) = functionType;
        if (parametersInfo is null or { Parameters.Count: 0, IsVoid: false })
            throw new CompilationException($"Empty parameter list is not allowed for CLI-imported function {identifier}.");

        var method = scope.Context.MethodLookup(cliImportMemberName, parametersInfo, returnType);
        var cliImportFunctionInfo = new FunctionInfo(parametersInfo, returnType, method, IsDefined: true);
        if (!scope.Context.Functions.TryGetValue(identifier, out var existingDeclaration))
        {
            scope.Context.Functions.Add(identifier, cliImportFunctionInfo);
            return;
        }

        cliImportFunctionInfo.VerifySignatureEquality(identifier, existingDeclaration.Parameters, existingDeclaration.ReturnType);
        if (!cliImportFunctionInfo.MethodReference.FullName.Equals(existingDeclaration.MethodReference.FullName))
        {
            throw new CompilationException($"Function {identifier} already defined as as CLI-import with {existingDeclaration.MethodReference.FullName}.");
        }
    }

    private void EmitFunctionDeclaration(
        IEmitScope scope)
    {
        var (parametersInfo, returnType) = functionType;
        var existingFunction = scope.Context.Functions.GetValueOrDefault(identifier);
        if (existingFunction != null)
        {
            // The function with the same name is already defined. Then, just verify that it has the same signature and
            // exit:
            existingFunction.VerifySignatureEquality(identifier, parametersInfo, returnType);
            return;
        }

        var method = scope.Context.ModuleType.DefineMethod(
            scope.Context,
            identifier,
            returnType.Resolve(scope.Context),
            parametersInfo);

        scope.Context.Functions.Add(identifier, new FunctionInfo(parametersInfo, returnType, method));
    }
}
