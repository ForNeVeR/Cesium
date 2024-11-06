using Cesium.Ast;
using Cesium.Core;

namespace Cesium.Compiler;

internal class AstDumper
{
    private readonly IndentedTextWriter _writer;

    public AstDumper(TextWriter writer)
    {
        _writer = new IndentedTextWriter(writer);
    }

    internal void Dump(TranslationUnit translationUnit)
    {
        _writer.WriteLine("TranslationUnitDecl");
        _writer.Indent++;
        foreach (var declaration in translationUnit.Declarations)
        {
            DumpExternalDeclaration(declaration);
        }

        _writer.Indent--;
    }

    private void DumpExternalDeclaration(ExternalDeclaration declaration)
    {
        switch(declaration)
        {
            case FunctionDefinition functionDefinition:
                DumpFunctionDefinition(functionDefinition);
                break;
            case SymbolDeclaration symbolDeclaration:
                DumpSymbolDeclaration(symbolDeclaration);
                break;
            case PInvokeDeclaration pInvokeDeclaration:
                DumpPInvokeDeclaration(pInvokeDeclaration);
                break;
            default:
                throw new AssertException($"Unknown external declaration of type {declaration.GetType()}.");
        }
    }

    private void DumpFunctionDefinition(FunctionDefinition functionDefinition)
    {
        _writer.WriteLine($"FunctionDefinition");
        _writer.Indent++;
        _writer.WriteLine($"Specifiers");
        _writer.Indent++;
        foreach (var declarationSpecifier in functionDefinition.Specifiers)
        {
            DumpIDeclarationSpecifier(declarationSpecifier);
        }

        _writer.Indent--;
        DumpDeclarator(functionDefinition.Declarator);
        if (functionDefinition.Declarations is not null)
        {
            _writer.Indent++;
            foreach (var declaration in functionDefinition.Declarations)
            {
                DumpDeclaration(declaration);
            }

            _writer.Indent--;
        }

        DumpStatement(functionDefinition.Statement);
        _writer.Indent--;
    }

    private void DumpPInvokeDeclaration(PInvokeDeclaration pInvokeDeclaration)
    {
        _writer.WriteLine($"PInvokeDecl {pInvokeDeclaration.Declaration} {(pInvokeDeclaration.Prefix is null ? "" : $"Prefix = {pInvokeDeclaration.Prefix}")}");
    }

    private void DumpSymbolDeclaration(SymbolDeclaration symbolDeclaration)
    {
        _writer.WriteLine($"SymbolDecl");
        _writer.Indent++;
        DumpDeclaration(symbolDeclaration.Declaration);
        _writer.Indent--;
    }

    private void DumpDeclaration(Declaration declaration)
    {
        _writer.WriteLine($"Decl");
        _writer.Indent++;
        foreach (var specifier in declaration.Specifiers)
        {
            DumpIDeclarationSpecifier(specifier);
        }

        _writer.Indent--;
        if (declaration.InitDeclarators is not null)
        {
            _writer.Indent++;
            foreach (var initDeclarator in declaration.InitDeclarators)
            {
                DumpInitDeclarator(initDeclarator);
            }

            _writer.Indent--;
        }
    }

    private void DumpInitDeclarator(InitDeclarator initDeclarator)
    {
        _writer.WriteLine($"InitDecl");
        _writer.Indent++;
        DumpDeclarator(initDeclarator.Declarator);
        if (initDeclarator.Initializer is not null)
        {
            DumpInitializer(initDeclarator.Initializer);
        }

        _writer.Indent--;
    }

    private void DumpDeclarator(Declarator declarator)
    {
        _writer.WriteLine($"Declarator");
        _writer.Indent++;
        if (declarator.Pointer is not null)
        {
            DumpPointer(declarator.Pointer);
        }

        DumpIDirectDeclarator(declarator.DirectDeclarator);
        _writer.Indent--;
    }

