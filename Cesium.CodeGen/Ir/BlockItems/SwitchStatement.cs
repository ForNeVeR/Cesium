using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.Constants;
using Cesium.Core;
using Mono.Cecil.Cil;
using System.Diagnostics;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class SwitchStatement : IBlockItem
{
    private readonly IExpression _expression;
    private readonly IBlockItem _body;
    private readonly string? _breakLabel;

    public SwitchStatement(Ast.SwitchStatement statement)
    {
        var (expression, body) = statement;

        _expression = expression.ToIntermediate();
        _body = body.ToIntermediate();
    }

    private SwitchStatement(
        IExpression expression,
        IBlockItem body,
        string breakLabel)
    {
        _expression = expression;
        _body = body;
        _breakLabel = breakLabel;
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        var switchScope = new SwitchScope((IEmitScope)scope);
        var breakLabel = switchScope.GetBreakLabel();
        // TODO[#201]: Remove side effects from Lower, migrate labels to a separate compilation stage.
        scope.AddLabel(breakLabel);
        List<IBlockItem> linearizedCases = new();
        CompoundStatement? compoundStatement = _body as CompoundStatement;
        if (compoundStatement is null)
        {
            compoundStatement = new CompoundStatement(new List<IBlockItem>(){ _body });
        }

        foreach (var statement in compoundStatement.Statements)
        {
            AddStatement(linearizedCases, statement);
        }

        foreach (var statement in linearizedCases)
        {
            if (statement is CaseStatement caseStatement && caseStatement.Expression is not null)
            {
                var expression = caseStatement.Expression;
                var evaluator = new ConstantEvaluator(expression);
                var constant = evaluator.GetConstantValue();
                if (constant is not IntegerConstant integerConstant && constant is not CharConstant charConstant)
                {
                    throw new CompilationException("Constant expression should be convertable to integer value");
                }

                // scope.AddLabel(breakLabel + "_" + integerConstant.Value);
            }
        }

        return new SwitchStatement(
            _expression.Lower(switchScope),
            new CompoundStatement(linearizedCases).Lower(switchScope),
            breakLabel);
    }

    private static void AddStatement(List<IBlockItem> linearizedCases, IBlockItem statement)
    {
        if (statement is CaseStatement caseStatement)
        {
            if (caseStatement.Statement is CaseStatement)
            {
                linearizedCases.Add(new CaseStatement(caseStatement.Expression, new CompoundStatement(new List<IBlockItem>())));
                AddStatement(linearizedCases, caseStatement.Statement);
            }
            else
            {
                linearizedCases.Add(statement);
            }
        }
        else
        {
            linearizedCases.Add(statement);
        }
    }

    bool IBlockItem.HasDefiniteReturn => _body.HasDefiniteReturn;

    public void EmitTo(IEmitScope scope)
    {
        Debug.Assert(_breakLabel != null);
        var switchScope = new SwitchScope(scope);
        var bodyProcessor = switchScope.Method.Body.GetILProcessor();

        var compoundStatement = _body as CompoundStatement;
        Debug.Assert(compoundStatement != null);
        Dictionary<IExpression, Instruction> caseExpressions = new();
        Instruction? defaultCaseInstruction = Instruction.Create(OpCodes.Nop);
        foreach (var caseStatement in compoundStatement.Statements.OfType<CaseStatement>())
        {
            if (caseStatement.Expression != null)
            {
                caseExpressions.Add(caseStatement.Expression, Instruction.Create(OpCodes.Nop));
            }
            else
            {
                defaultCaseInstruction = Instruction.Create(OpCodes.Nop);
            }
        }

        _expression.EmitTo(switchScope);
        var intermediateVar = new Mono.Cecil.Cil.VariableDefinition(switchScope.Context.TypeSystem.Int32);
        switchScope.Method.Body.Variables.Add(intermediateVar);
        bodyProcessor.Emit(OpCodes.Stloc, intermediateVar);
        foreach (var caseExpression in caseExpressions)
        {
            bodyProcessor.Emit(OpCodes.Ldloc, intermediateVar);
            caseExpression.Key.EmitTo(switchScope);
            bodyProcessor.Emit(OpCodes.Ceq);
            bodyProcessor.Emit(OpCodes.Brtrue, caseExpression.Value);
        }

        var exitLoop = switchScope.ResolveLabel(_breakLabel);
        if (defaultCaseInstruction is null)
        {
            bodyProcessor.Emit(OpCodes.Br, exitLoop);
        }
        else
        {
            bodyProcessor.Emit(OpCodes.Br, defaultCaseInstruction);
        }

        foreach (var statement in compoundStatement.Statements)
        {
            if (statement is CaseStatement caseStatement)
            {
                if (caseStatement.Expression != null)
                {
                    bodyProcessor.Append(caseExpressions[caseStatement.Expression]);
                }
                else
                {
                    bodyProcessor.Append(defaultCaseInstruction!);
                }

                caseStatement.Statement.EmitTo(switchScope);
            }
            else
            {
                statement.Lower(switchScope);
                statement.EmitTo(switchScope);
            }
        }

        bodyProcessor.Append(exitLoop);
    }
}
