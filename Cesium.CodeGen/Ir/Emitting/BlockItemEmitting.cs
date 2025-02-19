// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.Emitting;

internal static class BlockItemEmitting
{
    public static void EmitCode(IEmitScope scope, IBlockItem blockItem)
    {
        switch (blockItem)
        {
            case AmbiguousBlockItem:
            {
                throw new WipException(213, "Ambiguous variable declarations aren't supported, yet.");
            }
            case BreakStatement:
            case CaseStatement:
            case ContinueStatement:
            case DoWhileStatement:
            case WhileStatement:
            case ForStatement:
            case SwitchStatement:
            case DeclarationBlockItem:
            {
                throw new AssertException("Should be lowered");
            }
            case CompoundStatement s:
            {
                var realScope = s.EmitScope ?? scope;

                foreach (var item in s.Statements)
                {
                    EmitCode(realScope, item);
                }

                return;
            }
            case ExpressionStatement s:
            {
                s.Expression?.EmitTo(scope);

                return;
            }
            case FunctionDeclaration d:
            {
                if (d.CliImportMemberName != null)
                    return;

                var (parametersInfo, returnType) = d.FunctionType;
                var existingFunction = scope.Context.GetFunctionInfo(d.Identifier);
                if (existingFunction!.MethodReference is null)
                {
                    scope.Context.DefineMethod(
                        d.Identifier,
                        d.StorageClass,
                        returnType,
                        parametersInfo);
                }

                return;
            }
            case FunctionDefinition functionDefinition:
            {
                // too large to put here
                functionDefinition.EmitCode(scope);

                return;
            }
            case GlobalVariableDefinition d:
            {
                scope.ResolveGlobalField(d.Identifier);
                return;
            }
            case EnumConstantDefinition:
                // This is fake declaration
                break;
            case GoToStatement s:
            {
                var instruction = scope.ResolveLabel(s.Identifier);
                scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Br, instruction));

                return;
            }
            case IfElseStatement s:
            {
                if (s.IsEscapeBranchRequired == null)
                    throw new CompilationException("CFG Graph pass missing");

                var bodyProcessor = scope.Method.Body.GetILProcessor();
                var ifFalseLabel = bodyProcessor.Create(OpCodes.Nop);

                s.Expression.EmitTo(scope);
                bodyProcessor.Emit(OpCodes.Brfalse, ifFalseLabel);

                EmitCode(scope, s.TrueBranch);

                if (s.FalseBranch == null)
                {
                    bodyProcessor.Append(ifFalseLabel);
                    return;
                }

                if (s.IsEscapeBranchRequired.Value)
                {
                    var statementEndLabel = bodyProcessor.Create(OpCodes.Nop);
                    bodyProcessor.Emit(OpCodes.Br, statementEndLabel);

                    bodyProcessor.Append(ifFalseLabel);
                    EmitCode(scope, s.FalseBranch);
                    bodyProcessor.Append(statementEndLabel);
                }
                else
                {
                    bodyProcessor.Append(ifFalseLabel);
                    EmitCode(scope, s.FalseBranch);
                }

                return;
            }
            case LabelStatement s:
            {
                var instruction = scope.ResolveLabel(s.Identifier);
                scope.Method.Body.Instructions.Add(instruction);

                EmitCode(scope, s.Expression);

                return;
            }
            case ReturnStatement s:
            {
                s.Expression?.EmitTo(scope);
                scope.Method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

                return;
            }
            case TagBlockItem t:
            {
                foreach (var typeDef in t.Types)
                {
                    var (type, identifier, _) = typeDef;
                    if (type is IGeneratedType g)
                        scope.Context.GenerateType(identifier!, g);
                }

                return;
            }
            case TypeDefBlockItem t:
            {
                foreach (var typeDef in t.Types)
                {
                    var (type, identifier, _) = typeDef;
                    if (type is IGeneratedType g)
                        scope.Context.GenerateType(identifier!, g);
                }

                return;
            }
            case PInvokeDefinition:
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(blockItem));
        }
    }
}
