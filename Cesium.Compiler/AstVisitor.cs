// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.Ast;
using Cesium.Core;

namespace Cesium.Compiler;

internal abstract class AstVisitor
{
    public virtual void Visit(TranslationUnit translationUnit)
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

        if (functionDefinition.Declarations is { } declarations)
        {
            foreach (var declaration in declarations)
            {
                Visit(declaration);
            }
        }

        Visit((Statement)functionDefinition.Statement);
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

        if (declaration.InitDeclarators is { } initDeclarations)
        {
            foreach (var initDeclarator in initDeclarations)
            {
                Visit(initDeclarator);
            }
        }
    }

    protected virtual void Visit(InitDeclarator initDeclarator)
    {
        Visit(initDeclarator.Declarator);
        if (initDeclarator.Initializer is not null)
        {
            Visit(initDeclarator.Initializer);
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

        if (arrayDirectDeclarator.TypeQualifiers is { } typeQualifiers)
        {
            foreach (var typeQualifier in typeQualifiers)
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

    protected virtual void Visit(TypeName typeName)
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

    protected virtual void Visit(Initializer initializer)
    {
        switch (initializer)
        {
            case AssignmentInitializer assignmentInitializer:
                Visit(assignmentInitializer);
                break;
            case ArrayInitializer arrayInitializer:
                Visit(arrayInitializer);
                break;
            default:
                throw new AssertException($"Unknown initializer of type {initializer.GetType()}.");
        }
    }

    protected virtual void Visit(AssignmentInitializer assignmentInitializer)
    {
        if (assignmentInitializer.Designation is not null)
        {
            Visit(assignmentInitializer.Designation);
        }

        Visit(assignmentInitializer.Expression);
    }

    protected virtual void Visit(ArrayInitializer arrayInitializer)
    {
        if (arrayInitializer.Designation is not null)
        {
            Visit(arrayInitializer.Designation);
        }

        foreach (var arraySubInitializer in arrayInitializer.Initializers)
        {
            Visit(arraySubInitializer);
        }
    }

    protected virtual void Visit(Designation designation)
    {
        foreach (var designator in designation.Designators)
        {
            Visit(designator);
        }
    }

    protected virtual void Visit(Designator designator)
    {
        switch (designator)
        {
            case BracketsDesignator bracketsDesignator:
                Visit(bracketsDesignator);
                break;
            case IdentifierDesignator identifierDesignator:
                Visit(identifierDesignator);
                break;
            default:
                throw new AssertException($"Unknown designator of type {designator.GetType()}.");
        }
    }

    protected virtual void Visit(BracketsDesignator bracketsDesignator)
    {
        Visit(bracketsDesignator.Expression);
    }

    protected virtual void Visit(IdentifierDesignator identifierDesignator)
    {
    }

    protected virtual void Visit(Expression expression)
    {
        switch (expression)
        {
            case StringLiteralListExpression expr:
                Visit(expr);
                break;
            case IdentifierExpression expr:
                Visit(expr);
                break;
            case ConstantLiteralExpression expr:
                Visit(expr);
                break;
            case ParenthesizedExpression expr:
                Visit(expr);
                break;
            case SubscriptingExpression expr:
                Visit(expr);
                break;
            case FunctionCallExpression expr:
                Visit(expr);
                break;
            case TypeCastOrNamedFunctionCallExpression expr:
                Visit(expr);
                break;
            case MemberAccessExpression expr:
                Visit(expr);
                break;
            case PointerMemberAccessExpression expr:
                Visit(expr);
                break;
            case PostfixIncrementDecrementExpression expr:
                Visit(expr);
                break;
            case CompoundLiteralExpression expr:
                Visit(expr);
                break;
            case PrefixIncrementDecrementExpression expr:
                Visit(expr);
                break;
            case UnaryOperatorExpression expr:
                Visit(expr);
                break;
            case IndirectionExpression expr:
                Visit(expr);
                break;
            case UnaryExpressionSizeOfOperatorExpression expr:
                Visit(expr);
                break;
            case TypeNameSizeOfOperatorExpression expr:
                Visit(expr);
                break;
            case CastExpression expr:
                Visit(expr);
                break;
            case BinaryOperatorExpression expr:
                Visit(expr);
                break;
            case ConditionalExpression expr:
                Visit(expr);
                break;
            case CommaExpression expr:
                Visit(expr);
                break;
            default:
                throw new AssertException($"Unknown expression of type {expression.GetType()}.");
        }
    }

    protected virtual void Visit(StringLiteralListExpression stringLiteralListExpression)
    {
    }

    protected virtual void Visit(IdentifierExpression identifierExpression)
    {
    }

    protected virtual void Visit(ConstantLiteralExpression constantLiteralExpression)
    {
    }

    protected virtual void Visit(ParenthesizedExpression expression)
    {
        Visit(expression.Contents);
    }

    protected virtual void Visit(SubscriptingExpression expression)
    {
        Visit(expression.Base);
        Visit(expression.Index);
    }

    protected virtual void Visit(FunctionCallExpression expression)
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

    protected virtual void Visit(TypeCastOrNamedFunctionCallExpression expression)
    {
        foreach (var argument in expression.Arguments)
        {
            Visit(argument);
        }
    }

    protected virtual void Visit(MemberAccessExpression expression)
    {
        Visit(expression.Target);
    }

    protected virtual void Visit(PointerMemberAccessExpression expression)
    {
        Visit(expression.Target);
    }

    protected virtual void Visit(PostfixIncrementDecrementExpression expression)
    {
        Visit(expression.Target);
    }

    protected virtual void Visit(CompoundLiteralExpression expression)
    {
        foreach (var specifier in expression.StorageClassSpecifiers)
        {
            Visit((IDeclarationSpecifier)specifier);
        }

        Visit(expression.TypeName);
        foreach (var initializer in expression.Initializers)
        {
            Visit(initializer);
        }
    }

    protected virtual void Visit(PrefixIncrementDecrementExpression expression)
    {
        Visit(expression.Target);
    }

    protected virtual void Visit(UnaryOperatorExpression expression)
    {
        Visit(expression.Target);
    }

    protected virtual void Visit(IndirectionExpression expression)
    {
        Visit(expression.Target);
    }

    protected virtual void Visit(UnaryExpressionSizeOfOperatorExpression expression)
    {
        Visit(expression.TargetExpession);
    }

    protected virtual void Visit(TypeNameSizeOfOperatorExpression expression)
    {
        Visit(expression.TypeName);
    }

    protected virtual void Visit(CastExpression expression)
    {
        Visit(expression.TypeName);
        Visit(expression.Target);
    }

    protected virtual void Visit(BinaryOperatorExpression expression)
    {
        Visit(expression.Left);
        Visit(expression.Right);
    }

    protected virtual void Visit(ConditionalExpression expression)
    {
        Visit(expression.Condition);
        Visit(expression.TrueExpression);
        Visit(expression.FalseExpression);
    }

    protected virtual void Visit(CommaExpression expression)
    {
        Visit(expression.Left);
        Visit(expression.Right);
    }

    protected virtual void Visit(AmbiguousBlockItem ambiguousBlockItem)
    {
    }

    protected virtual void Visit(IBlockItem blockItem)
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
                Visit(ambiguousBlockItem);
                break;
            default:
                throw new AssertException($"Unknown block item of type {blockItem.GetType()}.");
        }
    }

    protected virtual void Visit(Statement statement)
    {
        switch (statement)
        {
            case LabelStatement stmt:
                Visit(stmt);
                break;
            case CaseStatement stmt:
                Visit(stmt);
                break;
            case CompoundStatement stmt:
                Visit(stmt);
                break;
            case ExpressionStatement stmt:
                Visit(stmt);
                break;
            case IfElseStatement stmt:
                Visit(stmt);
                break;
            case SwitchStatement stmt:
                Visit(stmt);
                break;
            case WhileStatement stmt:
                Visit(stmt);
                break;
            case DoWhileStatement stmt:
                Visit(stmt);
                break;
            case ForStatement stmt:
                Visit(stmt);
                break;
            case GoToStatement stmt:
                Visit(stmt);
                break;
            case BreakStatement stmt:
                Visit(stmt);
                break;
            case ContinueStatement stmt:
                Visit(stmt);
                break;
            case ReturnStatement stmt:
                Visit(stmt);
                break;
            default:
                throw new AssertException($"Unknown statement of type {statement.GetType()}.");
        }
    }

    protected virtual void Visit(LabelStatement statement)
    {
        Visit(statement.Body);
    }

    protected virtual void Visit(CaseStatement statement)
    {
        if (statement.Constant is not null)
        {
            Visit(statement.Constant);
        }

        Visit(statement.Body);
    }

    protected virtual void Visit(CompoundStatement statement)
    {
        foreach (var blockItem in statement.Block)
        {
            Visit(blockItem);
        }
    }

    protected virtual void Visit(ExpressionStatement statement)
    {
        if (statement.Expression is not null)
        {
            Visit(statement.Expression);
        }
    }

    protected virtual void Visit(IfElseStatement statement)
    {
        Visit(statement.Expression);
        Visit(statement.TrueBranch);
        if (statement.FalseBranch is not null)
        {
            Visit(statement.FalseBranch);
        }
    }

    protected virtual void Visit(SwitchStatement statement)
    {
        Visit(statement.Expression);
        Visit(statement.Body);
    }

    protected virtual void Visit(WhileStatement statement)
    {
        Visit(statement.TestExpression);
        Visit(statement.Body);
    }

    protected virtual void Visit(DoWhileStatement statement)
    {
        Visit(statement.TestExpression);
        Visit(statement.Body);
    }

    protected virtual void Visit(ForStatement statement)
    {
        if (statement.InitDeclaration is not null)
        {
            Visit(statement.InitDeclaration);
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

        Visit(statement.Body);
    }

    protected virtual void Visit(GoToStatement statement)
    {
    }

    protected virtual void Visit(BreakStatement statement)
    {
    }

    protected virtual void Visit(ContinueStatement statement)
    {
    }

    protected virtual void Visit(ReturnStatement statement)
    {
        Visit(statement.Expression);
    }
}
