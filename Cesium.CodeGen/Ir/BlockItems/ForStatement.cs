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
    private readonly IBlockItem _body;

    public ForStatement(Ast.ForStatement statement)
    {
        var (initDeclaration, initExpression, testExpression, updateExpression, body) = statement;
        _initDeclaration = initDeclaration?.ToIntermediate();
        _initExpression = initExpression?.ToIntermediate();

        if (_initDeclaration != null && _initExpression != null)
            throw new CompilationException("for statement: can't have both init declaration and expression");

        _testExpression = testExpression?.ToIntermediate();
        _updateExpression = updateExpression?.ToIntermediate();
        _body = body.ToIntermediate();
    }

    public override IBlockItem Lower(IDeclarationScope scope)
    {
        var loopScope = new LoopScope((IEmitScope)scope);
        var breakLabel = loopScope.GetBreakLabel();
        var continueLabel = loopScope.GetContinueLabel();

        return MakeLoop(
            loopScope,
            _initDeclaration ?? new ExpressionStatement(_initExpression),
            _testExpression,
            _updateExpression,
            _body,
            breakLabel,
            null,
            null,
            continueLabel
        );
    }

    bool IBlockItem.HasDefiniteReturn => _body.HasDefiniteReturn;
}
