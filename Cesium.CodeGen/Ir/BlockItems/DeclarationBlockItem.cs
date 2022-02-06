using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class DeclarationBlockItem : IBlockItem
{
    private readonly IList<InitializableDeclarationInfo> _declarations;
    private DeclarationBlockItem(IList<InitializableDeclarationInfo> declarations)
    {
        _declarations = declarations;
    }

    public DeclarationBlockItem(Declaration declaration)
        : this(InitializableDeclarationInfo.Of(declaration).ToList())
    {
    }

    public IBlockItem Lower() =>
        new DeclarationBlockItem(
            _declarations
                .Select(d =>
                {
                    var (declaration, initializer) = d;
                    return new InitializableDeclarationInfo(declaration, initializer?.Lower());
                })
                .ToList());

    public void EmitTo(FunctionScope scope)
    {
        foreach (var (declaration, initializer) in _declarations)
        {
            var method = scope.Method;
            var (type, isConst, identifier, parametersInfo, cliImportMemberName) = declaration;

            // TODO[#91]: A place to register {isConst}.

            if (identifier == null)
                throw new NotSupportedException("An anonymous local declaration isn't supported.");

            if (parametersInfo != null)
                throw new NotImplementedException(
                    $"A local declaration of function {identifier} isn't supported, yet.");

            if (cliImportMemberName != null)
                throw new NotSupportedException(
                    $"Local declaration with a CLI import member name {cliImportMemberName} isn't supported.");

            var typeReference = type.Resolve(scope.TypeSystem);
            var variable = new VariableDefinition(typeReference);
            method.Body.Variables.Add(variable);
            scope.Variables.Add(identifier, variable);

            if (initializer == null) return;

            initializer.EmitTo(scope);
            scope.StLoc(variable);
        }
    }
}
