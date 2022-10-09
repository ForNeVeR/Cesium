using Cesium.CodeGen.Contexts;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class GoToStatement : IBlockItem
{
    private readonly string _identifier;

    public GoToStatement(Ast.GoToStatement statement)
    {
        _identifier = statement.Identifier;
    }

    public GoToStatement(string identifier)
    {
        _identifier = identifier;
    }

    bool IBlockItem.HasDefiniteReturn => false;

    public IBlockItem Lower(IDeclarationScope scope) => this;

    public void EmitTo(IEmitScope scope)
    {
        var instruction = scope.ResolveLabel(_identifier);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Br, instruction));
    }
}
