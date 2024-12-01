using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal sealed class ForStatement : IBlockItem
{
    public IBlockItem? InitDeclaration { get; }
    public IExpression? InitExpression { get; }
    public IExpression? TestExpression { get; }
    public IExpression? UpdateExpression { get; }
    public IBlockItem Body { get; }

    public ForStatement(Ast.ForStatement statement, IDeclarationScope scope)
    {
        var (initDeclaration, initExpression, testExpression, updateExpression, body) = statement;
        InitDeclaration = initDeclaration?.ToIntermediate(scope);
        InitExpression = initExpression?.ToIntermediate(scope);

        if (InitDeclaration != null && InitExpression != null)
            throw new CompilationException("for statement: can't have both init declaration and expression");

        TestExpression = testExpression?.ToIntermediate(scope);
        UpdateExpression = updateExpression?.ToIntermediate(scope);
        Body = body.ToIntermediate(scope);
    }
}
