using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Types;

namespace Cesium.CodeGen.Ir.TopLevel;

internal class FunctionDefinition : ITopLevelNode
{
    private readonly IType _returnType;
    private readonly object _statement;

    public FunctionDefinition(Ast.FunctionDefinition ast)
    {
        var (specifiers, declarator, declarations, astStatement) = ast;
        var (pointer, directDeclarator) = declarator;
        if (pointer != null)
            throw new NotImplementedException(
                $"Function with pointer in declaration not supported, yet: {declarator}.");

        (_returnType, var isConstReturn) = DeclarationInfo.Of(specifiers, directDeclarator);
        if (isConstReturn)
            throw new NotImplementedException(
                $"Functions with const return type aren't supported, yet: {string.Join(", ", specifiers)}.");

        throw new NotImplementedException($"Direct declarator in function not supported: {directDeclarator}.");
        if (declarations?.IsEmpty == false)
            throw new NotImplementedException(
                $"Non-empty declaration list for a function is not yet supported: {string.Join(", ", declarations)}.");
        _statement = astStatement.ToIntermediate();
    }

    public void EmitTo(TranslationUnitContext context)
    {
        throw new NotImplementedException();
        // Functions.EmitFunction(context, (Ast.FunctionDefinition) );
    }
}
