// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.Ast;

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
        if (enumDeclaration.Constant is not null)
        {
            _writer.Indent++;
            base.Visit(enumDeclaration.Constant);
            _writer.Indent--;
        }
    }

    protected override void Visit(Expression expression)
    {
        base.Visit(expression);
    }
    protected override void Visit(StringLiteralListExpression stringLiteralListExpression)
    {
        _writer.WriteLine($"StringLiteralListExpression {string.Join(", ", stringLiteralListExpression.ConstantList.Select(_ => _.Text))}");
        base.Visit(stringLiteralListExpression);
    }

    protected override void Visit(IdentifierExpression identifierExpression)
    {
        _writer.WriteLine($"IdentifierExpression {identifierExpression.Identifier}");
        base.Visit(identifierExpression);
    }

    protected override void Visit(ConstantLiteralExpression constantLiteralExpression)
    {
        _writer.WriteLine($"ConstantLiteralExpression {constantLiteralExpression.Constant.Text}");
        base.Visit(constantLiteralExpression);
    }

    protected override void Visit(ParenExpression expression)
    {
        Enter("ParenExpression");
        base.Visit(expression);
        Exit();
    }

    protected override void Visit(SubscriptingExpression expression)
    {
        Enter("SubscriptingExpression");
        Enter("Base");
        base.Visit(expression.Base);
        Exit();
        Enter("Index");
        base.Visit(expression.Index);
        Exit();
        Exit();
    }

    protected override void Visit(FunctionCallExpression expression)
    {
        Enter("FunctionCallExpression");
        Enter("Function");
        base.Visit(expression.Function);
        Exit();
        Enter("Arguments");
        if (expression.Arguments is not null)
        {
            foreach (var argument in expression.Arguments)
            {
                base.Visit(argument);
            }
        }
        Exit();
        Exit();
    }

    protected override void Visit(TypeCastOrNamedFunctionCallExpression expression)
    {
        Enter($"TypeCastOrNamedFunctionCallExpression {expression.TypeOrFunctionName}");
        Enter("Arguments");
        foreach (var argument in expression.Arguments)
        {
            base.Visit(argument);
        }
        Exit();
        Exit();
    }

    protected override void Visit(MemberAccessExpression expression)
    {
        Enter($"MemberAccessExpression {expression.Identifier.Identifier}");
        Enter("Target");
        base.Visit(expression.Target);
        Exit();
        Exit();
    }

    protected override void Visit(PointerMemberAccessExpression expression)
    {
        Enter($"PointerMemberAccessExpression {expression.Identifier.Identifier}");
        Enter("Target");
        base.Visit(expression.Target);
        Exit();
        Exit();
    }

    protected override void Visit(PostfixIncrementDecrementExpression expression)
    {
        Enter($"PostfixIncrementDecrementExpression {expression.PostfixOperator.Text}");
        Enter("Target");
        base.Visit(expression.Target);
        Exit();
        Exit();
    }

    protected override void Visit(CompoundLiteralExpression expression)
    {
        Enter("CompoundLiteralExpression"); //?
        base.Visit(expression);
        Exit();
    }

    protected override void Visit(PrefixIncrementDecrementExpression expression)
    {
        Enter($"PrefixIncrementDecrementExpression {expression.PrefixOperator.Text}");
        Enter("Target");
        base.Visit(expression.Target);
        Exit();
        Exit();
    }

    protected override void Visit(UnaryOperatorExpression expression)
    {
        Enter($"UnaryOperatorExpression {expression.Operator}");
        Enter("Target");
        base.Visit(expression.Target);
        Exit();
        Exit();
    }

    protected override void Visit(IndirectionExpression expression)
    {
        Enter("IndirectionExpression");
        Enter("Target");
        base.Visit(expression);
        Exit();
        Exit();
    }

    protected override void Visit(UnaryExpressionSizeOfOperatorExpression expression)
    {
        Enter("UnaryExpressionSizeOfOperatorExpression");
        Enter("Target");
        base.Visit(expression.TargetExpession);
        Exit();
        Exit();
    }

    protected override void Visit(TypeNameSizeOfOperatorExpression expression)
    {
        Enter("TypeNameSizeOfOperatorExpression");
        base.Visit(expression);
        Exit();
    }

    protected override void Visit(CastExpression expression)
    {
        Enter("CastExpression");
        Enter("TypeName");
        base.Visit(expression.TypeName);
        Exit();
        Enter("Target");
        base.Visit(expression.Target);
        Exit();
        Exit();
    }

    protected override void Visit(BinaryOperatorExpression expression)
    {
        Enter("BinaryOperatorExpression");
        Enter("Left");
        base.Visit(expression.Left);
        Exit();
        Enter("Right");
        base.Visit(expression.Right);
        Exit();
        Exit();
    }

    protected override void Visit(ConditionalExpression expression)
    {
        Enter("ConditionalExpression");
        Enter("Condition");
        base.Visit(expression.Condition);
        Exit();
        Enter("TrueExpression");
        base.Visit(expression.TrueExpression);
        Exit();
        Enter("FalseExpression");
        base.Visit(expression.FalseExpression);
        Exit();
        Exit();
    }

    protected override void Visit(CommaExpression expression)
    {
        Enter("CommaExpression");
        Enter("Left");
        base.Visit(expression.Left);
        Exit();
        Enter("Right");
        base.Visit(expression.Right);
        Exit();
        Exit();
    }

    protected override void Visit(AmbiguousBlockItem ambiguousBlockItem)
    {
        _writer.WriteLine($"AmbiguousBlockItem ({ambiguousBlockItem.Item1}, {ambiguousBlockItem.Item2})");
        base.Visit(ambiguousBlockItem);
    }

    protected override void Visit(TypeName typeName)
    {
        Enter("TypeName");
        base.Visit(typeName);
        Exit();
    }

    protected override void Visit(Statement statement)
    {
        base.Visit(statement);
    }

    protected override void Visit(LabelStatement statement)
    {
        Enter($"LabelStatement {statement.Identifier}");
        base.Visit(statement);
        Exit();
    }
    protected override void Visit(CaseStatement statement)
    {
        Enter("CaseStatement");
        if (statement.Constant is not null)
        {
            Enter("Constant");
            base.Visit(statement.Constant);
            Exit();
        }

        Enter("Body");
        base.Visit(statement.Body);
        Exit();
        Exit();
    }

    protected override void Visit(CompoundStatement statement)
    {
        _writer.WriteLine("CompoundStatement");
        base.Visit(statement);
    }

    protected override void Visit(ExpressionStatement statement)
    {
        Enter("ExpressionStatement");
        if (statement.Expression is not null)
        {
            Enter("Expression");
            base.Visit(statement.Expression);
            Exit();
        }
        Exit();
    }

    protected override void Visit(IfElseStatement statement)
    {
        Enter("IfElseStatement");
        Enter("Expression");
        base.Visit(statement.Expression);
        Exit();

        Enter("TrueBranch");
        base.Visit(statement.TrueBranch);
        Exit();

        if (statement.FalseBranch is not null)
        {
            Enter("FalseBranch");
            base.Visit(statement.FalseBranch);
            Exit();
        }

        Exit();
    }

    protected override void Visit(SwitchStatement statement)
    {
        Enter("SwitchStatement");
        Enter("Expression");
        base.Visit(statement.Expression);
        Exit();
        Enter("Body");
        base.Visit(statement.Body);
        Exit();
        Exit();
    }

    protected override void Visit(WhileStatement statement)
    {
        Enter("WhileStatement");
        Enter("TestExpression");
        base.Visit(statement.TestExpression);
        Exit();
        Enter("Body");
        base.Visit(statement.Body);
        Exit();
        Exit();
    }

    protected override void Visit(DoWhileStatement statement)
    {
        Enter("DoWhileStatement");
        Enter("TestExpression");
        base.Visit(statement.TestExpression);
        Exit();
        Enter("Body");
        Visit(statement.Body);
        Exit();
        Exit();
    }

    protected override void Visit(ForStatement statement)
    {
        Enter("ForStatement");

        if (statement.InitDeclaration is not null)
        {
            Enter("InitDeclaration");
            base.Visit(statement.InitDeclaration);
            Exit();
        }

        if (statement.InitExpression is not null)
        {
            Enter("InitExpression");
            base.Visit(statement.InitExpression);
            Exit();
        }

        if (statement.TestExpression is not null)
        {
            Enter("TestExpression");
            base.Visit(statement.TestExpression);
            Exit();
        }

        if (statement.UpdateExpression is not null)
        {
            Enter("UpdateExpression");
            base.Visit(statement.UpdateExpression);
            Exit();
        }

        Enter("Body");
        base.Visit(statement.Body);
        Exit();
        Exit();
    }

    protected override void Visit(GoToStatement statement)
    {
        _writer.WriteLine($"GoToStatement {statement.Identifier}");
        base.Visit(statement);
    }

    protected override void Visit(BreakStatement statement)
    {
        _writer.WriteLine("BreakStatement");
        base.Visit(statement);
    }

    protected override void Visit(ContinueStatement statement)
    {
        _writer.WriteLine("ContinueStatement");
        base.Visit(statement);
    }

    protected override void Visit(ReturnStatement statement)
    {
        Enter("ReturnStatement");
        Enter("Expression");
        base.Visit(statement.Expression);
        Exit();
        Exit();
    }
}
