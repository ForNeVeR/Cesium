using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions.LValues
{
    internal class LValueGlobalVariable : ILValue
    {
        private readonly FieldDefinition _definition;

        public LValueGlobalVariable(FieldDefinition definition)
        {
            _definition = definition;
        }

        public void EmitGetValue(IDeclarationScope scope)
        {
            scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, _definition));
        }

        public void EmitGetAddress(IDeclarationScope scope)
        {
            scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldflda, _definition));
        }

        public void EmitSetValue(IDeclarationScope scope, IExpression value)
        {
            value.EmitTo(scope);

            scope.StFld(_definition);
        }

        public TypeReference GetValueType() => _definition.FieldType;
    }
}
