using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class FunctionDeclaration : IBlockItem
{
    private readonly string _identifier;
    private readonly FunctionType _functionType;
    private readonly string? _cliImportMemberName;

    public FunctionDeclaration(string identifier, FunctionType functionType, string? cliImportMemberName)
    {
        this._identifier = identifier;
        this._functionType = functionType;
        this._cliImportMemberName = cliImportMemberName;
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        return this;
    }

    public void EmitTo(IEmitScope scope)
    {
        if (_cliImportMemberName != null)
        {
            EmitCliImportDeclaration(scope, _cliImportMemberName);
            return;
        }

        EmitFunctionDeclaration(scope);
    }

    private void EmitCliImportDeclaration(
        IEmitScope scope,
        string cliImportMemberName)
    {
        var (parametersInfo, returnType) = _functionType;
        if (parametersInfo is null or { Parameters.Count: 0, IsVoid: false })
            throw new CompilationException($"Empty parameter list is not allowed for CLI-imported function {_identifier}.");

        var method = scope.Context.MethodLookup(cliImportMemberName, parametersInfo, returnType);
        var cliImportFunctionInfo = new FunctionInfo(parametersInfo, returnType, method, IsDefined: true);
        if (!scope.Context.Functions.TryGetValue(_identifier, out var existingDeclaration))
        {
            scope.Context.Functions.Add(_identifier, cliImportFunctionInfo);
            return;
        }

        cliImportFunctionInfo.VerifySignatureEquality(_identifier, existingDeclaration.Parameters, existingDeclaration.ReturnType);
        if (!cliImportFunctionInfo.MethodReference.FullName.Equals(existingDeclaration.MethodReference.FullName))
        {
            throw new CompilationException($"Function {_identifier} already defined as as CLI-import with {existingDeclaration.MethodReference.FullName}.");
        }
    }

    private void EmitFunctionDeclaration(
        IEmitScope scope)
    {
        var (parametersInfo, returnType) = _functionType;
        var existingFunction = scope.Context.Functions.GetValueOrDefault(_identifier);
        if (existingFunction != null)
        {
            // The function with the same name is already defined. Then, just verify that it has the same signature and
            // exit:
            existingFunction.VerifySignatureEquality(_identifier, parametersInfo, returnType);
            return;
        }

        var method = scope.Context.ModuleType.DefineMethod(
            scope.Context,
            _identifier,
            returnType.Resolve(scope.Context),
            parametersInfo);

        scope.Context.Functions.Add(_identifier, new FunctionInfo(parametersInfo, returnType, method));
    }
}
