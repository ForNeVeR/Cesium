using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Contexts;

internal record VariableInfo(string Identifier, StorageClass StorageClass, IType Type)
{
}