    private void DumpIDirectDeclarator(IDirectDeclarator directDeclarator)
    {
        switch (directDeclarator)
        {
            case IdentifierDirectDeclarator identifierDirectDeclarator:
                _writer.WriteLine($"IdentifierDirectDeclarator {identifierDirectDeclarator.Identifier}");
                break;
            case ArrayDirectDeclarator arrayDirectDeclarator:
                _writer.WriteLine($"ArrayDirectDeclarator");
                _writer.Indent++;
                DumpIDirectDeclarator(arrayDirectDeclarator.Base);
                if (arrayDirectDeclarator.TypeQualifiers is not null)
                {
                    foreach (var typeQualifier in arrayDirectDeclarator.TypeQualifiers)
                    {
                        DumpTypeQualifier(typeQualifier);
                    }
                }
                if (arrayDirectDeclarator.Size is not null)
                {
                    DumpExpression(arrayDirectDeclarator.Size);
                }

                _writer.Indent--;
                break;
            case ParameterListDirectDeclarator parameterListDirectDeclarator:
                _writer.WriteLine($"ParameterListDirectDeclarator");
                _writer.Indent++;
                DumpIDirectDeclarator(parameterListDirectDeclarator.Base);
                if (parameterListDirectDeclarator.Parameters is not null)
                {
                    foreach (var parameterDeclaration in parameterListDirectDeclarator.Parameters.Parameters)
                    {
                        DumpParameterDeclaration(parameterDeclaration);
                    }

                    if (parameterListDirectDeclarator.Parameters.HasEllipsis)
                    {
                        _writer.WriteLine($"HasEllipsis");
                    }
                }

                _writer.Indent--;
                break;
            case IdentifierListDirectDeclarator identifierListDirectDeclarator:
                _writer.WriteLine($"IdentifierListDirectDeclarator");
                _writer.Indent++;
                DumpIDirectDeclarator(identifierListDirectDeclarator.Base);
                if (identifierListDirectDeclarator.Identifiers is not null)
                {
                    _writer.WriteLine($"Identifiers {string.Join(", ", identifierListDirectDeclarator.Identifiers)}");
                }

                _writer.Indent--;
                break;
            case DeclaratorDirectDeclarator declaratorDirectDeclarator:
                _writer.WriteLine($"DeclaratorDirectDeclarator");
                _writer.Indent++;
                DumpDeclarator(declaratorDirectDeclarator.Declarator);
                _writer.Indent--;
                break;
            default:
                throw new AssertException($"Unknown direct declarator of type {directDeclarator.GetType()}.");
        }
    }

    private void DumpParameterDeclaration(ParameterDeclaration parameterDeclaration)
    {
        _writer.WriteLine($"ParameterDeclaration");
        _writer.Indent++;
        foreach (var declarationSpecifier in parameterDeclaration.Specifiers)
        {
            DumpIDeclarationSpecifier(declarationSpecifier);
        }
        if (parameterDeclaration.Declarator is not null)
        {
            DumpDeclarator(parameterDeclaration.Declarator);
        }
        if (parameterDeclaration.AbstractDeclarator is not null)
        {
            DumpAbstractDeclarator(parameterDeclaration.AbstractDeclarator);
        }

        _writer.Indent--;
    }

    private void DumpAbstractDeclarator(AbstractDeclarator abstractDeclarator)
    {
        _writer.WriteLine($"ParameterDeclaration");
        _writer.Indent++;
        if (abstractDeclarator.Pointer is not null)
        {
            DumpPointer(abstractDeclarator.Pointer);
        }
        if (abstractDeclarator.DirectAbstractDeclarator is not null)
        {
            DumpIDirectAbstractDeclarator(abstractDeclarator.DirectAbstractDeclarator);
        }

        _writer.Indent--;
    }

