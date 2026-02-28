// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.Ast;
using Cesium.Core;

namespace Cesium.Compiler;

internal abstract class AstVisitor
{
    public void VisitTranslationUnit(TranslationUnit translationUnit)
    {
        ArgumentNullException.ThrowIfNull(translationUnit);
        Visit(translationUnit);
    }

    protected virtual void Visit(TranslationUnit translationUnit)
    {
        foreach (var declaration in translationUnit.Declarations)
        {
            Visit(declaration);
        }
    }

    protected virtual void Visit(ExternalDeclaration declaration)
    {
        switch (declaration)
        {
            case FunctionDefinition functionDefinition:
                Visit(functionDefinition);
                break;
            case SymbolDeclaration symbolDeclaration:
                Visit(symbolDeclaration);
                break;
            case PInvokeDeclaration pInvokeDeclaration:
                Visit(pInvokeDeclaration);
                break;
            default:
                throw new AssertException($"Unknown external declaration of type {declaration.GetType()}.");
        }
    }

    protected virtual void Visit(FunctionDefinition functionDefinition)
    {
        foreach (var specifier in functionDefinition.Specifiers)
        {
            Visit(specifier);
        }

        Visit(functionDefinition.Declarator);

        VisitFunctionDeclarations(functionDefinition);

        Visit(functionDefinition.Statement);
    }

    protected virtual void VisitFunctionDeclarations(FunctionDefinition functionDefinition)
    {
        if (functionDefinition.Declarations is not null)
        {
            foreach (var declaration in functionDefinition.Declarations)
            {
                Visit(declaration);
            }
        }
    }

    protected virtual void Visit(IDeclarationSpecifier specifier)
    {
        switch (specifier)
        {
            case StorageClassSpecifier storageClassSpecifier:
                Visit(storageClassSpecifier);
                break;
            case CliImportSpecifier cliImportSpecifier:
                Visit(cliImportSpecifier);
                break;
            case FunctionSpecifier functionSpecifier:
                Visit(functionSpecifier);
                break;
            case ISpecifierQualifierListItem specifierQualifierListItem:
                Visit(specifierQualifierListItem);
                break;
            default:
                throw new AssertException($"Unknown declaration specifier of type {specifier.GetType()}.");
        }
    }

    protected virtual void Visit(SymbolDeclaration symbolDeclaration)
    {
        Visit(symbolDeclaration.Declaration);
    }

    protected virtual void Visit(PInvokeDeclaration pInvokeDeclaration)
    {
    }

    protected virtual void Visit(Declaration declaration)
    {
        foreach (var specifier in declaration.Specifiers)
        {
            Visit(specifier);
        }

        if (declaration.InitDeclarators is not null)
        {
            foreach (var initDeclarator in declaration.InitDeclarators)
            {
                VisitInitDeclarator(initDeclarator);
            }
        }
    }

    protected virtual void VisitInitDeclarator(InitDeclarator initDeclarator)
    {
        Visit(initDeclarator.Declarator);
        if (initDeclarator.Initializer is not null)
        {
            VisitInitializer(initDeclarator.Initializer);
        }
    }

    protected virtual void Visit(Declarator declarator)
    {
        if (declarator.Pointer is not null)
        {
            Visit(declarator.Pointer);
        }

        Visit(declarator.DirectDeclarator);
    }

    protected virtual void Visit(IDirectDeclarator directDeclarator)
    {
        switch (directDeclarator)
        {
            case IdentifierDirectDeclarator identifierDirectDeclarator:
                Visit(identifierDirectDeclarator);
                break;
            case ArrayDirectDeclarator arrayDirectDeclarator:
                Visit(arrayDirectDeclarator);
                break;
            case ParameterListDirectDeclarator parameterListDirectDeclarator:
                Visit(parameterListDirectDeclarator);
                break;
            case IdentifierListDirectDeclarator identifierListDirectDeclarator:
                Visit(identifierListDirectDeclarator);
                break;
            case DeclaratorDirectDeclarator declaratorDirectDeclarator:
                Visit(declaratorDirectDeclarator);
                break;
            default:
                throw new AssertException($"Unknown direct declarator of type {directDeclarator.GetType()}.");
        }
    }

