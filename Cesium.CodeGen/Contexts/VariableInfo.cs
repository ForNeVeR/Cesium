using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Contexts;

internal record VariableInfo(string Identifier, StorageClass StorageClass, IType Type, IExpression? Constant)
{
    static int CurrentIndex = 0;
    public int Index { get; } = CurrentIndex++;
}
