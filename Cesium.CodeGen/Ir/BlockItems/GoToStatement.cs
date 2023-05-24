using Cesium.CodeGen.Contexts;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class GoToStatement : IBlockItem
{
    public string Identifier { get; }

    public GoToStatement(Ast.GoToStatement statement)
    {
        Identifier = statement.Identifier;
    }

    public GoToStatement(string identifier)
    {
        Identifier = identifier;
    }

    public void EmitTo(IEmitScope scope)
    {
        var instruction = scope.ResolveLabel(Identifier);
        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Br, instruction));
    }
}