    protected virtual void Visit(IdentifierDirectDeclarator identifierDirectDeclarator)
    {
    }

    protected virtual void Visit(ArrayDirectDeclarator arrayDirectDeclarator)
    {
        Visit(arrayDirectDeclarator.Base);

        if (arrayDirectDeclarator.TypeQualifiers is not null)
        {
            foreach (var typeQualifier in arrayDirectDeclarator.TypeQualifiers)
            {
                Visit(typeQualifier);
            }
        }

        if (arrayDirectDeclarator.Size is not null)
        {
            Visit(arrayDirectDeclarator.Size);
        }
    }

    protected virtual void Visit(ParameterListDirectDeclarator parameterListDirectDeclarator)
    {
        Visit(parameterListDirectDeclarator.Base);
        Visit(parameterListDirectDeclarator.Parameters);
    }

    protected virtual void Visit(IdentifierListDirectDeclarator identifierListDirectDeclarator)
    {
        Visit(identifierListDirectDeclarator.Base);
    }

    protected virtual void Visit(DeclaratorDirectDeclarator declaratorDirectDeclarator)
    {
        Visit(declaratorDirectDeclarator.Declarator);
    }

    protected virtual void Visit(Pointer pointer)
    {
        if (pointer.TypeQualifiers is not null)
        {
            foreach (var typeQualifier in pointer.TypeQualifiers)
            {
                Visit(typeQualifier);
            }
        }

        if (pointer.ChildPointer is not null)
        {
            Visit(pointer.ChildPointer);
        }
    }

    protected virtual void Visit(StorageClassSpecifier storageClassSpecifier)
    {
    }

    protected virtual void Visit(CliImportSpecifier cliImportSpecifier)
    {
    }

    protected virtual void Visit(FunctionSpecifier functionSpecifier)
    {
    }

    protected virtual void Visit(ISpecifierQualifierListItem specifierQualifierListItem)
    {
        switch (specifierQualifierListItem)
        {
            case TypeQualifier typeQualifier:
                Visit(typeQualifier);
                break;
            case ITypeSpecifier typeSpecifier:
                Visit(typeSpecifier);
                break;
            default:
                throw new AssertException($"Unknown specifier qualifier list item of type {specifierQualifierListItem.GetType()}.");
        }
    }

    protected virtual void Visit(TypeQualifier typeQualifier)
    {
    }

    protected virtual void Visit(ITypeSpecifier typeSpecifier)
    {
        switch (typeSpecifier)
        {
            case SimpleTypeSpecifier simpleTypeSpecifier:
                Visit(simpleTypeSpecifier);
                break;
            case StructOrUnionSpecifier structOrUnionSpecifier:
                Visit(structOrUnionSpecifier);
                break;
            case EnumSpecifier enumSpecifier:
                Visit(enumSpecifier);
                break;
            case NamedTypeSpecifier namedTypeSpecifier:
                Visit(namedTypeSpecifier);
                break;
            default:
                throw new AssertException($"Unknown type specifier of type {typeSpecifier.GetType()}.");
        }
    }

    protected virtual void Visit(SimpleTypeSpecifier simpleTypeSpecifier)
    {
    }

    protected virtual void Visit(NamedTypeSpecifier namedTypeSpecifier)
    {
    }

    protected virtual void Visit(StructOrUnionSpecifier structOrUnionSpecifier)
    {
        foreach (var structDeclaration in structOrUnionSpecifier.StructDeclarations)
        {
            Visit(structDeclaration);
        }
    }

