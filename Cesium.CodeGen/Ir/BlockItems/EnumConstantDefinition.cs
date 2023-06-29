using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.BlockItems;

internal record EnumConstantDefinition(string Identifier, IType Type, IExpression Value) : IBlockItem;
