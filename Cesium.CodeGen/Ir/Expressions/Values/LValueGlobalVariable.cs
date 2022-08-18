using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Expressions.Values
{
    internal class LValueGlobalVariable : ILValue
    {
        private readonly IType _type;
        private readonly FieldDefinition _definition;

        public LValueGlobalVariable(IType type, FieldDefinition definition)
        {
            _type = type;
            _definition = definition;
        }

        public void EmitGetValue(IDeclarationScope scope) =>
            scope.LdSFld(_definition);

        public void EmitGetAddress(IDeclarationScope scope) =>
            scope.LdSFldA(_definition);

        public void EmitSetValue(IDeclarationScope scope, IExpression value)
        {
            value.EmitTo(scope);
            scope.StSFld(_definition);
        }

        public IType GetValueType() => _type;
    }
}
