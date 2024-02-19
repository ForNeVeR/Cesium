using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class IndirectFunctionCallExpression : FunctionCallExpressionBase
{
    private readonly IReadOnlyList<IExpression> _arguments;
    private readonly IExpression _callee;
    private readonly FunctionType _calleeType;

    public IndirectFunctionCallExpression(IExpression callee, FunctionType calleeType, IReadOnlyList<IExpression> arguments)
    {
        _arguments = arguments;
        _callee = callee;
        _calleeType = calleeType;
    }

    public override IExpression Lower(IDeclarationScope scope) => this;

    public override void EmitTo(IEmitScope scope)
    {
        if (_callee == null)
            throw new AssertException("Should be lowered");

        EmitArgumentList(scope, _calleeType.Parameters, _arguments);

        _callee.EmitTo(scope);

        var callSite = new CallSite(_calleeType.ReturnType.Resolve(scope.Context));

        if (_calleeType.Parameters != null)
        {
            foreach (var param in _calleeType.Parameters.Parameters)
            {
                callSite.Parameters.Add(new ParameterDefinition(param.Type.Resolve(scope.Context)));
            }

            if (_calleeType.Parameters.IsVarArg)
            {
                callSite.Parameters.Add(new ParameterDefinition(scope.Context.TypeSystem.IntPtr));
            }
        }

        scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Calli, callSite));
    }

    public override IType GetExpressionType(IDeclarationScope scope)
    {
        return _calleeType.ReturnType;
    }
}
