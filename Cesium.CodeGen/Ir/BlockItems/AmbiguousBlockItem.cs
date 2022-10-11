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
    private readonly string _item1;
    private readonly string _item2;

    public AmbiguousBlockItem(Ast.AmbiguousBlockItem item)
    {
        (_item1, _item2) = item;
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        // Check if this can be a valid variable declaration:
        var isValidVariableDeclaration = scope.GetVariable(_item1) != null;

        // Check if this can be a function call:
        var function = scope.GetFunctionInfo(_item1);
        var isValidFunctionCall = function != null;
        if (!isValidVariableDeclaration && !isValidFunctionCall)
            throw new CompilationException(
                $"{_item1}({_item2}) is supposed to be either a variable declaration or a function call," +
                " but wasn't resolved to be either.");
        else if (isValidVariableDeclaration && isValidFunctionCall)
            throw new CompilationException(
                $"{_item1}({_item2}) is supposed to be either a variable declaration or a function call," +
                $" but it's ambiguous which it is, since both a function and a type of name {_item1} exist.");

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

        var functionNameToken = CreateFakeToken(_item1);
        var argumentToken = CreateFakeToken(_item2);

        var functionCallExpression = new Expressions.FunctionCallExpression(new FunctionCallExpression(
            new ConstantExpression(functionNameToken),
            ImmutableArray.Create<Expression>(new ConstantExpression(argumentToken))
        ));
        var realNode = new ExpressionStatement(functionCallExpression);
        return realNode.Lower(scope);
    }
}
