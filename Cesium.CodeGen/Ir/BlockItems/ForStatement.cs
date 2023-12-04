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

    public ForStatement(Ast.ForStatement statement)
    {
        var (initDeclaration, initExpression, testExpression, updateExpression, body) = statement;
        InitDeclaration = initDeclaration?.ToIntermediate();
        InitExpression = initExpression?.ToIntermediate();

        if (InitDeclaration != null && InitExpression != null)
            throw new CompilationException("for statement: can't have both init declaration and expression");

        TestExpression = testExpression?.ToIntermediate();
        UpdateExpression = updateExpression?.ToIntermediate();
        Body = body.ToIntermediate();
    }
}