    private void DumpIDirectAbstractDeclarator(IDirectAbstractDeclarator directAbstractDeclarator)
    {
        switch (directAbstractDeclarator)
        {
            case SimpleDirectAbstractDeclarator simpleDirectAbstractDeclarator:
                _writer.WriteLine($"SimpleDirectAbstractDeclarator");
                _writer.Indent++;
                DumpAbstractDeclarator(simpleDirectAbstractDeclarator.Declarator);
                _writer.Indent--;
                break;
            case ArrayDirectAbstractDeclarator arrayDirectDeclarator:
                _writer.WriteLine($"ArrayDirectDeclarator");
                _writer.Indent++;
                if (arrayDirectDeclarator.Base is not null)
                {
                    DumpIDirectAbstractDeclarator(arrayDirectDeclarator.Base);
                }

                if (arrayDirectDeclarator.TypeQualifiers is not null)
                {
                    foreach (var typeQualifier in arrayDirectDeclarator.TypeQualifiers)
                    {
                        DumpTypeQualifier(typeQualifier);
                    }
                }
                if (arrayDirectDeclarator.Size is not null)
                {
                    DumpExpression(arrayDirectDeclarator.Size);
                }

                _writer.Indent--;
                break;
            default:
                throw new AssertException($"Unknown direct abstract declarator of type {directAbstractDeclarator.GetType()}.");
        }
    }

    private void DumpPointer(Pointer pointer)
    {
        _writer.WriteLine($"Pointer");
        _writer.Indent--;
        if (pointer.TypeQualifiers is not null)
        {
            _writer.Indent++;
            foreach (var typeQualifier in pointer.TypeQualifiers)
            {
                DumpTypeQualifier(typeQualifier);
            }

            _writer.Indent--;
        }
        if (pointer.ChildPointer is not null)
        {
            DumpPointer(pointer.ChildPointer);
        }
        _writer.Indent++;
    }

    private void DumpInitializer(Initializer initializer)
    {
        switch (initializer)
        {
            case AssignmentInitializer assignmentInitializer:
                _writer.WriteLine($"AssignmentInitializer");
                _writer.Indent++;
                if (initializer.Designation is not null)
                {
                    DumpDesignation(initializer.Designation);
                }

                DumpExpression(assignmentInitializer.Expression);
                _writer.Indent--;
                break;
            case ArrayInitializer arrayInitializer:
                _writer.WriteLine($"ArrayInitializer");
                _writer.Indent++;
                if (initializer.Designation is not null)
                {
                    DumpDesignation(initializer.Designation);
                }

                foreach (var arraySubInitializer in arrayInitializer.Initializers)
                {
                    DumpInitializer(arraySubInitializer);
                }

                _writer.Indent--;
                break;
            default:
                throw new AssertException($"Unknown initializer of type {initializer.GetType()}.");
        }
    }

    private void DumpDesignation(Designation designation)
    {
        _writer.WriteLine($"Designation");
        _writer.Indent++;
        foreach (var designator in designation.Designators)
        {
            DumpDesignator(designator);
        }

        _writer.Indent--;
    }

    private void DumpDesignator(Designator designator)
    {
        switch (designator)
        {
            case BracketsDesignator bracketsDesignator:
                _writer.WriteLine($"BracketsDesignator");
                _writer.Indent++;
                DumpExpression(bracketsDesignator.Expression);
                _writer.Indent--;
                break;
            case IdentifierDesignator identifierDesignator:
                _writer.WriteLine($"IdentifierDesignator .{identifierDesignator.FieldName}");
                break;
            default:
                throw new AssertException($"Unknown designator of type {designator.GetType()}.");
        }
    }

