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

    public void Dump(TranslationUnit translationUnit) =>
        VisitTranslationUnit(translationUnit);

    protected override void Visit(TranslationUnit translationUnit)
    {
        _writer.WriteLine("TranslationUnitDecl");
        _writer.Indent++;
        base.Visit(translationUnit);
        _writer.Indent--;
    }

    protected override void Visit(FunctionDefinition functionDefinition)
    {
        _writer.WriteLine("FunctionDefinition");
        _writer.Indent++;
        VisitFunctionDeclarationSpecifiers(functionDefinition);

        Visit(functionDefinition.Declarator);

        _writer.Indent++;
        VisitFunctionDeclarations(functionDefinition);
        _writer.Indent--;

        Visit(functionDefinition.Statement);
        _writer.Indent--;
    }

    protected override void VisitFunctionDeclarationSpecifiers(FunctionDefinition functionDefinition)
    {
        _writer.WriteLine("Specifiers");
        _writer.Indent++;
        base.VisitFunctionDeclarationSpecifiers(functionDefinition);
        _writer.Indent--;
    }

    protected override void Visit(SymbolDeclaration symbolDeclaration)
    {
        _writer.WriteLine("SymbolDecl");
        _writer.Indent++;
        Visit(symbolDeclaration.Declaration);
        _writer.Indent--;
    }

    protected override void Visit(PInvokeDeclaration pInvokeDeclaration)
    {
        var prefixPart = pInvokeDeclaration.Prefix is null ? string.Empty : $" Prefix = {pInvokeDeclaration.Prefix}";
        _writer.WriteLine($"PInvokeDecl {pInvokeDeclaration.Declaration}{prefixPart}");
    }

    protected override void Visit(Declaration declaration)
    {
        _writer.WriteLine("Decl");
        _writer.Indent++;
        _writer.WriteLine("Specifiers");
        _writer.Indent++;
        foreach (var specifier in declaration.Specifiers)
        {
            Visit(specifier);
        }

        _writer.Indent--;
        if (declaration.InitDeclarators is not null)
        {
            _writer.Indent++;
            foreach (var initDeclarator in declaration.InitDeclarators)
            {
                VisitInitDeclarator(initDeclarator);
            }

            _writer.Indent--;
        }

        _writer.Indent--;
    }

    protected override void VisitInitDeclarator(InitDeclarator initDeclarator)
    {
        _writer.WriteLine("InitDecl");
        _writer.Indent++;
        Visit(initDeclarator.Declarator);
        if (initDeclarator.Initializer is not null)
        {
            VisitInitializer(initDeclarator.Initializer);
        }

        _writer.Indent--;
    }

    protected override void Visit(Declarator declarator)
    {
        _writer.WriteLine("Declarator");
        _writer.Indent++;
        if (declarator.Pointer is not null)
        {
            Visit(declarator.Pointer);
        }

        Visit(declarator.DirectDeclarator);
        _writer.Indent--;
    }

    protected override void Visit(IDirectDeclarator directDeclarator)
    {
        switch (directDeclarator)
        {
            case IdentifierDirectDeclarator identifierDirectDeclarator:
                _writer.WriteLine($"IdentifierDirectDeclarator {identifierDirectDeclarator.Identifier}");
                break;
            case ArrayDirectDeclarator arrayDirectDeclarator:
                _writer.WriteLine("ArrayDirectDeclarator");
                _writer.Indent++;
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
                _writer.Indent--;
                break;
            case ParameterListDirectDeclarator parameterListDirectDeclarator:
                _writer.WriteLine("ParameterListDirectDeclarator");
                _writer.Indent++;
                Visit(parameterListDirectDeclarator.Base);
                Visit(parameterListDirectDeclarator.Parameters);
                if (parameterListDirectDeclarator.Parameters.HasEllipsis)
                {
                    _writer.WriteLine("HasEllipsis");
                }

                _writer.Indent--;
                break;
            case IdentifierListDirectDeclarator identifierListDirectDeclarator:
                _writer.WriteLine("IdentifierListDirectDeclarator");
                _writer.Indent++;
                Visit(identifierListDirectDeclarator.Base);
                if (identifierListDirectDeclarator.Identifiers is not null)
                {
                    _writer.WriteLine($"Identifiers {string.Join(", ", identifierListDirectDeclarator.Identifiers)}");
                }

                _writer.Indent--;
                break;
            case DeclaratorDirectDeclarator declaratorDirectDeclarator:
                _writer.WriteLine("DeclaratorDirectDeclarator");
                _writer.Indent++;
                Visit(declaratorDirectDeclarator.Declarator);
                _writer.Indent--;
                break;
            default:
                throw new AssertException($"Unknown direct declarator of type {directDeclarator.GetType()}.");
        }
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
        _writer.WriteLine("ParameterDeclaration");
        _writer.Indent++;
        foreach (var declarationSpecifier in parameterDeclaration.Specifiers)
        {
            Visit(declarationSpecifier);
        }
        if (parameterDeclaration.Declarator is not null)
        {
            Visit(parameterDeclaration.Declarator);
        }
        if (parameterDeclaration.AbstractDeclarator is not null)
        {
            Visit(parameterDeclaration.AbstractDeclarator);
        }

        _writer.Indent--;
    }

    protected override void Visit(AbstractDeclarator abstractDeclarator)
    {
        _writer.WriteLine("ParameterDeclaration");
        _writer.Indent++;
        if (abstractDeclarator.Pointer is not null)
        {
            Visit(abstractDeclarator.Pointer);
        }
        if (abstractDeclarator.DirectAbstractDeclarator is not null)
        {
            Visit(abstractDeclarator.DirectAbstractDeclarator);
        }

        _writer.Indent--;
    }

    protected override void Visit(IDirectAbstractDeclarator directAbstractDeclarator)
    {
        switch (directAbstractDeclarator)
        {
            case SimpleDirectAbstractDeclarator simpleDirectAbstractDeclarator:
                _writer.WriteLine("SimpleDirectAbstractDeclarator");
                _writer.Indent++;
                Visit(simpleDirectAbstractDeclarator.Declarator);
                _writer.Indent--;
                break;
            case ArrayDirectAbstractDeclarator arrayDirectDeclarator:
                _writer.WriteLine("ArrayDirectDeclarator");
                _writer.Indent++;
                if (arrayDirectDeclarator.Base is not null)
                {
                    Visit(arrayDirectDeclarator.Base);
                }

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

                _writer.Indent--;
                break;
            default:
                throw new AssertException($"Unknown direct abstract declarator of type {directAbstractDeclarator.GetType()}.");
        }
    }

    protected override void Visit(Pointer pointer)
    {
        _writer.WriteLine("Pointer");
        _writer.Indent--;
        if (pointer.TypeQualifiers is not null)
        {
            _writer.Indent++;
            foreach (var typeQualifier in pointer.TypeQualifiers)
            {
                Visit(typeQualifier);
            }

            _writer.Indent--;
        }
        if (pointer.ChildPointer is not null)
        {
            Visit(pointer.ChildPointer);
        }
        _writer.Indent++;
    }

    protected override void VisitInitializer(Initializer initializer)
    {
        switch (initializer)
        {
            case AssignmentInitializer assignmentInitializer:
                _writer.WriteLine("AssignmentInitializer");
                _writer.Indent++;
                if (initializer.Designation is not null)
                {
                    VisitDesignation(initializer.Designation);
                }

                Visit(assignmentInitializer.Expression);
                _writer.Indent--;
                break;
            case ArrayInitializer arrayInitializer:
                _writer.WriteLine("ArrayInitializer");
                _writer.Indent++;
                if (initializer.Designation is not null)
                {
                    VisitDesignation(initializer.Designation);
                }

                foreach (var arraySubInitializer in arrayInitializer.Initializers)
                {
                    VisitInitializer(arraySubInitializer);
                }

                _writer.Indent--;
                break;
            default:
                throw new AssertException($"Unknown initializer of type {initializer.GetType()}.");
        }
    }

    protected override void VisitDesignation(Designation designation)
    {
        _writer.WriteLine("Designation");
        _writer.Indent++;
        foreach (var designator in designation.Designators)
        {
            VisitDesignator(designator);
        }

        _writer.Indent--;
    }

    protected override void VisitDesignator(Designator designator)
    {
        switch (designator)
        {
            case BracketsDesignator bracketsDesignator:
                _writer.WriteLine("BracketsDesignator");
                _writer.Indent++;
                Visit(bracketsDesignator.Expression);
                _writer.Indent--;
                break;
            case IdentifierDesignator identifierDesignator:
                _writer.WriteLine($"IdentifierDesignator .{identifierDesignator.FieldName}");
                break;
            default:
                throw new AssertException($"Unknown designator of type {designator.GetType()}.");
        }
    }

    protected override void Visit(IDeclarationSpecifier specifier)
    {
        switch (specifier)
        {
            case StorageClassSpecifier storageClassSpecifier:
                _writer.WriteLine($"StorageClassSpecifier {storageClassSpecifier.Name}");
                break;
            case CliImportSpecifier cliImportSpecifier:
                _writer.WriteLine($"CliImportSpecifier {cliImportSpecifier.MemberName}");
                break;
            case ISpecifierQualifierListItem specifierQualifierListItem:
                Visit(specifierQualifierListItem);
                break;
            default:
                throw new AssertException($"Unknown declaration specifier of type {specifier.GetType()}.");
        }
    }

    protected override void Visit(ISpecifierQualifierListItem specifierQualifierListItem)
    {
        switch (specifierQualifierListItem)
        {
            case TypeQualifier typeQualifier:
                Visit(typeQualifier);
                break;
            case ITypeSpecifier typeSpecifier:
                VisitITypeSpecifier(typeSpecifier);
                break;
            default:
                throw new AssertException($"Unknown specifier qualified list item of type {specifierQualifierListItem.GetType()}.");
        }
    }

    private void VisitITypeSpecifier(ITypeSpecifier typeSpecifier)
    {
        switch (typeSpecifier)
        {
            case SimpleTypeSpecifier simpleTypeSpecifier:
                _writer.WriteLine($"SimpleTypeSpecifier {simpleTypeSpecifier.TypeName}");
                break;
            case StructOrUnionSpecifier structOrUnionSpecifier:
                Visit(structOrUnionSpecifier);
                break;
            case EnumSpecifier enumSpecifier:
                Visit(enumSpecifier);
                break;
            case NamedTypeSpecifier namedTypeSpecifier:
                _writer.WriteLine($"NamedTypeSpecifier {namedTypeSpecifier.TypeDefName}");
                break;
            default:
                throw new AssertException($"Unknown type specifier of type {typeSpecifier.GetType()}.");
        }
    }

    protected override void Visit(TypeQualifier typeQualifier)
    {
        _writer.WriteLine($"TypeQualifier {typeQualifier.Name}");
    }

    protected override void Visit(StructOrUnionSpecifier structOrUnionSpecifier)
    {
        _writer.WriteLine($"StructOrUnionSpecifier {structOrUnionSpecifier.TypeKind} {structOrUnionSpecifier.Identifier}");
        foreach (var structDeclaration in structOrUnionSpecifier.StructDeclarations)
        {
            _writer.WriteLine("StructDeclaration");
            _writer.Indent++;
            _writer.WriteLine("SpecifiersQualifiers");
            _writer.Indent++;
            foreach (var specifierQualifierListItem in structDeclaration.SpecifiersQualifiers)
            {
                Visit(specifierQualifierListItem);
            }

            _writer.Indent--;
            _writer.WriteLine("SpecifiersQualifiers");
            if (structDeclaration.Declarators is not null)
            {
                _writer.Indent++;
                foreach (var structDeclarator in structDeclaration.Declarators)
                {
                    Visit(structDeclarator);
                }

                _writer.Indent--;
            }

            _writer.Indent--;
        }
    }

    protected override void Visit(StructDeclarator structDeclarator)
    {
        _writer.WriteLine("StructDeclarator");
        _writer.Indent++;
        Visit(structDeclarator.Declarator);
        _writer.Indent--;
    }

    protected override void Visit(EnumSpecifier enumSpecifier)
    {
        _writer.WriteLine($"EnumSpecifier {enumSpecifier.Identifier}");
        if (enumSpecifier.EnumDeclarations is not null)
        {
            foreach (var enumDeclaration in enumSpecifier.EnumDeclarations)
            {
                _writer.WriteLine($"EnumDeclaration {enumDeclaration.Identifier}");
                if (enumDeclaration.Constant is not null)
                {
                    _writer.Indent++;
                    Visit(enumDeclaration.Constant);
                    _writer.Indent--;
                }
            }
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
