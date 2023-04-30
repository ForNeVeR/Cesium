using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class ReturnStatement : IBlockItem
{
    public List<IBlockItem>? NextNodes { get; set; }
    public IBlockItem? Parent { get; set; }

    private readonly IExpression? _expression;

    public ReturnStatement(Ast.ReturnStatement statement)
    {
        _expression = statement.Expression?.ToIntermediate();
    }

    private ReturnStatement(IExpression? expression)
    {
        _expression = expression;
    }

    public void ResolveNextNodes(IBlockItem root, IBlockItem parent)
    {
        NextNodes = new();
    }

    public IEnumerable<IBlockItem> GetChildren(IBlockItem root)
    {
        yield break;
    }

    bool IBlockItem.HasDefiniteReturn => true;

    public IBlockItem Lower(IDeclarationScope scope) => new ReturnStatement(_expression?.Lower(scope));

    public void EmitTo(IEmitScope scope)
    {
        _expression?.EmitTo(scope);

        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    }
}
