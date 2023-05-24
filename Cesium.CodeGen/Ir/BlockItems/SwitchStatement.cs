using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class SwitchStatement : IBlockItem
{
    public IExpression Expression { get; }
    public IBlockItem Body { get; }

    public SwitchStatement(Ast.SwitchStatement statement)
    {
        var (expression, body) = statement;

        Expression = expression.ToIntermediate();
        Body = body.ToIntermediate();
    }

    public void EmitTo(IEmitScope scope) => throw new CompilationException("Should be lowered");
}
