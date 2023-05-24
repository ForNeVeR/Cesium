using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class FunctionDeclaration : IBlockItem
{
    private readonly string _identifier;
    private readonly StorageClass _storageClass;
    private readonly FunctionType _functionType;
    private readonly string? _cliImportMemberName;

    public FunctionDeclaration(string identifier, StorageClass storageClass, FunctionType functionType, string? cliImportMemberName)
    {
        _storageClass = storageClass;
        _identifier = identifier;
        _functionType = functionType;
        _cliImportMemberName = cliImportMemberName;
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        var resolvedFunctionType = (FunctionType)scope.ResolveType(_functionType);
        var (parametersInfo, returnType) = resolvedFunctionType;
        if (_cliImportMemberName != null)
        {
            if (parametersInfo is null or { Parameters.Count: 0, IsVoid: false })
                throw new CompilationException($"Empty parameter list is not allowed for CLI-imported function {_identifier}.");
        }

        var cliImportFunctionInfo = new FunctionInfo(parametersInfo, returnType, _storageClass, IsDefined: _cliImportMemberName is not null)
        {
            CliImportMember = _cliImportMemberName
        };
        scope.DeclareFunction(_identifier, cliImportFunctionInfo);
        return new FunctionDeclaration(_identifier, _storageClass, resolvedFunctionType, _cliImportMemberName);
    }

    public void EmitTo(IEmitScope scope)
    {
        if (_cliImportMemberName != null)
        {
            return;
        }

        EmitFunctionDeclaration(scope);
    }

    private void EmitFunctionDeclaration(
        IEmitScope scope)
    {
        var (parametersInfo, returnType) = _functionType;
        var existingFunction = scope.Context.GetFunctionInfo(_identifier);
        if (existingFunction!.MethodReference is null)
        {
            scope.Context.DefineMethod(
                _identifier,
                _storageClass,
                returnType,
                parametersInfo);
        }
    }
}