    protected virtual void Visit(StructDeclaration structDeclaration)
    {
        foreach (var specifierQualifierListItem in structDeclaration.SpecifiersQualifiers)
        {
            Visit(specifierQualifierListItem);
        }

        if (structDeclaration.Declarators is not null)
        {
            foreach (var structDeclarator in structDeclaration.Declarators)
            {
                Visit(structDeclarator);
            }
        }
    }

    protected virtual void Visit(StructDeclarator structDeclarator)
    {
        Visit(structDeclarator.Declarator);
    }

    protected virtual void Visit(EnumSpecifier enumSpecifier)
    {
        if (enumSpecifier.EnumDeclarations is not null)
        {
            foreach (var enumDeclaration in enumSpecifier.EnumDeclarations)
            {
                Visit(enumDeclaration);
            }
        }
    }

    protected virtual void Visit(EnumDeclaration enumDeclaration)
    {
        if (enumDeclaration.Constant is not null)
        {
            Visit(enumDeclaration.Constant);
        }
    }

    protected virtual void VisitTypeName(TypeName typeName)
    {
        foreach (var specifierQualifierListItem in typeName.SpecifierQualifierList)
        {
            Visit(specifierQualifierListItem);
        }

        if (typeName.AbstractDeclarator is not null)
        {
            Visit(typeName.AbstractDeclarator);
        }
    }

    protected virtual void Visit(AbstractDeclarator abstractDeclarator)
    {
        if (abstractDeclarator.Pointer is not null)
        {
            Visit(abstractDeclarator.Pointer);
        }

        if (abstractDeclarator.DirectAbstractDeclarator is not null)
        {
            Visit(abstractDeclarator.DirectAbstractDeclarator);
        }
    }

    protected virtual void Visit(IDirectAbstractDeclarator directAbstractDeclarator)
    {
        switch (directAbstractDeclarator)
        {
            case SimpleDirectAbstractDeclarator simpleDirectAbstractDeclarator:
                Visit(simpleDirectAbstractDeclarator);
                break;
            case ArrayDirectAbstractDeclarator arrayDirectAbstractDeclarator:
                Visit(arrayDirectAbstractDeclarator);
                break;
            default:
                throw new AssertException($"Unknown direct abstract declarator of type {directAbstractDeclarator.GetType()}.");
        }
    }

    protected virtual void Visit(SimpleDirectAbstractDeclarator simpleDirectAbstractDeclarator)
    {
        Visit(simpleDirectAbstractDeclarator.Declarator);
    }

    protected virtual void Visit(ArrayDirectAbstractDeclarator arrayDirectAbstractDeclarator)
    {
        if (arrayDirectAbstractDeclarator.Base is not null)
        {
            Visit(arrayDirectAbstractDeclarator.Base);
        }

        if (arrayDirectAbstractDeclarator.TypeQualifiers is not null)
        {
            foreach (var typeQualifier in arrayDirectAbstractDeclarator.TypeQualifiers)
            {
                Visit(typeQualifier);
            }
        }

        if (arrayDirectAbstractDeclarator.Size is not null)
        {
            Visit(arrayDirectAbstractDeclarator.Size);
        }
    }

    protected virtual void Visit(ParameterTypeList parameterTypeList)
    {
        foreach (var parameterDeclaration in parameterTypeList.Parameters)
        {
            Visit(parameterDeclaration);
        }
    }

    protected virtual void Visit(ParameterDeclaration parameterDeclaration)
    {
        foreach (var specifier in parameterDeclaration.Specifiers)
        {
            Visit(specifier);
        }

        if (parameterDeclaration.Declarator is not null)
        {
            Visit(parameterDeclaration.Declarator);
        }

        if (parameterDeclaration.AbstractDeclarator is not null)
        {
            Visit(parameterDeclaration.AbstractDeclarator);
        }
    }

