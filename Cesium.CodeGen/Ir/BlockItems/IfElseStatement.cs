using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal record IfElseStatement : IBlockItem
{
    public IExpression Expression { get; init; }
    public IBlockItem TrueBranch { get; init; }
    public IBlockItem? FalseBranch { get; init; }

    public bool? IsEscapeBranchRequired { get; set; }

    public IfElseStatement(IExpression expression, IBlockItem trueBranch, IBlockItem? falseBranch)
    {
        Expression = expression;
        TrueBranch = trueBranch;
        FalseBranch = falseBranch;
    }

    public IfElseStatement(Ast.IfElseStatement statement)
    {
        var (expression, trueBranch, falseBranch) = statement;
        Expression = expression.ToIntermediate();
        TrueBranch = trueBranch.ToIntermediate();
        FalseBranch = falseBranch?.ToIntermediate();
    }

    public IBlockItem Lower(IDeclarationScope scope) => new IfElseStatement(Expression.Lower(scope), TrueBranch.Lower(scope), FalseBranch?.Lower(scope));

    public void EmitTo(IEmitScope scope)
    {
        if (IsEscapeBranchRequired == null)
            throw new CompilationException("CFG Graph pass missing");

        var bodyProcessor = scope.Method.Body.GetILProcessor();
        var ifFalseLabel = bodyProcessor.Create(OpCodes.Nop);

        Expression.EmitTo(scope);
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
}
