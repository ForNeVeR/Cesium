using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Types;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class DeclarationBlockItem : IBlockItem
{
    private readonly IScopedDeclarationInfo _declaration;
    private DeclarationBlockItem(IScopedDeclarationInfo declaration)
    {
        _declaration = declaration;
    }

    public DeclarationBlockItem(Declaration declaration)
        : this(IScopedDeclarationInfo.Of(declaration))
    {
    }

    public IBlockItem Lower()
    {
        switch (_declaration)
        {
            case ScopedIdentifierDeclaration declaration:
            {
                declaration.Deconstruct(out var items);
                return new DeclarationBlockItem(
                    new ScopedIdentifierDeclaration(
                        items.Select(d =>
                            {
                                var (itemDeclaration, initializer) = d;
                                return new InitializableDeclarationInfo(itemDeclaration, initializer?.Lower());
                            })
                            .ToList()));
            }
            case TypeDefDeclaration: return this;
            default: throw new NotSupportedException($"Unknown kind of declaration: {_declaration}.");
        }
    }


    public void EmitTo(IDeclarationScope scope)
    {
        switch (_declaration)
        {
            case ScopedIdentifierDeclaration declaration:
                EmitScopedIdentifier(scope, declaration);
                break;
            case TypeDefDeclaration declaration:
                EmitTypeDef(declaration);
                break;
            default:
                throw new NotSupportedException($"Unknown kind of declaration: {_declaration}.");
        }
    }

    private static void EmitScopedIdentifier(IDeclarationScope scope, ScopedIdentifierDeclaration scopedDeclaration)
    {
        scopedDeclaration.Deconstruct(out var declarations);
        foreach (var (declaration, initializer) in declarations)
        {
            var method = scope.Method;
            var (type, identifier, cliImportMemberName) = declaration;

            // TODO[#91]: A place to register whether {type} is const or not.

            if (identifier == null)
                throw new NotSupportedException("An anonymous local declaration isn't supported.");

            if (cliImportMemberName != null)
                throw new NotSupportedException(
                    $"Local declaration with a CLI import member name {cliImportMemberName} isn't supported.");

            var typeReference = type.Resolve(scope.Context);
            var variable = new VariableDefinition(typeReference);
            method.Body.Variables.Add(variable);
            scope.AddVariable(identifier, variable);

            switch (initializer)
            {
                case null when type is not StackArrayType:
                    return;
                case null when type is StackArrayType arrayType:
                    arrayType.EmitInitializer(scope);
                    break;
                default:
                    initializer?.EmitTo(scope);
                    break;
            }

            scope.StLoc(variable);
        }
    }

    private static void EmitTypeDef(TypeDefDeclaration declaration) =>
        throw new NotImplementedException($"typedef is not supported at block level, yet: {declaration}.");
}
