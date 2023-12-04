using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.BlockItems;

internal sealed class FunctionDeclaration : IBlockItem
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
}
