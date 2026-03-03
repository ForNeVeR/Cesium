// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.Ast;
using Cesium.Core;

namespace Cesium.Compiler;

internal sealed class AstDumper : AstVisitor
{
    private readonly IndentedTextWriter _writer;

    public AstDumper(TextWriter writer)
    {
        _writer = new IndentedTextWriter(writer);
    }

    public override void Visit(TranslationUnit translationUnit)
    {
        Enter("TranslationUnitDecl");
        base.Visit(translationUnit);
        Exit();
    }

    protected override void Visit(FunctionDefinition functionDefinition)
    {
        Enter("FunctionDefinition");
        base.Visit(functionDefinition);
        Exit();
    }

    private void Exit()
    {
        _writer.Indent--;
    }

    private void Enter(string nodeName)
    {
        _writer.WriteLine(nodeName);
        _writer.Indent++;
    }

    protected override void Visit(SymbolDeclaration symbolDeclaration)
    {
        Enter("SymbolDecl");
        base.Visit(symbolDeclaration);
        Exit();
    }

    protected override void Visit(PInvokeDeclaration pInvokeDeclaration)
    {
        var prefixPart = pInvokeDeclaration.Prefix is null ? string.Empty : $" Prefix = {pInvokeDeclaration.Prefix}";
        _writer.WriteLine($"PInvokeDecl {pInvokeDeclaration.Declaration}{prefixPart}");
        base.Visit(pInvokeDeclaration);
    }

    protected override void Visit(Declaration declaration)
    {
        Enter("Decl");
        base.Visit(declaration);
        Exit();
    }

    protected override void Visit(InitDeclarator initDeclarator)
    {
        Enter("InitDecl");
        base.Visit(initDeclarator);
        Exit();
    }

    protected override void Visit(Declarator declarator)
    {
        Enter("Declarator");
        base.Visit(declarator);
        Exit();
    }

    protected override void Visit(IDirectDeclarator directDeclarator)
    {
        base.Visit(directDeclarator);
    }

    protected override void Visit(IdentifierDirectDeclarator identifierDirectDeclarator)
    {
        _writer.WriteLine($"IdentifierDirectDeclarator {identifierDirectDeclarator.Identifier}");
        base.Visit(identifierDirectDeclarator);
    }
    protected override void Visit(ArrayDirectDeclarator arrayDirectDeclarator)
    {
        Enter("ArrayDirectDeclarator");
        base.Visit(arrayDirectDeclarator);
        Exit();
    }

    protected override void Visit(ParameterListDirectDeclarator parameterListDirectDeclarator)
    {
        Enter("ParameterListDirectDeclarator");
        base.Visit(parameterListDirectDeclarator);

        if (parameterListDirectDeclarator.Parameters.HasEllipsis)
        {
            _writer.WriteLine("HasEllipsis");
        }
        Exit();
    }

    protected override void Visit(IdentifierListDirectDeclarator identifierListDirectDeclarator)
    {
        Enter("IdentifierListDirectDeclarator");
        base.Visit(identifierListDirectDeclarator.Base);
        if (identifierListDirectDeclarator.Identifiers is not null)
        {
            _writer.WriteLine($"Identifiers {string.Join(", ", identifierListDirectDeclarator.Identifiers)}");
        }
        Exit();
    }

    protected override void Visit(DeclaratorDirectDeclarator declaratorDirectDeclarator)
    {
        Enter("DeclaratorDirectDeclarator");
        base.Visit(declaratorDirectDeclarator);
        Exit();
    }

    protected override void Visit(ParameterTypeList parameterTypeList)
    {
        foreach (var parameterDeclaration in parameterTypeList.Parameters)
        {
            Visit(parameterDeclaration);
        }
    }

    protected override void Visit(ParameterDeclaration parameterDeclaration)
    {
        Enter("ParameterDeclaration");
        base.Visit(parameterDeclaration);
        Exit();
    }

    protected override void Visit(AbstractDeclarator abstractDeclarator)
    {
        Enter("AbstractDeclarator");
        base.Visit(abstractDeclarator);
        Exit();
    }

    protected override void Visit(IDirectAbstractDeclarator directAbstractDeclarator)
    {
        base.Visit(directAbstractDeclarator);
    }

