using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Expressions;

internal interface IExpression
{
    IExpression Lower();
    void EmitTo(IDeclarationScope scope);
    IType GetExpressionType(IDeclarationScope scope);
}
