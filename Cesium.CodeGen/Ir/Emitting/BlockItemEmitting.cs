// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.ControlFlow;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;
using Mono.Cecil.Cil;
using System.Diagnostics;

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
            case IfElseStatement:
            {
                throw new AssertException("Should be lowered");
            }
            case BasicBlock s:
            {
                foreach (var item in s.Statements)
                {
                    EmitCode(scope, item);
                }

                return;
            }
            case CompoundStatement s:
            {
                Debug.Assert(s.EmitScope == null || s.EmitScope == scope, "EmitScope should be function scope after Lineralize");

                foreach (var item in s.Statements)
                {
                    EmitCode(scope, item);
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
            case ConditionalGotoStatement s:
            {
                s.Condition.EmitTo(scope);
                var instruction = scope.ResolveLabel(s.Identifier);
                var opcode = s.JumpType == ConditionalJumpType.True ? OpCodes.Brtrue : OpCodes.Brfalse;
                scope.Method.Body.Instructions.Add(Instruction.Create(opcode, instruction));
                return;
            }
            case LabelStatement s:
            {
                var instruction = scope.ResolveLabel(s.Identifier);
                scope.Method.Body.Instructions.Add(instruction);

                EmitCode(scope, s.Expression);

                return;
            }
            case LabeledNopStatement s:
            {
                var instruction = scope.ResolveLabel(s.Label);
                scope.Method.Body.Instructions.Add(instruction);
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
                    if (type is StructType g)
                        scope.Context.GenerateType(identifier!, g);
                }

                return;
            }
            case TypeDefBlockItem t:
            {
                foreach (var typeDef in t.Types)
                {
                    var (type, identifier, _) = typeDef;
                    if (type is StructType g)
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
