using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.Expressions;

internal interface IExpression
{
    IExpression Lower(IDeclarationScope scope);
    void EmitTo(IEmitScope scope);
    IType GetExpressionType(IDeclarationScope scope);
}
