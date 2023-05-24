using System.Collections.Immutable;
using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.Core;
using Yoakke.SynKit.C.Syntax;
using Range = Yoakke.SynKit.Text.Range;

namespace Cesium.CodeGen.Ir.BlockItems;

/// <summary>
/// This is a special block item which was constructed in an ambiguous context: it is either a declaration or a function
/// call, depending on the context.
///
/// It defines an AST of form <code>item1(item2);</code>, where item1 is either a function name or a type, and item2 is
/// either a variable name or an argument name.
/// </summary>
internal class AmbiguousBlockItem : IBlockItem
{
    public string Item1 { get; }
    public string Item2 { get; }

    public AmbiguousBlockItem(Ast.AmbiguousBlockItem item)
    {
        (Item1, Item2) = item;
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        // Check if this can be a valid variable declaration:
        var isValidVariableDeclaration = scope.GetVariable(Item1) != null;

        // Check if this can be a function call:
        var function = scope.GetFunctionInfo(Item1);
        var isValidFunctionCall = function != null;
        if (!isValidVariableDeclaration && !isValidFunctionCall)
            throw new CompilationException(
                $"{Item1}({Item2}) is supposed to be either a variable declaration or a function call," +
                " but wasn't resolved to be either.");
        else if (isValidVariableDeclaration && isValidFunctionCall)
            throw new CompilationException(
                $"{Item1}({Item2}) is supposed to be either a variable declaration or a function call," +
                $" but it's ambiguous which it is, since both a function and a type of name {Item1} exist.");

        if (isValidFunctionCall)
        {
            return CreateFuctionCallStatement(scope);

        }

        return this;
    }

    public void EmitTo(IEmitScope scope)
    {
        throw new WipException(213, "Ambiguous variable declarations aren't supported, yet.");
    }

    private IBlockItem CreateFuctionCallStatement(IDeclarationScope scope)
    {
        CToken CreateFakeToken(string id) => new(new Range(), id, new Range(), id, CTokenType.Identifier);

        var functionNameToken = CreateFakeToken(Item1);
        var argumentToken = CreateFakeToken(Item2);

        var functionCallExpression = new Expressions.FunctionCallExpression(new FunctionCallExpression(
            new ConstantLiteralExpression(functionNameToken),
            ImmutableArray.Create<Expression>(new ConstantLiteralExpression(argumentToken))
        ));
        var realNode = new ExpressionStatement(functionCallExpression);
        return realNode.Lower(scope);
    }
}
