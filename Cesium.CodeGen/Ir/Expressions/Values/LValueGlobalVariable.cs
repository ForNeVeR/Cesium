using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.Values
{
    internal sealed class LValueGlobalVariable : ILValue
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
            if (value is CompoundInitializationExpression)
            {
                // for compound initialization copy memory.s
                scope.AddInstruction(OpCodes.Ldflda, GetVariableReference(scope));
                var expression = ((InPlaceArrayType)_type).GetSizeInBytesExpression(scope.AssemblyContext.ArchitectureSet);
                expression.EmitTo(scope);
                scope.AddInstruction(OpCodes.Conv_U);

                var initializeCompoundMethod = scope.Context.GetRuntimeHelperMethod("InitializeCompound");
                scope.AddInstruction(OpCodes.Call, initializeCompoundMethod);
            }
            else
            {
                // Regular initialization.
                scope.StSFld(GetVariableReference(scope));
            }
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
