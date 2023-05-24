using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class FunctionDeclaration : IBlockItem
{
    public string Identifier { get; }
    public StorageClass StorageClass { get; }
    public FunctionType FunctionType { get; }
    public string? CliImportMemberName { get; }

    public FunctionDeclaration(string identifier, StorageClass storageStorageClass, FunctionType functionType, string? cliImportMemberName)
    {
        StorageClass = storageStorageClass;
        Identifier = identifier;
        FunctionType = functionType;
        CliImportMemberName = cliImportMemberName;
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        var resolvedFunctionType = (FunctionType)scope.ResolveType(FunctionType);
        var (parametersInfo, returnType) = resolvedFunctionType;
        if (CliImportMemberName != null)
        {
            if (parametersInfo is null or { Parameters.Count: 0, IsVoid: false })
                throw new CompilationException($"Empty parameter list is not allowed for CLI-imported function {Identifier}.");
        }

        var cliImportFunctionInfo = new FunctionInfo(parametersInfo, returnType, StorageClass, IsDefined: CliImportMemberName is not null)
        {
            CliImportMember = CliImportMemberName
        };
        scope.DeclareFunction(Identifier, cliImportFunctionInfo);
        return new FunctionDeclaration(Identifier, StorageClass, resolvedFunctionType, CliImportMemberName);
    }

    public void EmitTo(IEmitScope scope)
    {
        if (CliImportMemberName != null)
        {
            return;
        }

        EmitFunctionDeclaration(scope);
    }

    private void EmitFunctionDeclaration(
        IEmitScope scope)
    {
        var (parametersInfo, returnType) = FunctionType;
        var existingFunction = scope.Context.GetFunctionInfo(Identifier);
        if (existingFunction!.MethodReference is null)
        {
            scope.Context.DefineMethod(
                Identifier,
                StorageClass,
                returnType,
                parametersInfo);
        }
    }
}