    protected virtual void VisitInitializer(Initializer initializer)
    {
        if (initializer.Designation is not null)
        {
            VisitDesignation(initializer.Designation);
        }

        switch (initializer)
        {
            case AssignmentInitializer assignmentInitializer:
                Visit(assignmentInitializer.Expression);
                break;
            case ArrayInitializer arrayInitializer:
                foreach (var arraySubInitializer in arrayInitializer.Initializers)
                {
                    VisitInitializer(arraySubInitializer);
                }
                break;
            default:
                throw new AssertException($"Unknown initializer of type {initializer.GetType()}.");
        }
    }

    protected virtual void VisitDesignation(Designation designation)
    {
        foreach (var designator in designation.Designators)
        {
            VisitDesignator(designator);
        }
    }

    protected virtual void VisitDesignator(Designator designator)
    {
        switch (designator)
        {
            case BracketsDesignator bracketsDesignator:
                Visit(bracketsDesignator.Expression);
                break;
            case IdentifierDesignator:
                break;
            default:
                throw new AssertException($"Unknown designator of type {designator.GetType()}.");
        }
    }

    protected virtual void Visit(Expression expression)
    {
        switch (expression)
        {
            case StringLiteralListExpression stringLiteralListExpression:
                VisitStringLiteralListExpression(stringLiteralListExpression);
                break;
            case IdentifierExpression identifierExpression:
                VisitIdentifierExpression(identifierExpression);
                break;
            case ConstantLiteralExpression constantLiteralExpression:
                VisitConstantLiteralExpression(constantLiteralExpression);
                break;
            case ParenExpression parenExpression:
                VisitParenExpression(parenExpression);
                break;
            case SubscriptingExpression subscriptingExpression:
                VisitSubscriptingExpression(subscriptingExpression);
                break;
            case FunctionCallExpression functionCallExpression:
                VisitFunctionCallExpression(functionCallExpression);
                break;
            case TypeCastOrNamedFunctionCallExpression typeCastOrNamedFunctionCallExpression:
                VisitTypeCastOrNamedFunctionCallExpression(typeCastOrNamedFunctionCallExpression);
                break;
            case MemberAccessExpression memberAccessExpression:
                VisitMemberAccessExpression(memberAccessExpression);
                break;
            case PointerMemberAccessExpression pointerMemberAccessExpression:
                VisitPointerMemberAccessExpression(pointerMemberAccessExpression);
                break;
            case PostfixIncrementDecrementExpression postfixIncrementDecrementExpression:
                VisitPostfixIncrementDecrementExpression(postfixIncrementDecrementExpression);
                break;
            case CompoundLiteralExpression compoundLiteralExpression:
                VisitCompoundLiteralExpression(compoundLiteralExpression);
                break;
            case PrefixIncrementDecrementExpression prefixIncrementDecrementExpression:
                VisitPrefixIncrementDecrementExpression(prefixIncrementDecrementExpression);
                break;
            case UnaryOperatorExpression unaryOperatorExpression:
                VisitUnaryOperatorExpression(unaryOperatorExpression);
                break;
            case IndirectionExpression indirectionExpression:
                VisitIndirectionExpression(indirectionExpression);
                break;
            case UnaryExpressionSizeOfOperatorExpression unaryExpressionSizeOfOperatorExpression:
                VisitUnaryExpressionSizeOfOperatorExpression(unaryExpressionSizeOfOperatorExpression);
                break;
            case TypeNameSizeOfOperatorExpression typeNameSizeOfOperatorExpression:
                VisitTypeNameSizeOfOperatorExpression(typeNameSizeOfOperatorExpression);
                break;
            case CastExpression castExpression:
                VisitCastExpression(castExpression);
                break;
            case BinaryOperatorExpression binaryOperatorExpression:
                VisitBinaryOperatorExpression(binaryOperatorExpression);
                break;
            case ConditionalExpression conditionalExpression:
                VisitConditionalExpression(conditionalExpression);
                break;
            case CommaExpression commaExpression:
                VisitCommaExpression(commaExpression);
                break;
            default:
                throw new AssertException($"Unknown expression of type {expression.GetType()}.");
        }
    }

