// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.Ast;
using Cesium.Core;

namespace Cesium.Compiler;

internal abstract class AstVisitor
{
    public void Visit(TranslationUnit translationUnit)
    {
        ArgumentNullException.ThrowIfNull(translationUnit);
        VisitTranslationUnit(translationUnit);
    }

    protected virtual void VisitTranslationUnit(TranslationUnit translationUnit)
    {
        foreach (var declaration in translationUnit.Declarations)
        {
            VisitExternalDeclaration(declaration);
        }
    }

    protected virtual void VisitExternalDeclaration(ExternalDeclaration declaration)
    {
        switch (declaration)
        {
            case FunctionDefinition functionDefinition:
                VisitFunctionDefinition(functionDefinition);
                break;
            case SymbolDeclaration symbolDeclaration:
                VisitSymbolDeclaration(symbolDeclaration);
                break;
            case PInvokeDeclaration pInvokeDeclaration:
                VisitPInvokeDeclaration(pInvokeDeclaration);
                break;
            default:
                throw new AssertException($"Unknown external declaration of type {declaration.GetType()}.");
        }
    }

    protected virtual void VisitFunctionDefinition(FunctionDefinition functionDefinition)
    {
        foreach (var specifier in functionDefinition.Specifiers)
        {
            VisitDeclarationSpecifier(specifier);
        }

        VisitDeclarator(functionDefinition.Declarator);

        if (functionDefinition.Declarations is not null)
        {
            foreach (var declaration in functionDefinition.Declarations)
            {
                VisitDeclaration(declaration);
            }
        }

        VisitStatement(functionDefinition.Statement);
    }

    protected virtual void VisitSymbolDeclaration(SymbolDeclaration symbolDeclaration)
    {
        VisitDeclaration(symbolDeclaration.Declaration);
    }

    protected virtual void VisitPInvokeDeclaration(PInvokeDeclaration pInvokeDeclaration)
    {
    }

