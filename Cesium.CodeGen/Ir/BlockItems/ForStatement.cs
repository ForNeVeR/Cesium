using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class ForStatement : LoopStatement, IBlockItem
{
    private readonly IBlockItem? _initDeclaration;
    private readonly IExpression? _initExpression;
    private readonly IExpression? _testExpression;
    private readonly IExpression? _updateExpression;
    public IBlockItem Body { get; set; }
    public string? BreakLabel { get; }
    public string? ContinueLabel { get; }

    public ForStatement(Ast.ForStatement statement)
    {
        var (initDeclaration, initExpression, testExpression, updateExpression, body) = statement;
        _initDeclaration = initDeclaration?.ToIntermediate();
        _initExpression = initExpression?.ToIntermediate();

        if (_initDeclaration != null && _initExpression != null)
            throw new CompilationException("for statement: can't have both init declaration and expression");

        _testExpression = testExpression?.ToIntermediate();
        _updateExpression = updateExpression?.ToIntermediate();
        Body = body.ToIntermediate();
    }

    public override IBlockItem Lower(IDeclarationScope scope)
    {
        var breakLabel = Guid.NewGuid().ToString();
        var continueLabel = Guid.NewGuid().ToString();

        var loopScope = new BlockScope((IEmitScope)scope, breakLabel, continueLabel);

        return MakeLoop(
            loopScope,
            _initDeclaration ?? new ExpressionStatement(_initExpression),
            _testExpression,
            _updateExpression,
            Body,
            breakLabel,
            null,
            null,
            continueLabel
        );
    }
}