    protected override void Visit(SimpleDirectAbstractDeclarator simpleDirectAbstractDeclarator)
    {
        Enter("SimpleDirectAbstractDeclarator");
        base.Visit(simpleDirectAbstractDeclarator);
        Exit();
    }

    protected override void Visit(ArrayDirectAbstractDeclarator arrayDirectAbstractDeclarator)
    {
        Enter("ArrayDirectDeclarator");
        base.Visit(arrayDirectAbstractDeclarator);
        Exit();
    }

    protected override void Visit(Pointer pointer)
    {
        Enter("Pointer");
        base.Visit(pointer);
        Exit();
    }

    protected override void Visit(Initializer initializer)
    {
        base.Visit(initializer);
    }

    protected override void Visit(AssignmentInitializer assignmentInitializer)
    {
        Enter("AssignmentInitializer");
        base.Visit(assignmentInitializer);
        Exit();
    }

    protected override void Visit(ArrayInitializer arrayInitializer)
    {
        Enter("ArrayInitializer");
        base.Visit(arrayInitializer);
        Exit();
    }

    protected override void Visit(Designation designation)
    {
        Enter("Designation");
        base.Visit(designation);
        Exit();
    }

    protected override void Visit(Designator designator)
    {
        base.Visit(designator);
    }

    protected override void Visit(BracketsDesignator bracketsDesignator)
    {
        Enter("BracketsDesignator");
        base.Visit(bracketsDesignator);
        Exit();
    }

    protected override void Visit(IdentifierDesignator identifierDesignator)
    {
        _writer.WriteLine($"IdentifierDesignator .{identifierDesignator.FieldName}");
        base.Visit(identifierDesignator);
    }

    protected override void Visit(IDeclarationSpecifier specifier)
    {
        base.Visit(specifier);
    }

    protected override void Visit(StorageClassSpecifier storageClassSpecifier)
    {
        _writer.WriteLine($"StorageClassSpecifier {storageClassSpecifier.Name}");
        base.Visit(storageClassSpecifier);
    }

    protected override void Visit(CliImportSpecifier cliImportSpecifier)
    {
        Enter($"CliImportSpecifier {cliImportSpecifier.MemberName}");
        base.Visit(cliImportSpecifier);
        Exit();
    }

    protected override void Visit(ISpecifierQualifierListItem specifierQualifierListItem)
    {
        base.Visit(specifierQualifierListItem);
    }

    protected override void Visit(ITypeSpecifier typeSpecifier)
    {
        base.Visit(typeSpecifier);
    }

    protected override void Visit(SimpleTypeSpecifier simpleTypeSpecifier)
    {
        _writer.WriteLine($"SimpleTypeSpecifier {simpleTypeSpecifier.TypeName}");
        base.Visit(simpleTypeSpecifier);
    }

    protected override void Visit(NamedTypeSpecifier namedTypeSpecifier)
    {
        _writer.WriteLine($"NamedTypeSpecifier {namedTypeSpecifier.TypeDefName}");
        base.Visit(namedTypeSpecifier);
    }

    protected override void Visit(TypeQualifier typeQualifier)
    {
        _writer.WriteLine($"TypeQualifier {typeQualifier.Name}");
    }

    protected override void Visit(StructOrUnionSpecifier structOrUnionSpecifier)
    {
        _writer.WriteLine($"StructOrUnionSpecifier {structOrUnionSpecifier.TypeKind} {structOrUnionSpecifier.Identifier}");
        base.Visit(structOrUnionSpecifier);
    }

    protected override void Visit(StructDeclaration structDeclaration)
    {
        Enter("StructDeclaration");
        Enter("SpecifiersQualifiers");
        base.Visit(structDeclaration);
        Exit();
        Exit();
    }

    protected override void Visit(StructDeclarator structDeclarator)
    {
        Enter("StructDeclarator");
        base.Visit(structDeclarator);
        Exit();
    }

    protected override void Visit(EnumSpecifier enumSpecifier)
    {
        _writer.WriteLine($"EnumSpecifier {enumSpecifier.Identifier}");
        base.Visit(enumSpecifier);
    }

    protected override void Visit(EnumDeclaration enumDeclaration)
    {
        _writer.WriteLine($"EnumDeclaration {enumDeclaration.Identifier}");
        base.Visit(enumDeclaration);
        if (enumDeclaration.Constant is not null)
        {
            _writer.Indent++;
            base.Visit(enumDeclaration.Constant);
            _writer.Indent--;
        }
    }