    protected virtual void VisitDeclaration(Declaration declaration)
    {
        foreach (var specifier in declaration.Specifiers)
        {
            VisitDeclarationSpecifier(specifier);
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
        VisitDeclarator(initDeclarator.Declarator);
        if (initDeclarator.Initializer is not null)
        {
            VisitInitializer(initDeclarator.Initializer);
        }
    }

    protected virtual void VisitDeclarator(Declarator declarator)
    {
        if (declarator.Pointer is not null)
        {
            VisitPointer(declarator.Pointer);
        }

        VisitIDirectDeclarator(declarator.DirectDeclarator);
    }

    protected virtual void VisitIDirectDeclarator(IDirectDeclarator directDeclarator)
    {
        switch (directDeclarator)
        {
            case IdentifierDirectDeclarator identifierDirectDeclarator:
                VisitIdentifierDirectDeclarator(identifierDirectDeclarator);
                break;
            case ArrayDirectDeclarator arrayDirectDeclarator:
                VisitArrayDirectDeclarator(arrayDirectDeclarator);
                break;
            case ParameterListDirectDeclarator parameterListDirectDeclarator:
                VisitParameterListDirectDeclarator(parameterListDirectDeclarator);
                break;
            case IdentifierListDirectDeclarator identifierListDirectDeclarator:
                VisitIdentifierListDirectDeclarator(identifierListDirectDeclarator);
                break;
            case DeclaratorDirectDeclarator declaratorDirectDeclarator:
                VisitDeclaratorDirectDeclarator(declaratorDirectDeclarator);
                break;
            default:
                throw new AssertException($"Unknown direct declarator of type {directDeclarator.GetType()}.");
        }
    }

    protected virtual void VisitIdentifierDirectDeclarator(IdentifierDirectDeclarator identifierDirectDeclarator)
    {
    }

    protected virtual void VisitArrayDirectDeclarator(ArrayDirectDeclarator arrayDirectDeclarator)
    {
        VisitIDirectDeclarator(arrayDirectDeclarator.Base);

        if (arrayDirectDeclarator.TypeQualifiers is not null)
        {
            foreach (var typeQualifier in arrayDirectDeclarator.TypeQualifiers)
            {
                VisitTypeQualifier(typeQualifier);
            }
        }

        if (arrayDirectDeclarator.Size is not null)
        {
            VisitExpression(arrayDirectDeclarator.Size);
        }
    }

    protected virtual void VisitParameterListDirectDeclarator(ParameterListDirectDeclarator parameterListDirectDeclarator)
    {
        VisitIDirectDeclarator(parameterListDirectDeclarator.Base);
        VisitParameterTypeList(parameterListDirectDeclarator.Parameters);
    }

    protected virtual void VisitIdentifierListDirectDeclarator(IdentifierListDirectDeclarator identifierListDirectDeclarator)
    {
        VisitIDirectDeclarator(identifierListDirectDeclarator.Base);
    }

    protected virtual void VisitDeclaratorDirectDeclarator(DeclaratorDirectDeclarator declaratorDirectDeclarator)
    {
        VisitDeclarator(declaratorDirectDeclarator.Declarator);
    }

    protected virtual void VisitPointer(Pointer pointer)
    {
        if (pointer.TypeQualifiers is not null)
        {
            foreach (var typeQualifier in pointer.TypeQualifiers)
            {
                VisitTypeQualifier(typeQualifier);
            }
        }

        if (pointer.ChildPointer is not null)
        {
            VisitPointer(pointer.ChildPointer);
        }
    }

    protected virtual void VisitDeclarationSpecifier(IDeclarationSpecifier specifier)
    {
        switch (specifier)
        {
            case StorageClassSpecifier storageClassSpecifier:
                VisitStorageClassSpecifier(storageClassSpecifier);
                break;
            case CliImportSpecifier cliImportSpecifier:
                VisitCliImportSpecifier(cliImportSpecifier);
                break;
            case FunctionSpecifier functionSpecifier:
                VisitFunctionSpecifier(functionSpecifier);
                break;
            case ISpecifierQualifierListItem specifierQualifierListItem:
                VisitSpecifierQualifierListItem(specifierQualifierListItem);
                break;
            default:
                throw new AssertException($"Unknown declaration specifier of type {specifier.GetType()}.");
        }
    }

    protected virtual void VisitStorageClassSpecifier(StorageClassSpecifier storageClassSpecifier)
    {
    }

    protected virtual void VisitCliImportSpecifier(CliImportSpecifier cliImportSpecifier)
    {
    }

    protected virtual void VisitFunctionSpecifier(FunctionSpecifier functionSpecifier)
    {
    }

    protected virtual void VisitSpecifierQualifierListItem(ISpecifierQualifierListItem specifierQualifierListItem)
    {
        switch (specifierQualifierListItem)
        {
            case TypeQualifier typeQualifier:
                VisitTypeQualifier(typeQualifier);
                break;
            case ITypeSpecifier typeSpecifier:
                VisitTypeSpecifier(typeSpecifier);
                break;
            default:
                throw new AssertException($"Unknown specifier qualifier list item of type {specifierQualifierListItem.GetType()}.");
        }
    }

    protected virtual void VisitTypeQualifier(TypeQualifier typeQualifier)
    {
    }

    protected virtual void VisitTypeSpecifier(ITypeSpecifier typeSpecifier)
    {
        switch (typeSpecifier)
        {
            case SimpleTypeSpecifier simpleTypeSpecifier:
                VisitSimpleTypeSpecifier(simpleTypeSpecifier);
                break;
            case StructOrUnionSpecifier structOrUnionSpecifier:
                VisitStructOrUnionSpecifier(structOrUnionSpecifier);
                break;
            case EnumSpecifier enumSpecifier:
                VisitEnumSpecifier(enumSpecifier);
                break;
            case NamedTypeSpecifier namedTypeSpecifier:
                VisitNamedTypeSpecifier(namedTypeSpecifier);
                break;
            default:
                throw new AssertException($"Unknown type specifier of type {typeSpecifier.GetType()}.");
        }
    }

    protected virtual void VisitSimpleTypeSpecifier(SimpleTypeSpecifier simpleTypeSpecifier)
    {
    }

    protected virtual void VisitNamedTypeSpecifier(NamedTypeSpecifier namedTypeSpecifier)
    {
    }

    protected virtual void VisitStructOrUnionSpecifier(StructOrUnionSpecifier structOrUnionSpecifier)
    {
        foreach (var structDeclaration in structOrUnionSpecifier.StructDeclarations)
        {
            VisitStructDeclaration(structDeclaration);
        }
    }

    protected virtual void VisitStructDeclaration(StructDeclaration structDeclaration)
    {
        foreach (var specifierQualifierListItem in structDeclaration.SpecifiersQualifiers)
        {
            VisitSpecifierQualifierListItem(specifierQualifierListItem);
        }

        if (structDeclaration.Declarators is not null)
        {
            foreach (var structDeclarator in structDeclaration.Declarators)
            {
                VisitStructDeclarator(structDeclarator);
            }
        }
    }

    protected virtual void VisitStructDeclarator(StructDeclarator structDeclarator)
    {
        VisitDeclarator(structDeclarator.Declarator);
    }

    protected virtual void VisitEnumSpecifier(EnumSpecifier enumSpecifier)
    {
        if (enumSpecifier.EnumDeclarations is not null)
        {
            foreach (var enumDeclaration in enumSpecifier.EnumDeclarations)
            {
                VisitEnumDeclaration(enumDeclaration);
            }
        }
    }

    protected virtual void VisitEnumDeclaration(EnumDeclaration enumDeclaration)
    {
        if (enumDeclaration.Constant is not null)
        {
            VisitExpression(enumDeclaration.Constant);
        }
    }

    protected virtual void VisitTypeName(TypeName typeName)
    {
        foreach (var specifierQualifierListItem in typeName.SpecifierQualifierList)
        {
            VisitSpecifierQualifierListItem(specifierQualifierListItem);
        }

        if (typeName.AbstractDeclarator is not null)
        {
            VisitAbstractDeclarator(typeName.AbstractDeclarator);
        }
    }

    protected virtual void VisitAbstractDeclarator(AbstractDeclarator abstractDeclarator)
    {
        if (abstractDeclarator.Pointer is not null)
        {
            VisitPointer(abstractDeclarator.Pointer);
        }

        if (abstractDeclarator.DirectAbstractDeclarator is not null)
        {
            VisitIDirectAbstractDeclarator(abstractDeclarator.DirectAbstractDeclarator);
        }
    }

    protected virtual void VisitIDirectAbstractDeclarator(IDirectAbstractDeclarator directAbstractDeclarator)
    {
        switch (directAbstractDeclarator)
        {
            case SimpleDirectAbstractDeclarator simpleDirectAbstractDeclarator:
                VisitSimpleDirectAbstractDeclarator(simpleDirectAbstractDeclarator);
                break;
            case ArrayDirectAbstractDeclarator arrayDirectAbstractDeclarator:
                VisitArrayDirectAbstractDeclarator(arrayDirectAbstractDeclarator);
                break;
            default:
                throw new AssertException($"Unknown direct abstract declarator of type {directAbstractDeclarator.GetType()}.");
        }
    }

    protected virtual void VisitSimpleDirectAbstractDeclarator(SimpleDirectAbstractDeclarator simpleDirectAbstractDeclarator)
    {
        VisitAbstractDeclarator(simpleDirectAbstractDeclarator.Declarator);
    }

    protected virtual void VisitArrayDirectAbstractDeclarator(ArrayDirectAbstractDeclarator arrayDirectAbstractDeclarator)
    {
        if (arrayDirectAbstractDeclarator.Base is not null)
        {
            VisitIDirectAbstractDeclarator(arrayDirectAbstractDeclarator.Base);
        }

        if (arrayDirectAbstractDeclarator.TypeQualifiers is not null)
        {
            foreach (var typeQualifier in arrayDirectAbstractDeclarator.TypeQualifiers)
            {
                VisitTypeQualifier(typeQualifier);
            }
        }

        if (arrayDirectAbstractDeclarator.Size is not null)
        {
            VisitExpression(arrayDirectAbstractDeclarator.Size);
        }
    }

    protected virtual void VisitParameterTypeList(ParameterTypeList parameterTypeList)
    {
        foreach (var parameterDeclaration in parameterTypeList.Parameters)
        {
            VisitParameterDeclaration(parameterDeclaration);
        }
    }

    protected virtual void VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
    {
        foreach (var specifier in parameterDeclaration.Specifiers)
        {
            VisitDeclarationSpecifier(specifier);
        }

        if (parameterDeclaration.Declarator is not null)
        {
            VisitDeclarator(parameterDeclaration.Declarator);
        }

        if (parameterDeclaration.AbstractDeclarator is not null)
        {
            VisitAbstractDeclarator(parameterDeclaration.AbstractDeclarator);
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
                VisitExpression(assignmentInitializer.Expression);
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
                VisitExpression(bracketsDesignator.Expression);
                break;
            case IdentifierDesignator:
                break;
            default:
                throw new AssertException($"Unknown designator of type {designator.GetType()}.");
        }
    }

    protected virtual void VisitExpression(Expression expression)
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
        VisitExpression(expression.Contents);
    }

