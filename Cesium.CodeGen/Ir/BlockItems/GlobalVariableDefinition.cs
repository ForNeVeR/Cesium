using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.BlockItems;

/// <summary>Either an assembly-global or a file-level ("static") variable.</summary>
internal sealed record GlobalVariableDefinition(
    StorageClass StorageClass,
    IType Type,
    string Identifier,
    IExpression? Initializer) : IBlockItem
{
}
