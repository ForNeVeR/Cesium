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
        private FieldReference? _field;

        public LValueGlobalVariable(IType type, string name)
        {
            _type = type;
            _name = name;
        }

        public void EmitGetValue(IEmitScope scope) =>
            scope.LdSFld(GetVariableReference(scope));

        public void EmitGetAddress(IEmitScope scope) =>
            scope.LdSFldA(GetVariableReference(scope));

        public void EmitSetValue(IEmitScope scope, IExpression value)
        {
            value.EmitTo(scope);
            scope.StSFld(GetVariableReference(scope));
        }

        public IType GetValueType() => _type;

        private FieldReference GetVariableReference(IEmitScope scope)
        {
            if (_field != null)
            {
                return _field;
            }

            _field = scope.ResolveGlobalField(_name);
            return _field;
        }
    }
}
