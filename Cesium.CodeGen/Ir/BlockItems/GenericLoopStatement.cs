using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Expressions;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal record GenericLoopStatement(
    LoopScope Scope,
    IBlockItem? Initializer,
    IExpression? TestExpression,
    IExpression? UpdateExpression,
    IBlockItem Body,
    string BreakLabel,
    string TestConditionLabel,
    string? LoopBodyLabel,
    string? UpdateLabel
) : IBlockItem
{
    public IBlockItem Lower(IDeclarationScope scope) => this;

    bool IBlockItem.HasDefiniteReturn => Body.HasDefiniteReturn;

    public void EmitTo(IEmitScope unused)
    {
        var loopScope = Scope;

        var bodyProcessor = loopScope.Method.Body.GetILProcessor();

        Initializer?.EmitTo(loopScope);

        var loopIterationStart = loopScope.ResolveLabel(TestConditionLabel);
        bodyProcessor.Append(loopIterationStart);

        var loopExit = loopScope.ResolveLabel(BreakLabel);

        if (TestExpression != null)
        {
            TestExpression.EmitTo(loopScope);

            bodyProcessor.Emit(OpCodes.Brfalse, loopExit);
        }

        if (LoopBodyLabel != null)
            bodyProcessor.Append(loopScope.ResolveLabel(LoopBodyLabel));

        Body.EmitTo(loopScope);

        if (UpdateLabel != null)
            bodyProcessor.Append(loopScope.ResolveLabel(UpdateLabel));

        UpdateExpression?.EmitTo(loopScope);

        var brToTest = bodyProcessor.Create(OpCodes.Br, loopIterationStart);
        bodyProcessor.Append(brToTest);
        bodyProcessor.Append(loopExit);
    }
}
