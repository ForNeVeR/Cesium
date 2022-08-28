using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions;

internal interface IExpression
{
    IExpression Lower(IDeclarationScope scope);
    void EmitTo(IDeclarationScope scope);
    IType GetExpressionType(IDeclarationScope scope);
}
