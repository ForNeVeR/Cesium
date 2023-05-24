using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class ForStatement : LoopStatement, IBlockItem
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

    public override IBlockItem Lower(IDeclarationScope scope)
    {
        var breakLabel = Guid.NewGuid().ToString();
        var continueLabel = Guid.NewGuid().ToString();

        var loopScope = new BlockScope((IEmitScope)scope, breakLabel, continueLabel);

        return MakeLoop(
            loopScope,
            InitDeclaration ?? new ExpressionStatement(InitExpression),
            TestExpression,
            UpdateExpression,
            Body,
            breakLabel,
            null,
            null,
            continueLabel
        );
    }
}
