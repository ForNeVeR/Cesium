using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class IfElseStatement : IBlockItem
{
    private readonly IExpression _expression;

    public IBlockItem TrueBranch { get; set; }
    public IBlockItem? FalseBranch { get; set; }

    public bool? IsEscapeBranchRequired { get; set; }

    public IfElseStatement(IExpression expression, IBlockItem trueBranch, IBlockItem? falseBranch)
    {
        _expression = expression;
        TrueBranch = trueBranch;
        FalseBranch = falseBranch;
    }

    public IfElseStatement(Ast.IfElseStatement statement)
    {
        var (expression, trueBranch, falseBranch) = statement;
        _expression = expression.ToIntermediate();
        TrueBranch = trueBranch.ToIntermediate();
        FalseBranch = falseBranch?.ToIntermediate();
    }

    public IBlockItem Lower(IDeclarationScope scope) => new IfElseStatement(_expression.Lower(scope), TrueBranch.Lower(scope), FalseBranch?.Lower(scope));

    public void EmitTo(IEmitScope scope)
    {
        if (IsEscapeBranchRequired == null)
            throw new CompilationException("CFG Graph pass missing");

        var bodyProcessor = scope.Method.Body.GetILProcessor();
        var ifFalseLabel = bodyProcessor.Create(OpCodes.Nop);

        _expression.EmitTo(scope);
        bodyProcessor.Emit(OpCodes.Brfalse, ifFalseLabel);

        TrueBranch.EmitTo(scope);

        if (FalseBranch == null)
        {
            bodyProcessor.Append(ifFalseLabel);
            return;
        }

        if (IsEscapeBranchRequired.Value)
        {
            var statementEndLabel = bodyProcessor.Create(OpCodes.Nop);
            bodyProcessor.Emit(OpCodes.Br, statementEndLabel);

            bodyProcessor.Append(ifFalseLabel);
            FalseBranch.EmitTo(scope);
            bodyProcessor.Append(statementEndLabel);
        }
        else
        {
            bodyProcessor.Append(ifFalseLabel);
            FalseBranch.EmitTo(scope);
        }
    }

    public bool TryUnsafeSubstitute(IBlockItem original, IBlockItem replacement)
    {
        if (TrueBranch == original)
        {
            TrueBranch = replacement;
            return true;
        }

        if (TrueBranch.TryUnsafeSubstitute(original, replacement))
            return true;

        if (FalseBranch == original)
        {
            FalseBranch = replacement;
            return true;
        }

        return FalseBranch?.TryUnsafeSubstitute(original, replacement) ?? false;
    }
}
