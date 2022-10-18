using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class VariableDefinition : IBlockItem
{
    private readonly string _identifier;
    private readonly IType _type;
    private readonly IExpression? _initializer;

    public VariableDefinition(string identifier, IType type, IExpression? initializer)
    {
        _identifier = identifier;
        _type = type;
        _initializer = initializer;
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        return new VariableDefinition(_identifier, scope.ResolveType(_type), _initializer?.Lower(scope));
    }

    public void EmitTo(IEmitScope scope)
    {
        scope.AssemblyContext.AddGlobalField(_identifier, _type);
        var field = scope.AssemblyContext.ResolveGlobalField(_identifier, scope.Context);
        if (_initializer != null)
        {
            _initializer.EmitTo(scope);
            scope.StSFld(field);
        }
        else
        {
            if (_type is InPlaceArrayType arrayType)
            {
                arrayType.EmitInitializer(scope);
                scope.StSFld(field);
            }
        }
    }
}