    protected virtual void VisitSubscriptingExpression(SubscriptingExpression expression)
    {
        VisitExpression(expression.Base);
        VisitExpression(expression.Index);
    }

    protected virtual void VisitFunctionCallExpression(FunctionCallExpression expression)
    {
        VisitExpression(expression.Function);
        if (expression.Arguments is not null)
        {
            foreach (var argument in expression.Arguments)
            {
                VisitExpression(argument);
            }
        }
    }

    protected virtual void VisitTypeCastOrNamedFunctionCallExpression(TypeCastOrNamedFunctionCallExpression expression)
    {
        foreach (var argument in expression.Arguments)
        {
            VisitExpression(argument);
        }
    }

    protected virtual void VisitMemberAccessExpression(MemberAccessExpression expression)
    {
        VisitExpression(expression.Target);
    }

    protected virtual void VisitPointerMemberAccessExpression(PointerMemberAccessExpression expression)
    {
        VisitExpression(expression.Target);
    }

    protected virtual void VisitPostfixIncrementDecrementExpression(PostfixIncrementDecrementExpression expression)
    {
        VisitExpression(expression.Target);
    }

    protected virtual void VisitCompoundLiteralExpression(CompoundLiteralExpression expression)
    {
        foreach (var specifier in expression.StorageClassSpecifiers)
        {
            VisitDeclarationSpecifier(specifier);
        }

        VisitTypeName(expression.TypeName);
        foreach (var initializer in expression.Initializers)
        {
            VisitInitializer(initializer);
        }
    }

