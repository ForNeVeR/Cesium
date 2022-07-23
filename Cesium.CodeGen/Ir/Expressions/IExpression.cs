using Cesium.CodeGen.Contexts;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Expressions;

internal interface IExpression
{
    IExpression Lower();
    void EmitTo(IDeclarationScope scope);
    TypeReference GetExpressionType(IDeclarationScope scope);
}
