// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Expressions;

internal sealed class IndirectFunctionCallExpression : FunctionCallExpressionBase
{
    private readonly FunctionType _calleeType;

    internal IExpression Callee { get; }

    internal IReadOnlyList<IExpression> Arguments { get; }

    public IndirectFunctionCallExpression(IExpression callee, FunctionType calleeType, IReadOnlyList<IExpression> arguments)
    {
        Arguments = arguments;
        Callee = callee;
        _calleeType = calleeType;
    }

    public override IExpression Lower(IDeclarationScope scope) => this;

    public override void EmitTo(IEmitScope scope)
    {
        if (Callee == null)
            throw new AssertException("Should be lowered");

        EmitArgumentList(scope, _calleeType.Parameters, Arguments);

        Callee.EmitTo(scope);

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
