using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Mono.Cecil.Cil;

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

    private LabelStatement(string identifier, IBlockItem expression)
    {
        _identifier = identifier;
        _expression = expression;
    }

    bool IBlockItem.HasDefiniteReturn => _expression.HasDefiniteReturn;

    public IBlockItem Lower(IDeclarationScope scope)
    {
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
