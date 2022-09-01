using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;

namespace Cesium.CodeGen.Ir.Expressions.Values
{
    internal class LValueGlobalVariable : ILValue
    {
        private readonly IType _type;
        private readonly string _name;
        private FieldDefinition? _definition;

        public LValueGlobalVariable(IType type, string name)
        {
            _type = type;
            _name = name;
        }

        public void EmitGetValue(IEmitScope scope) =>
            scope.LdSFld(GetVariableDefinition(scope));

        public void EmitGetAddress(IEmitScope scope) =>
            scope.LdSFldA(GetVariableDefinition(scope));

        public void EmitSetValue(IEmitScope scope, IExpression value)
        {
            value.EmitTo(scope);
            scope.StSFld(GetVariableDefinition(scope));
        }

        public IType GetValueType() => _type;

        private FieldDefinition GetVariableDefinition(IEmitScope scope)
        {
            if (_definition != null)
            {
                return _definition;
            }

            _definition = scope.Context.AssemblyContext.ResolveGlobalField(_name, scope.Context);
            return _definition;
        }
    }
}
