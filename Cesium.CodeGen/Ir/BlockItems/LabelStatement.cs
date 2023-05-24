using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal record LabelStatement : IBlockItem
{
    public IBlockItem Expression { get; set; }
    public string Identifier { get; }
    private readonly bool _didLowered;

    public LabelStatement(Ast.LabelStatement statement)
    {
        Expression = statement.Body.ToIntermediate();
        Identifier = statement.Identifier;
    }

    public LabelStatement(string identifier, IBlockItem expression, bool didLowered = false)
    {
        Identifier = identifier;
        Expression = expression;
        _didLowered = didLowered;
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        // TODO[#201]: Remove side effects from Lower, migrate labels to a separate compilation stage.
        if (!_didLowered)
            scope.AddLabel(Identifier);
        return new LabelStatement(Identifier, Expression.Lower(scope), true);
    }

    public void EmitTo(IEmitScope scope)
    {
        var instruction = scope.ResolveLabel(Identifier);
        scope.Method.Body.Instructions.Add(instruction);
        Expression.EmitTo(scope);
    }

    public bool TryUnsafeSubstitute(IBlockItem original, IBlockItem replacement)
    {
        if (Expression == original)
        {
            Expression = replacement;
            return true;
        }

        return Expression.TryUnsafeSubstitute(original, replacement);
    }
}
