using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class CompoundInitializationFunctionCallExpression : FunctionCallExpressionBase
{
    public CompoundInitializationFunctionCallExpression(IExpression target, IExpression source, IExpression size)
    {
        Target = target;
        Source = source;
        Size = size;
    }

    public IExpression Target { get; }
    public IExpression Source { get; }
    public IExpression Size { get; }

    public override void EmitTo(IEmitScope scope)
    {
        base.EmitArgumentList(
            scope,
            new([
                new(CTypeSystem.NativeInt, "src", 0),
                new(CTypeSystem.NativeInt, "target", 1),
                new(CTypeSystem.Int, "size", 2)], true, false),
            [Source, Target, Size]);
        var initializeCompoundMethod = scope.Context.GetRuntimeHelperMethod("InitializeCompound");
        scope.AddInstruction(OpCodes.Call, initializeCompoundMethod);
    }

    public override IType GetExpressionType(IDeclarationScope scope) => CTypeSystem.Void;

    public override IExpression Lower(IDeclarationScope scope)
    {
        return new CompoundInitializationFunctionCallExpression(
            Target.Lower(scope),
            Source.Lower(scope),
            Size.Lower(scope));
    }
}
