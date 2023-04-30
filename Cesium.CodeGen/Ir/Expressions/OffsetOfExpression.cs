using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal class OffsetOfExpression : IExpression
{
    private readonly StructType _resolvedType;
    private readonly LocalDeclarationInfo _fieldType;

    public OffsetOfExpression(StructType resolvedType, LocalDeclarationInfo fieldType)
    {
        _resolvedType = resolvedType;
        _fieldType = fieldType;
    }

    public IExpression Lower(IDeclarationScope scope) => this;

    public void EmitTo(IEmitScope scope)
    {
        var typeRef = _resolvedType.Resolve(scope.Context);
        var fieldRef = typeRef.Resolve().Fields.Single(f => f.Name == _fieldType.Identifier);

        VariableDefinition var;

        if (scope.Method.Body.Variables.FirstOrDefault(v => v.VariableType.IsEqualTo(typeRef)) is
            { } existingVar)
        {
            var = existingVar;
        }
        else
        {
            var = new VariableDefinition(typeRef);
            scope.Method.Body.Variables.Add(var);
        }

        scope.AddInstruction(OpCodes.Ldloca, var);
        scope.AddInstruction(OpCodes.Ldflda, fieldRef);
        scope.AddInstruction(OpCodes.Ldloca, var);
        scope.AddInstruction(OpCodes.Sub);
    }

    // todo: restore nativeint here
    public IType GetExpressionType(IDeclarationScope scope) => scope.CTypeSystem.Int;
    // public IType GetExpressionType(IDeclarationScope scope) => scope.CTypeSystem.NativeInt;
}
