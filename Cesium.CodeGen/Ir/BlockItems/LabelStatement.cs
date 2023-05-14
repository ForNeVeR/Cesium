using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class LabelStatement : IBlockItem
{
    private readonly IBlockItem _expression;
    private readonly string _identifier;

    public LabelStatement(Ast.LabelStatement statement)
    {
        _expression = statement.Body.ToIntermediate();
        _identifier = statement.Identifier;
    }

    public LabelStatement(string identifier, IBlockItem expression)
    {
        _identifier = identifier;
        _expression = expression;
    }

    bool IBlockItem.HasDefiniteReturn => _expression.HasDefiniteReturn;

    public IBlockItem Lower(IDeclarationScope scope)
    {
        // TODO[#201]: Remove side effects from Lower, migrate labels to a separate compilation stage.
        scope.AddLabel(_identifier);
        return new LabelStatement(_identifier, _expression.Lower(scope));
    }

    public void EmitTo(IEmitScope scope)
    {
        var instruction = scope.ResolveLabel(_identifier);
        scope.Method.Body.Instructions.Add(instruction);
        _expression.EmitTo(scope);
    }
}
