using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;

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
