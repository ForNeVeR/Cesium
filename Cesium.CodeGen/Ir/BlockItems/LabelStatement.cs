using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal record LabelStatement : IBlockItem
{
    public IBlockItem Expression { get; init; }
    public bool DidLowered { get; }
    public string Identifier { get; }

    public LabelStatement(Ast.LabelStatement statement)
    {
        Expression = statement.Body.ToIntermediate();
        Identifier = statement.Identifier;
    }

    public LabelStatement(string identifier, IBlockItem expression, bool didLowered = false)
    {
        Identifier = identifier;
        Expression = expression;
        DidLowered = didLowered;
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        // TODO[#201]: Remove side effects from Lower, migrate labels to a separate compilation stage.
        if (!DidLowered)
            scope.AddLabel(Identifier);
        return new LabelStatement(Identifier, Expression.Lower(scope), true);
    }

    public void EmitTo(IEmitScope scope)
    {
        var instruction = scope.ResolveLabel(Identifier);
        scope.Method.Body.Instructions.Add(instruction);
        Expression.EmitTo(scope);
    }
}
