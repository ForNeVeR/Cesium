using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Contexts.Meta;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.BlockItems;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.Lowering;

internal static class BlockItemLowering
{
    private static IBlockItem MakeLoop(
            BlockScope scope,
            IBlockItem? initializer,
            IExpression? testExpression,
            IExpression? updateExpression,
            IBlockItem body,
            string breakLabel,
            string? testConditionLabel,
            string? loopBodyLabel,
            string? updateLabel
        )
    {
        var stmts = new List<IBlockItem>();

        if (initializer != null)
            stmts.Add(initializer);

        testConditionLabel ??= Guid.NewGuid().ToString();

        stmts.Add(new LabelStatement(testConditionLabel, new ExpressionStatement((IExpression?)null)));

        if (testExpression != null)
        {
            stmts.Add(new IfElseStatement(new UnaryOperatorExpression(UnaryOperator.LogicalNot, testExpression), new GoToStatement(breakLabel), null));
        }

        if (loopBodyLabel != null)
            stmts.Add(new LabelStatement(loopBodyLabel, body));
        else
            stmts.Add(body);

        var updateStmt = new ExpressionStatement(updateExpression);

        if (updateLabel != null)
            stmts.Add(new LabelStatement(updateLabel, updateStmt));
        else
            stmts.Add(updateStmt);

        stmts.Add(new GoToStatement(testConditionLabel));
        stmts.Add(new LabelStatement(breakLabel, new ExpressionStatement((IExpression?)null)));

        return Lower(scope, new CompoundStatement(stmts, scope));
    }

    public static CompoundStatement LowerBody(FunctionScope scope, IBlockItem blockItem)
    {
        CompoundStatement compoundStatement = (CompoundStatement)Lower(scope, blockItem);
        var linearizedStatement = new CompoundStatement(Linearize(compoundStatement, scope).ToList(), scope);
        return linearizedStatement;
    }

    public static IBlockItem LowerDeclaration(IDeclarationScope scope, IBlockItem blockItem)
    {
        return Lower(scope, blockItem);
    }

    private static IEnumerable<IBlockItem> Linearize(CompoundStatement compoundStatement, FunctionScope scope)
    {
        Debug.Assert(compoundStatement.EmitScope != null);
        BlockScope currentScope = (BlockScope)compoundStatement.EmitScope;
        scope.MergeScope(currentScope);
        foreach (var statement in compoundStatement.Statements)
        {
            foreach (var nestedStatement in LinearizeBlockItem(statement, scope))
            {
                yield return nestedStatement;
            }
        }
    }
    private static IEnumerable<IBlockItem> LinearizeBlockItem(IBlockItem statement, FunctionScope scope)
    {
        if (statement is CompoundStatement nestedCompound)
        {
            foreach (var nestedStatement in Linearize(nestedCompound, scope))
            {
                yield return nestedStatement;
            }
        }
        else if (statement is LabelStatement labelStatement)
        {
            if (labelStatement.Expression is CompoundStatement nestedLabeledCompound)
            {
                bool first = true;
                foreach (var nestedStatement in Linearize(nestedLabeledCompound, scope))
                {
                    if (first)
                    {
                        first = false;
                        yield return new LabelStatement(labelStatement.Identifier, nestedStatement, true);
                    }
                    else
                    {
                        yield return nestedStatement;
                    }
                }
            }
            else
            {
                yield return statement;
            }
        }
        else if (statement is IfElseStatement ifElseStatement)
        {
            var elsePart = Simplify(ifElseStatement.FalseBranch);
            var truePart = Simplify(ifElseStatement.TrueBranch);

            yield return new IfElseStatement(ifElseStatement.Expression, truePart, elsePart) { IsEscapeBranchRequired = ifElseStatement.IsEscapeBranchRequired };

            [return: NotNullIfNotNull(nameof(blockItem))]
            IBlockItem? Simplify(IBlockItem? blockItem)
            {
                if (blockItem is null) return null;

                if (blockItem is CompoundStatement falseBranch)
                {
                    return new CompoundStatement(Linearize(falseBranch, scope).ToList(), null);
                }

                var result = LinearizeBlockItem(blockItem, scope).ToList();
                if (result.Count == 1) return result[0];
                return new CompoundStatement(result, null);
            }
        }
        else
        {
            yield return statement;
        }
    }

