using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class VariableDefinition : IBlockItem
{
    private readonly string identifier;
    private readonly IType type;
    private readonly IExpression? initializer;

    public VariableDefinition(string identifier, IType type, IExpression? initializer)
    {
        this.identifier = identifier;
        this.type = type;
        this.initializer = initializer;
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        return new VariableDefinition(identifier, scope.ResolveType(type), initializer?.Lower(scope));
    }

    public void EmitTo(IEmitScope scope)
    {
        scope.AssemblyContext.AddGlobalField(identifier, type);
        if (initializer != null)
        {
            var field = scope.AssemblyContext.ResolveGlobalField(identifier, scope.Context);
            initializer.EmitTo(scope);
            scope.StSFld(field);
        }
    }
}