    protected virtual void VisitStringLiteralListExpression(StringLiteralListExpression expression)
    {
    }

    protected virtual void VisitIdentifierExpression(IdentifierExpression expression)
    {
    }

    protected virtual void VisitConstantLiteralExpression(ConstantLiteralExpression expression)
    {
    }

    protected virtual void VisitParenExpression(ParenExpression expression)
    {
        Visit(expression.Contents);
    }

    protected virtual void VisitSubscriptingExpression(SubscriptingExpression expression)
    {
        Visit(expression.Base);
        Visit(expression.Index);
    }

    protected virtual void VisitFunctionCallExpression(FunctionCallExpression expression)
    {
        Visit(expression.Function);
        if (expression.Arguments is not null)
        {
            foreach (var argument in expression.Arguments)
            {
                Visit(argument);
            }
        }
    }

    protected virtual void VisitTypeCastOrNamedFunctionCallExpression(TypeCastOrNamedFunctionCallExpression expression)
    {
        foreach (var argument in expression.Arguments)
        {
            Visit(argument);
        }
    }

    protected virtual void VisitMemberAccessExpression(MemberAccessExpression expression)
    {
        Visit(expression.Target);
    }

    protected virtual void VisitPointerMemberAccessExpression(PointerMemberAccessExpression expression)
    {
        Visit(expression.Target);
    }

    protected virtual void VisitPostfixIncrementDecrementExpression(PostfixIncrementDecrementExpression expression)
    {
        Visit(expression.Target);
    }

    protected virtual void VisitCompoundLiteralExpression(CompoundLiteralExpression expression)
    {
        foreach (var specifier in expression.StorageClassSpecifiers)
        {
            Visit((IDeclarationSpecifier)specifier);
        }

        VisitTypeName(expression.TypeName);
        foreach (var initializer in expression.Initializers)
        {
            VisitInitializer(initializer);
        }
    }

    protected virtual void VisitPrefixIncrementDecrementExpression(PrefixIncrementDecrementExpression expression)
    {
        Visit(expression.Target);
    }

    protected virtual void VisitUnaryOperatorExpression(UnaryOperatorExpression expression)
    {
        Visit(expression.Target);
    }

    protected virtual void VisitIndirectionExpression(IndirectionExpression expression)
    {
        Visit(expression.Target);
    }

    protected virtual void VisitUnaryExpressionSizeOfOperatorExpression(UnaryExpressionSizeOfOperatorExpression expression)
    {
        Visit(expression.TargetExpession);
    }

    protected virtual void VisitTypeNameSizeOfOperatorExpression(TypeNameSizeOfOperatorExpression expression)
    {
        VisitTypeName(expression.TypeName);
    }

    protected virtual void VisitCastExpression(CastExpression expression)
    {
        VisitTypeName(expression.TypeName);
        Visit(expression.Target);
    }

    protected virtual void VisitBinaryOperatorExpression(BinaryOperatorExpression expression)
    {
        Visit(expression.Left);
        Visit(expression.Right);
    }

    protected virtual void VisitConditionalExpression(ConditionalExpression expression)
    {
        Visit(expression.Condition);
        Visit(expression.TrueExpression);
        Visit(expression.FalseExpression);
    }

    protected virtual void VisitCommaExpression(CommaExpression expression)
    {
        Visit(expression.Left);
        Visit(expression.Right);
    }

    protected virtual void VisitAmbiguousBlockItem(AmbiguousBlockItem ambiguousBlockItem)
    {
    }

    protected virtual void VisitBlockItem(IBlockItem blockItem)
    {
        switch (blockItem)
        {
            case Declaration declaration:
                Visit(declaration);
                break;
            case Statement statement:
                Visit(statement);
                break;
            case AmbiguousBlockItem ambiguousBlockItem:
                VisitAmbiguousBlockItem(ambiguousBlockItem);
                break;
            default:
                throw new AssertException($"Unknown block item of type {blockItem.GetType()}.");
        }
    }

