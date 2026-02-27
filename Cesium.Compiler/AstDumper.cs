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

    public void Dump(TranslationUnit translationUnit) => Visit(translationUnit);

    protected override void VisitTranslationUnit(TranslationUnit translationUnit)
    {
        _writer.WriteLine("TranslationUnitDecl");
        _writer.Indent++;
        base.VisitTranslationUnit(translationUnit);
        _writer.Indent--;
    }

    protected override void VisitFunctionDefinition(FunctionDefinition functionDefinition)
    {
        _writer.WriteLine("FunctionDefinition");
        _writer.Indent++;
        _writer.WriteLine("Specifiers");
        _writer.Indent++;
        foreach (var declarationSpecifier in functionDefinition.Specifiers)
        {
            VisitDeclarationSpecifier(declarationSpecifier);
        }

        _writer.Indent--;
        VisitDeclarator(functionDefinition.Declarator);
        if (functionDefinition.Declarations is not null)
        {
            _writer.Indent++;
            foreach (var declaration in functionDefinition.Declarations)
            {
                VisitDeclaration(declaration);
            }

            _writer.Indent--;
        }

        VisitStatement(functionDefinition.Statement);
        _writer.Indent--;
    }

    protected override void VisitSymbolDeclaration(SymbolDeclaration symbolDeclaration)
    {
        _writer.WriteLine("SymbolDecl");
        _writer.Indent++;
        VisitDeclaration(symbolDeclaration.Declaration);
        _writer.Indent--;
    }

    protected override void VisitPInvokeDeclaration(PInvokeDeclaration pInvokeDeclaration)
    {
        var prefixPart = pInvokeDeclaration.Prefix is null ? string.Empty : $" Prefix = {pInvokeDeclaration.Prefix}";
        _writer.WriteLine($"PInvokeDecl {pInvokeDeclaration.Declaration}{prefixPart}");
    }

    protected override void VisitDeclaration(Declaration declaration)
    {
        _writer.WriteLine("Decl");
        _writer.Indent++;
        _writer.WriteLine("Specifiers");
        _writer.Indent++;
        foreach (var specifier in declaration.Specifiers)
        {
            VisitDeclarationSpecifier(specifier);
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
        VisitDeclarator(initDeclarator.Declarator);
        if (initDeclarator.Initializer is not null)
        {
            VisitInitializer(initDeclarator.Initializer);
        }

        _writer.Indent--;
    }

    protected override void VisitDeclarator(Declarator declarator)
    {
        _writer.WriteLine("Declarator");
        _writer.Indent++;
        if (declarator.Pointer is not null)
        {
            VisitPointer(declarator.Pointer);
        }

        VisitIDirectDeclarator(declarator.DirectDeclarator);
        _writer.Indent--;
    }

    protected override void VisitIDirectDeclarator(IDirectDeclarator directDeclarator)
    {
        switch (directDeclarator)
        {
            case IdentifierDirectDeclarator identifierDirectDeclarator:
                _writer.WriteLine($"IdentifierDirectDeclarator {identifierDirectDeclarator.Identifier}");
                break;
            case ArrayDirectDeclarator arrayDirectDeclarator:
                _writer.WriteLine("ArrayDirectDeclarator");
                _writer.Indent++;
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
                _writer.Indent--;
                break;
            case ParameterListDirectDeclarator parameterListDirectDeclarator:
                _writer.WriteLine("ParameterListDirectDeclarator");
                _writer.Indent++;
                VisitIDirectDeclarator(parameterListDirectDeclarator.Base);
                VisitParameterTypeList(parameterListDirectDeclarator.Parameters);
                if (parameterListDirectDeclarator.Parameters.HasEllipsis)
                {
                    _writer.WriteLine("HasEllipsis");
                }

                _writer.Indent--;
                break;
            case IdentifierListDirectDeclarator identifierListDirectDeclarator:
                _writer.WriteLine("IdentifierListDirectDeclarator");
                _writer.Indent++;
                VisitIDirectDeclarator(identifierListDirectDeclarator.Base);
                if (identifierListDirectDeclarator.Identifiers is not null)
                {
                    _writer.WriteLine($"Identifiers {string.Join(", ", identifierListDirectDeclarator.Identifiers)}");
                }

                _writer.Indent--;
                break;
            case DeclaratorDirectDeclarator declaratorDirectDeclarator:
                _writer.WriteLine("DeclaratorDirectDeclarator");
                _writer.Indent++;
                VisitDeclarator(declaratorDirectDeclarator.Declarator);
                _writer.Indent--;
                break;
            default:
                throw new AssertException($"Unknown direct declarator of type {directDeclarator.GetType()}.");
        }
    }

    protected override void VisitParameterTypeList(ParameterTypeList parameterTypeList)
    {
        foreach (var parameterDeclaration in parameterTypeList.Parameters)
        {
            VisitParameterDeclaration(parameterDeclaration);
        }
    }

    protected override void VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
    {
        _writer.WriteLine("ParameterDeclaration");
        _writer.Indent++;
        foreach (var declarationSpecifier in parameterDeclaration.Specifiers)
        {
            VisitDeclarationSpecifier(declarationSpecifier);
        }
        if (parameterDeclaration.Declarator is not null)
        {
            VisitDeclarator(parameterDeclaration.Declarator);
        }
        if (parameterDeclaration.AbstractDeclarator is not null)
        {
            VisitAbstractDeclarator(parameterDeclaration.AbstractDeclarator);
        }

        _writer.Indent--;
    }

    protected override void VisitAbstractDeclarator(AbstractDeclarator abstractDeclarator)
    {
        _writer.WriteLine("ParameterDeclaration");
        _writer.Indent++;
        if (abstractDeclarator.Pointer is not null)
        {
            VisitPointer(abstractDeclarator.Pointer);
        }
        if (abstractDeclarator.DirectAbstractDeclarator is not null)
        {
            VisitIDirectAbstractDeclarator(abstractDeclarator.DirectAbstractDeclarator);
        }

        _writer.Indent--;
    }

    protected override void VisitIDirectAbstractDeclarator(IDirectAbstractDeclarator directAbstractDeclarator)
    {
        switch (directAbstractDeclarator)
        {
            case SimpleDirectAbstractDeclarator simpleDirectAbstractDeclarator:
                _writer.WriteLine("SimpleDirectAbstractDeclarator");
                _writer.Indent++;
                VisitAbstractDeclarator(simpleDirectAbstractDeclarator.Declarator);
                _writer.Indent--;
                break;
            case ArrayDirectAbstractDeclarator arrayDirectDeclarator:
                _writer.WriteLine("ArrayDirectDeclarator");
                _writer.Indent++;
                if (arrayDirectDeclarator.Base is not null)
                {
                    VisitIDirectAbstractDeclarator(arrayDirectDeclarator.Base);
                }

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

                _writer.Indent--;
                break;
            default:
                throw new AssertException($"Unknown direct abstract declarator of type {directAbstractDeclarator.GetType()}.");
        }
    }

    protected override void VisitPointer(Pointer pointer)
    {
        _writer.WriteLine("Pointer");
        _writer.Indent--;
        if (pointer.TypeQualifiers is not null)
        {
            _writer.Indent++;
            foreach (var typeQualifier in pointer.TypeQualifiers)
            {
                VisitTypeQualifier(typeQualifier);
            }

            _writer.Indent--;
        }
        if (pointer.ChildPointer is not null)
        {
            VisitPointer(pointer.ChildPointer);
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

                VisitExpression(assignmentInitializer.Expression);
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
                VisitExpression(bracketsDesignator.Expression);
                _writer.Indent--;
                break;
            case IdentifierDesignator identifierDesignator:
                _writer.WriteLine($"IdentifierDesignator .{identifierDesignator.FieldName}");
                break;
            default:
                throw new AssertException($"Unknown designator of type {designator.GetType()}.");
        }
    }

    protected override void VisitDeclarationSpecifier(IDeclarationSpecifier specifier)
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
                VisitSpecifierQualifierListItem(specifierQualifierListItem);
                break;
            default:
                throw new AssertException($"Unknown declaration specifier of type {specifier.GetType()}.");
        }
    }

    protected override void VisitSpecifierQualifierListItem(ISpecifierQualifierListItem specifierQualifierListItem)
    {
        switch (specifierQualifierListItem)
        {
            case TypeQualifier typeQualifier:
                VisitTypeQualifier(typeQualifier);
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
                VisitStructOrUnionSpecifier(structOrUnionSpecifier);
                break;
            case EnumSpecifier enumSpecifier:
                VisitEnumSpecifier(enumSpecifier);
                break;
            case NamedTypeSpecifier namedTypeSpecifier:
                _writer.WriteLine($"NamedTypeSpecifier {namedTypeSpecifier.TypeDefName}");
                break;
            default:
                throw new AssertException($"Unknown type specifier of type {typeSpecifier.GetType()}.");
        }
    }

    protected override void VisitTypeQualifier(TypeQualifier typeQualifier)
    {
        _writer.WriteLine($"TypeQualifier {typeQualifier.Name}");
    }

    protected override void VisitStructOrUnionSpecifier(StructOrUnionSpecifier structOrUnionSpecifier)
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
                VisitSpecifierQualifierListItem(specifierQualifierListItem);
            }

            _writer.Indent--;
            _writer.WriteLine("SpecifiersQualifiers");
            if (structDeclaration.Declarators is not null)
            {
                _writer.Indent++;
                foreach (var structDeclarator in structDeclaration.Declarators)
                {
                    VisitStructDeclarator(structDeclarator);
                }

                _writer.Indent--;
            }

            _writer.Indent--;
        }
    }

    protected override void VisitStructDeclarator(StructDeclarator structDeclarator)
    {
        _writer.WriteLine("StructDeclarator");
        _writer.Indent++;
        VisitDeclarator(structDeclarator.Declarator);
        _writer.Indent--;
    }

    protected override void VisitEnumSpecifier(EnumSpecifier enumSpecifier)
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
                    VisitExpression(enumDeclaration.Constant);
                    _writer.Indent--;
                }
            }
        }
    }

    protected override void VisitExpression(Expression expression)
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
                VisitExpression(parenExpression.Contents);
                _writer.Indent--;
                break;
            case SubscriptingExpression subscriptingExpression:
                _writer.WriteLine("ParenExpression");
                _writer.Indent++;
                _writer.WriteLine("Base");
                _writer.Indent++;
                VisitExpression(subscriptingExpression.Base);
                _writer.Indent--;
                _writer.WriteLine("Index");
                _writer.Indent++;
                VisitExpression(subscriptingExpression.Index);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case FunctionCallExpression functionCallExpression:
                _writer.WriteLine("FunctionCallExpression");
                _writer.Indent++;
                _writer.WriteLine("Function");
                _writer.Indent++;
                VisitExpression(functionCallExpression.Function);
                _writer.Indent--;
                _writer.WriteLine("Arguments");
                if (functionCallExpression.Arguments is not null)
                {
                    _writer.Indent++;
                    foreach (var argument in functionCallExpression.Arguments)
                    {
                        VisitExpression(argument);
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
                    VisitExpression(argument);
                }

                _writer.Indent--;
                _writer.Indent--;
                break;
            case MemberAccessExpression memberAccessExpression:
                _writer.WriteLine($"MemberAccessExpression {memberAccessExpression.Identifier.Identifier}");
                _writer.Indent++;
                _writer.WriteLine("Target");
                _writer.Indent++;
                VisitExpression(memberAccessExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case PointerMemberAccessExpression pointerMemberAccessExpression:
                _writer.WriteLine($"PointerMemberAccessExpression {pointerMemberAccessExpression.Identifier.Identifier}");
                _writer.Indent++;
                _writer.WriteLine("Target");
                _writer.Indent++;
                VisitExpression(pointerMemberAccessExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case PostfixIncrementDecrementExpression postfixIncrementDecrementExpression:
                _writer.WriteLine($"PostfixIncrementDecrementExpression {postfixIncrementDecrementExpression.PostfixOperator.Text}");
                _writer.Indent++;
                _writer.WriteLine("Target");
                _writer.Indent++;
                VisitExpression(postfixIncrementDecrementExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case PrefixIncrementDecrementExpression prefixIncrementDecrementExpression:
                _writer.WriteLine($"PrefixIncrementDecrementExpression {prefixIncrementDecrementExpression.PrefixOperator.Text}");
                _writer.Indent++;
                _writer.WriteLine("Target");
                _writer.Indent++;
                VisitExpression(prefixIncrementDecrementExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case UnaryOperatorExpression unaryOperatorExpression:
                _writer.WriteLine($"UnaryOperatorExpression {unaryOperatorExpression.Operator}");
                _writer.Indent++;
                _writer.WriteLine("Target");
                _writer.Indent++;
                VisitExpression(unaryOperatorExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case IndirectionExpression indirectionExpression:
                _writer.WriteLine("IndirectionExpression");
                _writer.Indent++;
                _writer.WriteLine("Target");
                _writer.Indent++;
                VisitExpression(indirectionExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case UnaryExpressionSizeOfOperatorExpression unaryExpressionSizeOfOperatorExpression:
                _writer.WriteLine("UnaryExpressionSizeOfOperatorExpression");
                _writer.Indent++;
                _writer.WriteLine("Target");
                _writer.Indent++;
                VisitExpression(unaryExpressionSizeOfOperatorExpression.TargetExpession);
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
                VisitExpression(castExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case BinaryOperatorExpression binaryOperatorExpression:
                _writer.WriteLine($"BinaryOperatorExpression {binaryOperatorExpression.Operator}");
                _writer.Indent++;
                _writer.WriteLine("Left");
                _writer.Indent++;
                VisitExpression(binaryOperatorExpression.Left);
                _writer.Indent--;
                _writer.WriteLine("Right");
                _writer.Indent++;
                VisitExpression(binaryOperatorExpression.Right);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case ConditionalExpression conditionalExpression:
                _writer.WriteLine("ConditionalExpression");
                _writer.Indent++;
                _writer.WriteLine("Condition");
                _writer.Indent++;
                VisitExpression(conditionalExpression.Condition);
                _writer.Indent--;
                _writer.WriteLine("TrueExpression");
                _writer.Indent++;
                VisitExpression(conditionalExpression.TrueExpression);
                _writer.Indent--;
                _writer.WriteLine("FalseExpression");
                _writer.Indent++;
                VisitExpression(conditionalExpression.FalseExpression);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case CommaExpression commaExpression:
                _writer.WriteLine("CommaExpression");
                _writer.Indent++;
                _writer.WriteLine("Left");
                _writer.Indent++;
                VisitExpression(commaExpression.Left);
                _writer.Indent--;
                _writer.WriteLine("Right");
                _writer.Indent++;
                VisitExpression(commaExpression.Right);
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
            VisitSpecifierQualifierListItem(specifierQualifierListItem);
        }

        _writer.Indent--;
        _writer.WriteLine("AbstractDeclarator");
        if (typeName.AbstractDeclarator is not null)
        {
            _writer.Indent++;
            VisitAbstractDeclarator(typeName.AbstractDeclarator);
            _writer.Indent--;
        }

        _writer.Indent--;
    }

    protected override void VisitStatement(Statement statement)
    {
        switch (statement)
        {
            case LabelStatement labelStatement:
                _writer.WriteLine($"LabelStatement {labelStatement.Identifier}");
                _writer.Indent++;
                VisitStatement(labelStatement.Body);
                _writer.Indent--;
                break;
            case CaseStatement caseStatement:
                _writer.WriteLine("CaseStatement");
                _writer.Indent++;
                if (caseStatement.Constant is not null)
                {
                    _writer.WriteLine("Constant");
                    _writer.Indent++;
                    VisitExpression(caseStatement.Constant);
                    _writer.Indent--;
                }

                _writer.WriteLine("Body");
                _writer.Indent++;
                VisitStatement(caseStatement.Body);
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
                    VisitExpression(expressionStatement.Expression);
                    _writer.Indent--;
                }

                _writer.Indent--;
                break;
            case IfElseStatement ifElseStatement:
                _writer.WriteLine("IfElseStatement");
                _writer.Indent++;
                _writer.WriteLine("Expression");
                _writer.Indent++;
                VisitExpression(ifElseStatement.Expression);
                _writer.Indent--;
                _writer.WriteLine("TrueBranch");
                _writer.Indent++;
                VisitStatement(ifElseStatement.TrueBranch);
                _writer.Indent--;
                if (ifElseStatement.FalseBranch is not null)
                {
                    _writer.WriteLine("FalseBranch");
                    _writer.Indent++;
                    VisitStatement(ifElseStatement.FalseBranch);
                    _writer.Indent--;
                }

                _writer.Indent--;
                break;
            case SwitchStatement switchStatement:
                _writer.WriteLine("SwitchStatement");
                _writer.Indent++;
                _writer.WriteLine("Expression");
                _writer.Indent++;
                VisitExpression(switchStatement.Expression);
                _writer.Indent--;
                _writer.WriteLine("Body");
                _writer.Indent++;
                VisitStatement(switchStatement.Body);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case WhileStatement whileStatement:
                _writer.WriteLine("WhileStatement");
                _writer.Indent++;
                _writer.WriteLine("TestExpression");
                _writer.Indent++;
                VisitExpression(whileStatement.TestExpression);
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
                VisitExpression(doWhileStatement.TestExpression);
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
                    VisitExpression(forStatement.InitExpression);
                    _writer.Indent--;
                }

                if (forStatement.TestExpression is not null)
                {
                    _writer.WriteLine("TestExpression");
                    _writer.Indent++;
                    VisitExpression(forStatement.TestExpression);
                    _writer.Indent--;
                }

                if (forStatement.UpdateExpression is not null)
                {
                    _writer.WriteLine("UpdateExpression");
                    _writer.Indent++;
                    VisitExpression(forStatement.UpdateExpression);
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
                VisitExpression(returnStatement.Expression);
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