    protected virtual void VisitPrefixIncrementDecrementExpression(PrefixIncrementDecrementExpression expression)
    {
        VisitExpression(expression.Target);
    }

    protected virtual void VisitUnaryOperatorExpression(UnaryOperatorExpression expression)
    {
        VisitExpression(expression.Target);
    }

    protected virtual void VisitIndirectionExpression(IndirectionExpression expression)
    {
        VisitExpression(expression.Target);
    }

    protected virtual void VisitUnaryExpressionSizeOfOperatorExpression(UnaryExpressionSizeOfOperatorExpression expression)
    {
        VisitExpression(expression.TargetExpession);
    }

    protected virtual void VisitTypeNameSizeOfOperatorExpression(TypeNameSizeOfOperatorExpression expression)
    {
        VisitTypeName(expression.TypeName);
    }

    protected virtual void VisitCastExpression(CastExpression expression)
    {
        VisitTypeName(expression.TypeName);
        VisitExpression(expression.Target);
    }

    protected virtual void VisitBinaryOperatorExpression(BinaryOperatorExpression expression)
    {
        VisitExpression(expression.Left);
        VisitExpression(expression.Right);
    }

    protected virtual void VisitConditionalExpression(ConditionalExpression expression)
    {
        VisitExpression(expression.Condition);
        VisitExpression(expression.TrueExpression);
        VisitExpression(expression.FalseExpression);
    }

    protected virtual void VisitCommaExpression(CommaExpression expression)
    {
        VisitExpression(expression.Left);
        VisitExpression(expression.Right);
    }

    protected virtual void VisitAmbiguousBlockItem(AmbiguousBlockItem ambiguousBlockItem)
    {
    }

    protected virtual void VisitBlockItem(IBlockItem blockItem)
    {
        switch (blockItem)
        {
            case Declaration declaration:
                VisitDeclaration(declaration);
                break;
            case Statement statement:
                VisitStatement(statement);
                break;
            case AmbiguousBlockItem ambiguousBlockItem:
                VisitAmbiguousBlockItem(ambiguousBlockItem);
                break;
            default:
                throw new AssertException($"Unknown block item of type {blockItem.GetType()}.");
        }
    }

    protected virtual void VisitStatement(Statement statement)
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
        VisitStatement(statement.Body);
    }

    protected virtual void VisitCaseStatement(CaseStatement statement)
    {
        if (statement.Constant is not null)
        {
            VisitExpression(statement.Constant);
        }

        VisitStatement(statement.Body);
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
            VisitExpression(statement.Expression);
        }
    }

    protected virtual void VisitIfElseStatement(IfElseStatement statement)
    {
        VisitExpression(statement.Expression);
        VisitStatement(statement.TrueBranch);
        if (statement.FalseBranch is not null)
        {
            VisitStatement(statement.FalseBranch);
        }
    }

    protected virtual void VisitSwitchStatement(SwitchStatement statement)
    {
        VisitExpression(statement.Expression);
        VisitStatement(statement.Body);
    }

    protected virtual void VisitWhileStatement(WhileStatement statement)
    {
        VisitExpression(statement.TestExpression);
        VisitBlockItem(statement.Body);
    }

    protected virtual void VisitDoWhileStatement(DoWhileStatement statement)
    {
        VisitExpression(statement.TestExpression);
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
            VisitExpression(statement.InitExpression);
        }

        if (statement.TestExpression is not null)
        {
            VisitExpression(statement.TestExpression);
        }

        if (statement.UpdateExpression is not null)
        {
            VisitExpression(statement.UpdateExpression);
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
        VisitExpression(statement.Expression);
    }
}