    protected virtual void Visit(Statement statement)
    {
        switch (statement)
        {
            case LabelStatement labelStatement:
                VisitLabelStatement(labelStatement);
                break;
            case CaseStatement caseStatement:
                VisitCaseStatement(caseStatement);
                break;
            case CompoundStatement compoundStatement:
                VisitCompoundStatement(compoundStatement);
                break;
            case ExpressionStatement expressionStatement:
                VisitExpressionStatement(expressionStatement);
                break;
            case IfElseStatement ifElseStatement:
                VisitIfElseStatement(ifElseStatement);
                break;
            case SwitchStatement switchStatement:
                VisitSwitchStatement(switchStatement);
                break;
            case WhileStatement whileStatement:
                VisitWhileStatement(whileStatement);
                break;
            case DoWhileStatement doWhileStatement:
                VisitDoWhileStatement(doWhileStatement);
                break;
            case ForStatement forStatement:
                VisitForStatement(forStatement);
                break;
            case GoToStatement goToStatement:
                VisitGoToStatement(goToStatement);
                break;
            case BreakStatement:
                VisitBreakStatement();
                break;
            case ContinueStatement:
                VisitContinueStatement();
                break;
            case ReturnStatement returnStatement:
                VisitReturnStatement(returnStatement);
                break;
            default:
                throw new AssertException($"Unknown statement of type {statement.GetType()}.");
        }
    }

    protected virtual void VisitLabelStatement(LabelStatement statement)
    {
        Visit(statement.Body);
    }

    protected virtual void VisitCaseStatement(CaseStatement statement)
    {
        if (statement.Constant is not null)
        {
            Visit(statement.Constant);
        }

        Visit(statement.Body);
    }

    protected virtual void VisitCompoundStatement(CompoundStatement statement)
    {
        foreach (var blockItem in statement.Block)
        {
            VisitBlockItem(blockItem);
        }
    }

    protected virtual void VisitExpressionStatement(ExpressionStatement statement)
    {
        if (statement.Expression is not null)
        {
            Visit(statement.Expression);
        }
    }

    protected virtual void VisitIfElseStatement(IfElseStatement statement)
    {
        Visit(statement.Expression);
        Visit(statement.TrueBranch);
        if (statement.FalseBranch is not null)
        {
            Visit(statement.FalseBranch);
        }
    }

    protected virtual void VisitSwitchStatement(SwitchStatement statement)
    {
        Visit(statement.Expression);
        Visit(statement.Body);
    }

    protected virtual void VisitWhileStatement(WhileStatement statement)
    {
        Visit(statement.TestExpression);
        VisitBlockItem(statement.Body);
    }

    protected virtual void VisitDoWhileStatement(DoWhileStatement statement)
    {
        Visit(statement.TestExpression);
        VisitBlockItem(statement.Body);
    }

    protected virtual void VisitForStatement(ForStatement statement)
    {
        if (statement.InitDeclaration is not null)
        {
            VisitBlockItem(statement.InitDeclaration);
        }

        if (statement.InitExpression is not null)
        {
            Visit(statement.InitExpression);
        }

        if (statement.TestExpression is not null)
        {
            Visit(statement.TestExpression);
        }

        if (statement.UpdateExpression is not null)
        {
            Visit(statement.UpdateExpression);
        }

        VisitBlockItem(statement.Body);
    }

    protected virtual void VisitGoToStatement(GoToStatement statement)
    {
    }

    protected virtual void VisitBreakStatement()
    {
    }

    protected virtual void VisitContinueStatement()
    {
    }

    protected virtual void VisitReturnStatement(ReturnStatement statement)
    {
        Visit(statement.Expression);
    }
}