    protected override void Visit(Expression expression)
    {
        switch (expression)
        {
            case StringLiteralListExpression stringLiteralListExpression:
                _writer.WriteLine($"StringLiteralListExpression {string.Join(", ", stringLiteralListExpression.ConstantList.Select(_ => _.Text))}");
                break;
            case IdentifierExpression identifierExpression:
                _writer.WriteLine($"IdentifierExpression {identifierExpression.Identifier}");
                break;
            case ConstantLiteralExpression constantLiteralExpression:
                _writer.WriteLine($"ConstantLiteralExpression {constantLiteralExpression.Constant.Text}");
                break;
            case ParenExpression parenExpression:
                _writer.WriteLine("ParenExpression");
                _writer.Indent++;
                Visit(parenExpression.Contents);
                _writer.Indent--;
                break;
            case SubscriptingExpression subscriptingExpression:
                _writer.WriteLine("ParenExpression");
                _writer.Indent++;
                _writer.WriteLine("Base");
                _writer.Indent++;
                Visit(subscriptingExpression.Base);
                _writer.Indent--;
                _writer.WriteLine("Index");
                _writer.Indent++;
                Visit(subscriptingExpression.Index);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case FunctionCallExpression functionCallExpression:
                _writer.WriteLine("FunctionCallExpression");
                _writer.Indent++;
                _writer.WriteLine("Function");
                _writer.Indent++;
                Visit(functionCallExpression.Function);
                _writer.Indent--;
                _writer.WriteLine("Arguments");
                if (functionCallExpression.Arguments is not null)
                {
                    _writer.Indent++;
                    foreach (var argument in functionCallExpression.Arguments)
                    {
                        Visit(argument);
                    }

                    _writer.Indent--;
                }
                _writer.Indent--;
                break;
            case TypeCastOrNamedFunctionCallExpression typeCastOrNamedFunctionCallExpression:
                _writer.WriteLine($"TypeCastOrNamedFunctionCallExpression {typeCastOrNamedFunctionCallExpression.TypeOrFunctionName}");
                _writer.Indent++;
                _writer.WriteLine("Arguments");
                _writer.Indent++;
                foreach (var argument in typeCastOrNamedFunctionCallExpression.Arguments)
                {
                    Visit(argument);
                }

                _writer.Indent--;
                _writer.Indent--;
                break;
            case MemberAccessExpression memberAccessExpression:
                _writer.WriteLine($"MemberAccessExpression {memberAccessExpression.Identifier.Identifier}");
                _writer.Indent++;
                _writer.WriteLine("Target");
                _writer.Indent++;
                Visit(memberAccessExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case PointerMemberAccessExpression pointerMemberAccessExpression:
                _writer.WriteLine($"PointerMemberAccessExpression {pointerMemberAccessExpression.Identifier.Identifier}");
                _writer.Indent++;
                _writer.WriteLine("Target");
                _writer.Indent++;
                Visit(pointerMemberAccessExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case PostfixIncrementDecrementExpression postfixIncrementDecrementExpression:
                _writer.WriteLine($"PostfixIncrementDecrementExpression {postfixIncrementDecrementExpression.PostfixOperator.Text}");
                _writer.Indent++;
                _writer.WriteLine("Target");
                _writer.Indent++;
                Visit(postfixIncrementDecrementExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case PrefixIncrementDecrementExpression prefixIncrementDecrementExpression:
                _writer.WriteLine($"PrefixIncrementDecrementExpression {prefixIncrementDecrementExpression.PrefixOperator.Text}");
                _writer.Indent++;
                _writer.WriteLine("Target");
                _writer.Indent++;
                Visit(prefixIncrementDecrementExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case UnaryOperatorExpression unaryOperatorExpression:
                _writer.WriteLine($"UnaryOperatorExpression {unaryOperatorExpression.Operator}");
                _writer.Indent++;
                _writer.WriteLine("Target");
                _writer.Indent++;
                Visit(unaryOperatorExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case IndirectionExpression indirectionExpression:
                _writer.WriteLine("IndirectionExpression");
                _writer.Indent++;
                _writer.WriteLine("Target");
                _writer.Indent++;
                Visit(indirectionExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case UnaryExpressionSizeOfOperatorExpression unaryExpressionSizeOfOperatorExpression:
                _writer.WriteLine("UnaryExpressionSizeOfOperatorExpression");
                _writer.Indent++;
                _writer.WriteLine("Target");
                _writer.Indent++;
                Visit(unaryExpressionSizeOfOperatorExpression.TargetExpession);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case TypeNameSizeOfOperatorExpression typeNameSizeOfOperatorExpression:
                _writer.WriteLine("TypeNameSizeOfOperatorExpression");
                _writer.Indent++;
                VisitTypeName(typeNameSizeOfOperatorExpression.TypeName);
                _writer.Indent--;
                break;
            case CastExpression castExpression:
                _writer.WriteLine("UnaryExpressionSizeOfOperatorExpression");
                _writer.Indent++;
                _writer.WriteLine("TypeName");
                _writer.Indent++;
                VisitTypeName(castExpression.TypeName);
                _writer.Indent--;
                _writer.WriteLine("Target");
                _writer.Indent++;
                Visit(castExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case BinaryOperatorExpression binaryOperatorExpression:
                _writer.WriteLine($"BinaryOperatorExpression {binaryOperatorExpression.Operator}");
                _writer.Indent++;
                _writer.WriteLine("Left");
                _writer.Indent++;
                Visit(binaryOperatorExpression.Left);
                _writer.Indent--;
                _writer.WriteLine("Right");
                _writer.Indent++;
                Visit(binaryOperatorExpression.Right);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case ConditionalExpression conditionalExpression:
                _writer.WriteLine("ConditionalExpression");
                _writer.Indent++;
                _writer.WriteLine("Condition");
                _writer.Indent++;
                Visit(conditionalExpression.Condition);
                _writer.Indent--;
                _writer.WriteLine("TrueExpression");
                _writer.Indent++;
                Visit(conditionalExpression.TrueExpression);
                _writer.Indent--;
                _writer.WriteLine("FalseExpression");
                _writer.Indent++;
                Visit(conditionalExpression.FalseExpression);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case CommaExpression commaExpression:
                _writer.WriteLine("CommaExpression");
                _writer.Indent++;
                _writer.WriteLine("Left");
                _writer.Indent++;
                Visit(commaExpression.Left);
                _writer.Indent--;
                _writer.WriteLine("Right");
                _writer.Indent++;
                Visit(commaExpression.Right);
                _writer.Indent--;
                _writer.Indent--;
                break;
            default:
                throw new AssertException($"Unknown expression of type {expression.GetType()}.");
        }
    }

    protected override void VisitTypeName(TypeName typeName)
    {
        _writer.WriteLine("TypeName");
        _writer.Indent++;
        _writer.WriteLine("SpecifierQualifierList");
        _writer.Indent++;
        foreach (var specifierQualifierListItem in typeName.SpecifierQualifierList)
        {
            Visit(specifierQualifierListItem);
        }

        _writer.Indent--;
        _writer.WriteLine("AbstractDeclarator");
        if (typeName.AbstractDeclarator is not null)
        {
            _writer.Indent++;
            Visit(typeName.AbstractDeclarator);
            _writer.Indent--;
        }

        _writer.Indent--;
    }

    protected override void Visit(Statement statement)
    {
        switch (statement)
        {
            case LabelStatement labelStatement:
                _writer.WriteLine($"LabelStatement {labelStatement.Identifier}");
                _writer.Indent++;
                Visit(labelStatement.Body);
                _writer.Indent--;
                break;
            case CaseStatement caseStatement:
                _writer.WriteLine("CaseStatement");
                _writer.Indent++;
                if (caseStatement.Constant is not null)
                {
                    _writer.WriteLine("Constant");
                    _writer.Indent++;
                    Visit(caseStatement.Constant);
                    _writer.Indent--;
                }

                _writer.WriteLine("Body");
                _writer.Indent++;
                Visit(caseStatement.Body);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case CompoundStatement compoundStatement:
                _writer.WriteLine("CompoundStatement");
                foreach (var subStatement in compoundStatement.Block)
                {
                    VisitBlockItem(subStatement);
                }
                break;
            case ExpressionStatement expressionStatement:
                _writer.WriteLine("ExpressionStatement");
                _writer.Indent++;
                if (expressionStatement.Expression is not null)
                {
                    _writer.WriteLine("Expression");
                    _writer.Indent++;
                    Visit(expressionStatement.Expression);
                    _writer.Indent--;
                }

                _writer.Indent--;
                break;
            case IfElseStatement ifElseStatement:
                _writer.WriteLine("IfElseStatement");
                _writer.Indent++;
                _writer.WriteLine("Expression");
                _writer.Indent++;
                Visit(ifElseStatement.Expression);
                _writer.Indent--;
                _writer.WriteLine("TrueBranch");
                _writer.Indent++;
                Visit(ifElseStatement.TrueBranch);
                _writer.Indent--;
                if (ifElseStatement.FalseBranch is not null)
                {
                    _writer.WriteLine("FalseBranch");
                    _writer.Indent++;
                    Visit(ifElseStatement.FalseBranch);
                    _writer.Indent--;
                }

                _writer.Indent--;
                break;
            case SwitchStatement switchStatement:
                _writer.WriteLine("SwitchStatement");
                _writer.Indent++;
                _writer.WriteLine("Expression");
                _writer.Indent++;
                Visit(switchStatement.Expression);
                _writer.Indent--;
                _writer.WriteLine("Body");
                _writer.Indent++;
                Visit(switchStatement.Body);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case WhileStatement whileStatement:
                _writer.WriteLine("WhileStatement");
                _writer.Indent++;
                _writer.WriteLine("TestExpression");
                _writer.Indent++;
                Visit(whileStatement.TestExpression);
                _writer.Indent--;
                _writer.WriteLine("Body");
                _writer.Indent++;
                VisitBlockItem(whileStatement.Body);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case DoWhileStatement doWhileStatement:
                _writer.WriteLine("DoWhileStatement");
                _writer.Indent++;
                _writer.WriteLine("TestExpression");
                _writer.Indent++;
                Visit(doWhileStatement.TestExpression);
                _writer.Indent--;
                _writer.WriteLine("Body");
                _writer.Indent++;
                VisitBlockItem(doWhileStatement.Body);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case ForStatement forStatement:
                _writer.WriteLine("ForStatement");
                _writer.Indent++;

                if (forStatement.InitDeclaration is not null)
                {
                    _writer.WriteLine("InitDeclaration");
                    _writer.Indent++;
                    VisitBlockItem(forStatement.InitDeclaration);
                    _writer.Indent--;
                }

                if (forStatement.InitExpression is not null)
                {
                    _writer.WriteLine("InitExpression");
                    _writer.Indent++;
                    Visit(forStatement.InitExpression);
                    _writer.Indent--;
                }

                if (forStatement.TestExpression is not null)
                {
                    _writer.WriteLine("TestExpression");
                    _writer.Indent++;
                    Visit(forStatement.TestExpression);
                    _writer.Indent--;
                }

                if (forStatement.UpdateExpression is not null)
                {
                    _writer.WriteLine("UpdateExpression");
                    _writer.Indent++;
                    Visit(forStatement.UpdateExpression);
                    _writer.Indent--;
                }

                _writer.WriteLine("Body");
                _writer.Indent++;
                VisitBlockItem(forStatement.Body);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case GoToStatement goToStatement:
                _writer.WriteLine($"GoToStatement {goToStatement.Identifier}");
                break;
            case BreakStatement:
                _writer.WriteLine("BreakStatement");
                break;
            case ContinueStatement:
                _writer.WriteLine("ContinueStatement");
                break;
            case ReturnStatement returnStatement:
                _writer.WriteLine("ReturnStatement");
                _writer.Indent++;
                _writer.WriteLine("Expression");
                _writer.Indent++;
                Visit(returnStatement.Expression);
                _writer.Indent--;
                _writer.Indent--;
                break;
            default:
                throw new AssertException($"Unknown statement of type {statement.GetType()}.");
        }
    }

    protected override void VisitAmbiguousBlockItem(AmbiguousBlockItem ambiguousBlockItem)
    {
        _writer.WriteLine($"AmbiguousBlockItem ({ambiguousBlockItem.Item1}, {ambiguousBlockItem.Item2})");
    }
}