    private static IBlockItem Lower(IDeclarationScope scope, IBlockItem blockItem)
    {
        switch (blockItem)
        {
            case AmbiguousBlockItem a:
                {
                    // Check if this can be a valid variable declaration:
                    var isValidVariableDeclaration = scope.GetVariable(a.Item1) != null;

                    // Check if this can be a function call:
                    var function = scope.GetFunctionInfo(a.Item1);
                    var isValidFunctionCall = function != null;

                    if (!isValidVariableDeclaration && !isValidFunctionCall)
                        throw new CompilationException(
                            $"{a.Item1}({a.Item2}) is supposed to be either a variable declaration or a function call," +
                            " but wasn't resolved to be either.");

                    if (isValidVariableDeclaration && isValidFunctionCall)
                        throw new CompilationException(
                            $"{a.Item1}({a.Item2}) is supposed to be either a variable declaration or a function call," +
                            $" but it's ambiguous which it is, since both a function and a type of name {a.Item1} exist.");

                    if (!isValidFunctionCall) return a;

                    var functionCallExpression = new FunctionCallExpression(
                        new IdentifierExpression(a.Item1),
                        null,
                        ImmutableArray.Create<IExpression>(new IdentifierExpression(a.Item2))
                    );

                    return Lower(scope, new ExpressionStatement(functionCallExpression));
                }
            case BreakStatement:
                {
                    var breakLabel = scope.GetBreakLabel() ?? throw new CompilationException("Can't break not from for statement");
                    return new GoToStatement(breakLabel);
                }
            case CaseStatement c:
                {
                    if (scope is not BlockScope sws || sws.SwitchCases == null)
                        throw new AssertException("Cannot use case statement outside of switch");

                    void ProcessCaseStatement(CaseStatement caseStatement, BlockScope switchBlock, string label)
                    {
                        if (switchBlock.SwitchCases is not null)
                        {
                            if (caseStatement.Expression != null)
                            {
                                var constValue = ConstantEvaluator.GetConstantValue(caseStatement.Expression.Lower(switchBlock));
                                switchBlock.SwitchCases.Add(new SwitchCase(new ConstantLiteralExpression(constValue), label));
                            }
                            else
                            {
                                switchBlock.SwitchCases.Add(new SwitchCase(null, label));
                            }
                        }
                    }

                    string label = c.Label;
                    IBlockItem currentStatement = c;

                    while (currentStatement is CaseStatement nestedCaseStatement)
                    {
                        ProcessCaseStatement(nestedCaseStatement, sws, label);
                        currentStatement = nestedCaseStatement.Statement;
                    }

                    return Lower(scope, new LabelStatement(label, currentStatement));
                }
            case CompoundStatement c:
                {
                    var blockScope = new BlockScope((IEmitScope)scope, null, null);

                    var newNestedStatements = new List<IBlockItem>();
                    foreach (var stmt in c.Statements)
                    {
                        newNestedStatements.Add(Lower(blockScope, stmt));
                    }

                    return new CompoundStatement(newNestedStatements, blockScope);
                }
            case ContinueStatement:
                {
                    var continueLabel = scope.GetContinueLabel() ?? throw new CompilationException("Can't use continue outside of a loop construct.");
                    return new GoToStatement(continueLabel);
                }
            case DeclarationBlockItem d:
                {
                    var (storageClass, items) = d.Declaration;
                    var newItems = new List<InitializerPart>();

                    foreach (var (declaration, initializer) in items)
                    {
                        var (type, identifier, cliImportMemberName) = declaration;

                        // TODO[#91]: A place to register whether {type} is const or not.

                        if (identifier == null)
                            throw new CompilationException("An anonymous local declaration isn't supported.");

                        if (cliImportMemberName != null)
                            throw new CompilationException(
                                $"Local declaration with a CLI import member name {cliImportMemberName} isn't supported.");

                        type = scope.ResolveType(type);
                        scope.AddVariable(storageClass, identifier, type, null);

                        var initializerExpression = initializer;
                        if (initializerExpression != null)
                        {
                            var initializerType = initializerExpression.Lower(scope).GetExpressionType(scope);
                            if (CTypeSystem.IsConversionAvailable(initializerType, type)
                                && CTypeSystem.IsConversionRequired(initializerType, type))
                            {
                                initializerExpression = new TypeCastExpression(type, initializerExpression);
                            }
                        }

                        if (initializerExpression != null)
                        {
                            initializerExpression = new AssignmentExpression(new IdentifierExpression(identifier),
                                AssignmentOperator.Assign, initializerExpression);

                            if (initializerExpression.GetExpressionType(scope) is not PrimitiveType
                                {
                                    Kind: PrimitiveTypeKind.Void
                                })
                                initializerExpression = new ConsumeExpression(initializerExpression);
                        }

                        IExpression? primaryInitializerExpression = null;
                        if (type is InPlaceArrayType i)
                        {
                            primaryInitializerExpression = new ConsumeExpression(
                                new AssignmentExpression(new IdentifierExpression(identifier), AssignmentOperator.Assign,
                                    new LocalAllocationExpression(i))
                            );
                        }

                        var initializableDeclaration = new InitializerPart(primaryInitializerExpression?.Lower(scope),
                            initializerExpression?.Lower(scope));
                        newItems.Add(initializableDeclaration);
                    }

                    return new InitializationBlockItem(newItems);
                }
            case DoWhileStatement s:
                {
                    var breakLabel = Guid.NewGuid().ToString();
                    var continueLabel = Guid.NewGuid().ToString();

                    var loopScope = new BlockScope((IEmitScope)scope, breakLabel, continueLabel);

                    return MakeLoop(
                        loopScope,
                        new GoToStatement(continueLabel),
                        s.TestExpression,
                        null,
                        s.Body,
                        breakLabel,
                        null,
                        continueLabel,
                        null
                    );
                }
            case ExpressionStatement s:
                {
                    var loweredExpression = s.Expression?.Lower(scope);

                    if (loweredExpression is SetValueExpression setValue)
                        return new ExpressionStatement(setValue.NoReturn());

                    if (loweredExpression is not null
                        && !loweredExpression.GetExpressionType(scope).IsEqualTo(CTypeSystem.Void))
                    {
                        loweredExpression = new ConsumeExpression(loweredExpression);
                    }

                    return new ExpressionStatement(loweredExpression);
                }
            case ForStatement s:
                {
                    var breakLabel = Guid.NewGuid().ToString();
                    var continueLabel = Guid.NewGuid().ToString();

                    var loopScope = new BlockScope((IEmitScope)scope, breakLabel, continueLabel);

                    return MakeLoop(
                        loopScope,
                        s.InitDeclaration ?? new ExpressionStatement(s.InitExpression),
                        s.TestExpression,
                        s.UpdateExpression,
                        s.Body,
                        breakLabel,
                        null,
                        null,
                        continueLabel
                    );
                }
            case FunctionDeclaration d:
                {
                    var resolvedFunctionType = (FunctionType)scope.ResolveType(d.FunctionType);
                    var (parametersInfo, returnType) = resolvedFunctionType;
                    if (d.CliImportMemberName != null)
                    {
                        if (parametersInfo is null or { Parameters.Count: 0, IsVoid: false })
                            throw new CompilationException($"Empty parameter list is not allowed for CLI-imported function {d.Identifier}.");
                    }

                    var pinvoke = scope.GetPragma<PInvokeDefinition>();
                    string? dllLibrary = pinvoke != null ? pinvoke.LibName : null;

                    var cliImportFunctionInfo = new FunctionInfo(parametersInfo, returnType, d.StorageClass, IsDefined: d.CliImportMemberName is not null)
                    {
                        CliImportMember = d.CliImportMemberName,
                        DllLibraryName = dllLibrary,
                        DllImportNameStrip = pinvoke?.Prefix
                    };
                    scope.DeclareFunction(d.Identifier, cliImportFunctionInfo);

                    return new FunctionDeclaration(d.Identifier, d.StorageClass, resolvedFunctionType, d.CliImportMemberName, dllLibrary);
                }

            case FunctionDefinition d:
                {
                    var resolvedFunctionType = (FunctionType)scope.ResolveType(d.FunctionType);
                    var (parameters, returnType) = resolvedFunctionType;
                    if (d.IsMain && !returnType.IsEqualTo(CTypeSystem.Int))
                        throw new CompilationException(
                            $"Invalid return type for the {d.Name} function: " +
                            $"int expected, got {returnType}.");

                    if (d.IsMain && parameters?.IsVarArg == true)
                        throw new WipException(196, $"Variable arguments for the {d.Name} function aren't supported.");

                    var declaration = scope.GetFunctionInfo(d.Name);
                    if (declaration?.IsDefined == true)
                        if (declaration.CliImportMember is null)
                            throw new CompilationException($"Double definition of function {d.Name}.");
                        else
                            throw new CompilationException($"Function {d.Name} already defined as immutable.");

                    var newDeclaration = new FunctionInfo(parameters, returnType, d.StorageClass, IsDefined: true);
                    scope.DeclareFunction(d.Name, newDeclaration);

                    return new FunctionDefinition(d.Name, d.StorageClass, resolvedFunctionType, d.Statement);
                }
            case GlobalVariableDefinition d:
                {
                    var resolvedType = scope.ResolveType(d.Type);
                    scope.AddVariable(d.StorageClass, d.Identifier, resolvedType, null);

                    return d with { Initializer = d.Initializer?.Lower(scope) };
                }
            case EnumConstantDefinition d:
                {
                    var resolvedType = scope.ResolveType(d.Type);
                    scope.AddVariable(StorageClass.Static, d.Identifier, resolvedType, d.Value);

                    return d;
                }
            case GoToStatement:
                {
                    // already lowered
                    return blockItem;
                }
            case IfElseStatement s:
                {
                    var falseBranch = s.FalseBranch != null ? Lower(scope, s.FalseBranch) : null;

                    return new IfElseStatement(s.Expression.Lower(scope), Lower(scope, s.TrueBranch), falseBranch);
                }
            case InitializationBlockItem:
                {
                    // already lowered
                    return blockItem;
                }
            case LabelStatement s:
                {
                    // TODO[#201]: Remove side effects from Lower, migrate labels to a separate compilation stage.
                    if (!s.DidLowered)
                        scope.AddLabel(s.Identifier);
                    return new LabelStatement(s.Identifier, Lower(scope, s.Expression), true);
                }
            case WhileStatement s:
                {
                    var breakLabel = Guid.NewGuid().ToString();
                    var continueLabel = Guid.NewGuid().ToString();

                    var loopScope = new BlockScope((IEmitScope)scope, breakLabel, continueLabel);

                    return MakeLoop(
                        loopScope,
                        null,
                        s.TestExpression,
                        null,
                        s.Body,
                        breakLabel,
                        continueLabel,
                        null,
                        null
                    );
                }
            case ReturnStatement s:
                {
                    return new ReturnStatement(s.Expression?.Lower(scope));
                }
            case SwitchStatement s:
                {
                    var switchCases = new List<SwitchCase>();
                    var breakLabel = Guid.NewGuid().ToString();
                    var switchScope = new BlockScope((IEmitScope)scope, breakLabel, null, switchCases);

                    var loweredBody = Lower(switchScope, s.Body);
                    var targetStmts = new List<IBlockItem>();

                    if (switchCases.Count == 0)
                    {
                        return Lower(switchScope, new ExpressionStatement(s.Expression));
                    }

                    var testExpression = s.Expression.Lower(scope);
                    var dbi = new DeclarationBlockItem(
                        new ScopedIdentifierDeclaration(
                            StorageClass.Auto,
                            new List<InitializableDeclarationInfo>
                            {
                                new(new LocalDeclarationInfo(testExpression.GetExpressionType(scope), "$switch_tmp", null),
                                testExpression)
                            }));

                    targetStmts.Add(Lower(switchScope, dbi));

                    var idExpr = new IdentifierExpression("$switch_tmp");

                    var hasDefaultCase = false;

                    foreach (var matchGroup in switchCases)
                    {
                        if (matchGroup.TestExpression != null)
                        {
                            targetStmts.Add(
                                new IfElseStatement(
                                    new BinaryOperatorExpression(idExpr, BinaryOperator.EqualTo, matchGroup.TestExpression).Lower(switchScope),
                                    new GoToStatement(matchGroup.Label),
                                    null
                                )
                            );
                        }
                        else
                        {
                            hasDefaultCase = true;
                            targetStmts.Add(new GoToStatement(matchGroup.Label));
                        }
                    }

                    if (!hasDefaultCase)
                        targetStmts.Add(new GoToStatement(breakLabel));

                    targetStmts.Add(loweredBody);
                    targetStmts.Add(Lower(switchScope, new LabelStatement(breakLabel, new ExpressionStatement((IExpression?)null))));

                    // avoiding lowering twice
                    return new CompoundStatement(targetStmts, switchScope);
                }
            case TagBlockItem t:
                {
                    List<LocalDeclarationInfo> list = new();
                    foreach (var typeDef in t.Types)
                    {
                        var (type, identifier, cliImportMemberName) = typeDef;
                        if (identifier == null)
                            throw new CompilationException($"Anonymous typedef not supported: {type}.");

                        if (cliImportMemberName != null)
                            throw new CompilationException($"typedef for CLI import not supported: {cliImportMemberName}.");

                        type = scope.ResolveType(type);
                        scope.AddTagDefinition(identifier, type);
                        list.Add(new LocalDeclarationInfo(type, identifier, cliImportMemberName));
                    }

                    return new TagBlockItem(list);
                }
            case TypeDefBlockItem t:
                {
                    List<LocalDeclarationInfo> list = new();
                    foreach (var typeDef in t.Types)
                    {
                        var (type, identifier, cliImportMemberName) = typeDef;
                        if (identifier == null)
                            throw new CompilationException($"Anonymous typedef not supported: {type}.");

                        if (cliImportMemberName != null)
                            throw new CompilationException($"typedef for CLI import not supported: {cliImportMemberName}.");

                        type = scope.ResolveType(type);
                        scope.AddTypeDefinition(identifier, type);

                        if (typeDef.Type is StructType { Identifier: { } tag })
                        {
                            scope.AddTagDefinition(tag, type);
                        }

                        if (typeDef.Type is EnumType { Identifier: { } enumTag })
                        {
                            scope.AddTagDefinition(enumTag, type);
                        }

                        list.Add(new LocalDeclarationInfo(type, identifier, cliImportMemberName));
                    }

                    return new TypeDefBlockItem(list);
                }
            case PInvokeDefinition pinv:
                if (!pinv.IsEnd)
                    scope.PushPragma(pinv);
                else
                    scope.RemovePragma<PInvokeDefinition>(_ => true);
                return pinv;
            default:
                throw new ArgumentOutOfRangeException(nameof(blockItem));
        }
    }
}