    private void DumpIDeclarationSpecifier(IDeclarationSpecifier specifier)
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
                DumpISpecifierQualifierListItem(specifierQualifierListItem);
                break;
            default:
                throw new AssertException($"Unknown declaration specifier of type {specifier.GetType()}.");
        }
    }

    private void DumpITypeSpecifier(ITypeSpecifier typeSpecifier)
    {
        switch (typeSpecifier)
        {
            case SimpleTypeSpecifier simpleTypeSpecifier:
                _writer.WriteLine($"SimpleTypeSpecifier {simpleTypeSpecifier.TypeName}");
                break;
            case StructOrUnionSpecifier structOrUnionSpecifier:
                DumpStructOrUnionSpecifier(structOrUnionSpecifier);
                break;
            case EnumSpecifier enumSpecifier:
                DumpEnumSpecifier(enumSpecifier);
                break;
            case NamedTypeSpecifier namedTypeSpecifier:
                _writer.WriteLine($"NamedTypeSpecifier {namedTypeSpecifier.TypeDefName}");
                break;
            default:
                throw new AssertException($"Unknown type specifier of type {typeSpecifier.GetType()}.");
        }
    }

    private void DumpStructOrUnionSpecifier(StructOrUnionSpecifier structOrUnionSpecifier)
    {
        _writer.WriteLine($"StructOrUnionSpecifier {structOrUnionSpecifier.TypeKind} {structOrUnionSpecifier.Identifier}");
        foreach (var structDeclaration in structOrUnionSpecifier.StructDeclarations)
        {
            _writer.WriteLine($"StructDeclaration");
            _writer.Indent++;
            _writer.WriteLine($"SpecifiersQualifiers");
            _writer.Indent++;
            foreach (var specifierQualifierListItem in structDeclaration.SpecifiersQualifiers)
            {
                DumpISpecifierQualifierListItem(specifierQualifierListItem);
            }

            _writer.Indent--;
            _writer.WriteLine($"SpecifiersQualifiers");
            if (structDeclaration.Declarators is not null)
            {
                _writer.Indent++;
                foreach (var structDeclarator in structDeclaration.Declarators)
                {
                    DumpStructDeclarator(structDeclarator);
                }

                _writer.Indent--;
            }

            _writer.Indent--;
        }
    }

    private void DumpStructDeclarator(StructDeclarator structDeclarator)
    {
        _writer.WriteLine($"StructDeclarator");
        _writer.Indent++;
        DumpDeclarator(structDeclarator.Declarator);
        _writer.Indent--;
    }

    private void DumpISpecifierQualifierListItem(ISpecifierQualifierListItem specifierQualifierListItem)
    {
        switch (specifierQualifierListItem)
        {
            case TypeQualifier typeQualifier:
                DumpTypeQualifier(typeQualifier);
                break;
            case ITypeSpecifier typeSpecifier:
                DumpITypeSpecifier(typeSpecifier);
                break;
            default:
                throw new AssertException($"Unknown specifier qualified list item of type {specifierQualifierListItem.GetType()}.");
        }
    }

    private void DumpTypeQualifier(TypeQualifier typeQualifier)
    {
        _writer.WriteLine($"TypeQualifier {typeQualifier.Name}");
    }

    private void DumpEnumSpecifier(EnumSpecifier enumSpecifier)
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
                    DumpExpression(enumDeclaration.Constant);
                    _writer.Indent--;
                }
            }
        }
    }

    private void DumpExpression(Expression expression)
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
                _writer.WriteLine($"ParenExpression");
                _writer.Indent++;
                DumpExpression(parenExpression.Contents);
                _writer.Indent--;
                break;
            case SubscriptingExpression subscriptingExpression:
                _writer.WriteLine($"ParenExpression");
                _writer.Indent++;
                _writer.WriteLine($"Base");
                _writer.Indent++;
                DumpExpression(subscriptingExpression.Base);
                _writer.Indent--;
                _writer.WriteLine($"Index");
                _writer.Indent++;
                DumpExpression(subscriptingExpression.Index);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case FunctionCallExpression functionCallExpression:
                _writer.WriteLine($"FunctionCallExpression");
                _writer.Indent++;
                _writer.WriteLine($"Function");
                _writer.Indent++;
                DumpExpression(functionCallExpression.Function);
                _writer.Indent--;
                _writer.WriteLine($"Arguments");
                if (functionCallExpression.Arguments is not null)
                {
                    _writer.Indent++;
                    foreach (var argument in functionCallExpression.Arguments)
                    {
                        DumpExpression(argument);
                    }

                    _writer.Indent--;
                }
                _writer.Indent--;
                break;
            case TypeCastOrNamedFunctionCallExpression typeCastOrNamedFunctionCallExpression:
                _writer.WriteLine($"TypeCastOrNamedFunctionCallExpression {typeCastOrNamedFunctionCallExpression.TypeOrFunctionName}");
                _writer.Indent++;
                _writer.WriteLine($"Arguments");
                _writer.Indent++;
                foreach (var argument in typeCastOrNamedFunctionCallExpression.Arguments)
                {
                    DumpExpression(argument);
                }

                _writer.Indent--;
                _writer.Indent--;
                break;
            case MemberAccessExpression memberAccessExpression:
                _writer.WriteLine($"MemberAccessExpression {memberAccessExpression.Identifier.Identifier}");
                _writer.Indent++;
                _writer.WriteLine($"Target");
                _writer.Indent++;
                DumpExpression(memberAccessExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case PointerMemberAccessExpression pointerMemberAccessExpression:
                _writer.WriteLine($"PointerMemberAccessExpression {pointerMemberAccessExpression.Identifier.Identifier}");
                _writer.Indent++;
                _writer.WriteLine($"Target");
                _writer.Indent++;
                DumpExpression(pointerMemberAccessExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case PostfixIncrementDecrementExpression postfixIncrementDecrementExpression:
                _writer.WriteLine($"PostfixIncrementDecrementExpression {postfixIncrementDecrementExpression.PostfixOperator.Text}");
                _writer.Indent++;
                _writer.WriteLine($"Target");
                _writer.Indent++;
                DumpExpression(postfixIncrementDecrementExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case PrefixIncrementDecrementExpression prefixIncrementDecrementExpression:
                _writer.WriteLine($"PrefixIncrementDecrementExpression {prefixIncrementDecrementExpression.PrefixOperator.Text}");
                _writer.Indent++;
                _writer.WriteLine($"Target");
                _writer.Indent++;
                DumpExpression(prefixIncrementDecrementExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case UnaryOperatorExpression unaryOperatorExpression:
                _writer.WriteLine($"UnaryOperatorExpression {unaryOperatorExpression.Operator}");
                _writer.Indent++;
                _writer.WriteLine($"Target");
                _writer.Indent++;
                DumpExpression(unaryOperatorExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case IndirectionExpression indirectionExpression:
                _writer.WriteLine($"IndirectionExpression");
                _writer.Indent++;
                _writer.WriteLine($"Target");
                _writer.Indent++;
                DumpExpression(indirectionExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case UnaryExpressionSizeOfOperatorExpression unaryExpressionSizeOfOperatorExpression:
                _writer.WriteLine($"UnaryExpressionSizeOfOperatorExpression");
                _writer.Indent++;
                _writer.WriteLine($"Target");
                _writer.Indent++;
                DumpExpression(unaryExpressionSizeOfOperatorExpression.TargetExpession);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case TypeNameSizeOfOperatorExpression typeNameSizeOfOperatorExpression:
                _writer.WriteLine($"TypeNameSizeOfOperatorExpression");
                _writer.Indent++;
                DumpTypeName(typeNameSizeOfOperatorExpression.TypeName);
                _writer.Indent--;
                break;
            case CastExpression castExpression:
                _writer.WriteLine($"UnaryExpressionSizeOfOperatorExpression");
                _writer.Indent++;
                _writer.WriteLine($"TypeName");
                _writer.Indent++;
                DumpTypeName(castExpression.TypeName);
                _writer.Indent--;
                _writer.WriteLine($"Target");
                _writer.Indent++;
                DumpExpression(castExpression.Target);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case BinaryOperatorExpression binaryOperatorExpression:
                _writer.WriteLine($"BinaryOperatorExpression {binaryOperatorExpression.Operator}");
                _writer.Indent++;
                _writer.WriteLine($"Left");
                _writer.Indent++;
                DumpExpression(binaryOperatorExpression.Left);
                _writer.Indent--;
                _writer.WriteLine($"Right");
                _writer.Indent++;
                DumpExpression(binaryOperatorExpression.Right);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case ConditionalExpression conditionalExpression:
                _writer.WriteLine($"ConditionalExpression");
                _writer.Indent++;
                _writer.WriteLine($"Condition");
                _writer.Indent++;
                DumpExpression(conditionalExpression.Condition);
                _writer.Indent--;
                _writer.WriteLine($"TrueExpression");
                _writer.Indent++;
                DumpExpression(conditionalExpression.TrueExpression);
                _writer.Indent--;
                _writer.WriteLine($"FalseExpression");
                _writer.Indent++;
                DumpExpression(conditionalExpression.FalseExpression);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case CommaExpression commaExpression:
                _writer.WriteLine($"CommaExpression");
                _writer.Indent++;
                _writer.WriteLine($"Left");
                _writer.Indent++;
                DumpExpression(commaExpression.Left);
                _writer.Indent--;
                _writer.WriteLine($"Right");
                _writer.Indent++;
                DumpExpression(commaExpression.Right);
                _writer.Indent--;
                _writer.Indent--;
                break;
            default:
                throw new AssertException($"Unknown expression of type {expression.GetType()}.");
        }
    }

    private void DumpTypeName(TypeName typeName)
    {
        _writer.WriteLine($"TypeName");
        _writer.Indent++;
        _writer.WriteLine($"SpecifierQualifierList");
        _writer.Indent++;
        foreach (var specifierQualifierListItem in typeName.SpecifierQualifierList)
        {
            DumpISpecifierQualifierListItem(specifierQualifierListItem);
        }

        _writer.Indent--;
        _writer.WriteLine($"AbstractDeclarator");
        if (typeName.AbstractDeclarator is not null)
        {
            _writer.Indent++;
            DumpAbstractDeclarator(typeName.AbstractDeclarator);
            _writer.Indent--;
        }

        _writer.Indent--;
    }
    private void DumpStatement(IBlockItem statement)
    {
        switch (statement)
        {
            case LabelStatement labelStatement:
                _writer.WriteLine($"LabelStatement {labelStatement.Identifier}");
                _writer.Indent++;
                DumpStatement(labelStatement.Body);
                _writer.Indent--;
                break;
            case CaseStatement caseStatement:
                _writer.WriteLine($"CaseStatement");
                _writer.Indent++;
                if (caseStatement.Constant is not null)
                {
                    _writer.WriteLine($"Constant");
                    _writer.Indent++;
                    DumpExpression(caseStatement.Constant);
                    _writer.Indent--;
                }

                _writer.WriteLine($"Body");
                _writer.Indent++;
                DumpStatement(caseStatement.Body);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case CompoundStatement compoundStatement:
                _writer.WriteLine($"CompoundStatement");
                foreach (var subStatement in compoundStatement.Block)
                {
                    DumpStatement(subStatement);
                }
                break;
            case ExpressionStatement expressionStatement:
                _writer.WriteLine($"ExpressionStatement");
                _writer.Indent++;
                if (expressionStatement.Expression is not null)
                {
                    _writer.WriteLine($"Expression");
                    _writer.Indent++;
                    DumpExpression(expressionStatement.Expression);
                    _writer.Indent--;
                }

                _writer.Indent--;
                break;
            case AmbiguousBlockItem ambiguousBlockItem:
                _writer.WriteLine($"AmbiguousBlockItem ({ambiguousBlockItem.Item1}, {ambiguousBlockItem.Item2})");
                break;
            case IfElseStatement ifElseStatement:
                _writer.WriteLine($"IfElseStatement");
                _writer.Indent++;
                _writer.WriteLine($"Expression");
                _writer.Indent++;
                DumpExpression(ifElseStatement.Expression);
                _writer.Indent--;
                _writer.WriteLine($"TrueBranch");
                _writer.Indent++;
                DumpStatement(ifElseStatement.TrueBranch);
                _writer.Indent--;
                if (ifElseStatement.FalseBranch is not null)
                {
                    _writer.WriteLine($"FalseBranch");
                    _writer.Indent++;
                    DumpStatement(ifElseStatement.FalseBranch);
                    _writer.Indent--;
                }

                _writer.Indent--;
                break;
            case SwitchStatement switchStatement:
                _writer.WriteLine($"SwitchStatement");
                _writer.Indent++;
                _writer.WriteLine($"Expression");
                _writer.Indent++;
                DumpExpression(switchStatement.Expression);
                _writer.Indent--;
                _writer.WriteLine($"Body");
                _writer.Indent++;
                DumpStatement(switchStatement.Body);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case WhileStatement whileStatement:
                _writer.WriteLine($"WhileStatement");
                _writer.Indent++;
                _writer.WriteLine($"TestExpression");
                _writer.Indent++;
                DumpExpression(whileStatement.TestExpression);
                _writer.Indent--;
                _writer.WriteLine($"Body");
                _writer.Indent++;
                DumpStatement(whileStatement.Body);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case DoWhileStatement doWhileStatement:
                _writer.WriteLine($"DoWhileStatement");
                _writer.Indent++;
                _writer.WriteLine($"TestExpression");
                _writer.Indent++;
                DumpExpression(doWhileStatement.TestExpression);
                _writer.Indent--;
                _writer.WriteLine($"Body");
                _writer.Indent++;
                DumpStatement(doWhileStatement.Body);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case ForStatement forStatement:
                _writer.WriteLine($"ForStatement");
                _writer.Indent++;

                if (forStatement.InitDeclaration is not null)
                {
                    _writer.WriteLine($"InitDeclaration");
                    _writer.Indent++;
                    DumpStatement(forStatement.InitDeclaration);
                    _writer.Indent--;
                }

                if (forStatement.InitExpression is not null)
                {
                    _writer.WriteLine($"InitExpression");
                    _writer.Indent++;
                    DumpExpression(forStatement.InitExpression);
                    _writer.Indent--;
                }

                if (forStatement.TestExpression is not null)
                {
                    _writer.WriteLine($"TestExpression");
                    _writer.Indent++;
                    DumpExpression(forStatement.TestExpression);
                    _writer.Indent--;
                }

                if (forStatement.UpdateExpression is not null)
                {
                    _writer.WriteLine($"UpdateExpression");
                    _writer.Indent++;
                    DumpExpression(forStatement.UpdateExpression);
                    _writer.Indent--;
                }

                _writer.WriteLine($"Body");
                _writer.Indent++;
                DumpStatement(forStatement.Body);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case GoToStatement goToStatement:
                _writer.WriteLine($"GoToStatement {goToStatement.Identifier}");
                break;
            case BreakStatement:
                _writer.WriteLine($"BreakStatement");
                break;
            case ContinueStatement:
                _writer.WriteLine($"ContinueStatement");
                break;
            case ReturnStatement returnStatement:
                _writer.WriteLine($"ReturnStatement");
                _writer.Indent++;
                _writer.WriteLine($"Expression");
                _writer.Indent++;
                DumpExpression(returnStatement.Expression);
                _writer.Indent--;
                _writer.Indent--;
                break;
            case Declaration declaration:
                DumpDeclaration(declaration);
                break;
            default:
                throw new AssertException($"Unknown statement of type {statement.GetType()}.");
        }
    }

}
