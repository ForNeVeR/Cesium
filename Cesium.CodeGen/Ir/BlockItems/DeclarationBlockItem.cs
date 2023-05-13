using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir.Declarations;
using Cesium.CodeGen.Ir.Expressions;
using Cesium.CodeGen.Ir.Expressions.BinaryOperators;
using Cesium.CodeGen.Ir.Types;
using Cesium.Core;

namespace Cesium.CodeGen.Ir.BlockItems;

internal class DeclarationBlockItem : IBlockItem
{
    private readonly ScopedIdentifierDeclaration _declaration;

    internal DeclarationBlockItem(ScopedIdentifierDeclaration declaration)
    {
        _declaration = declaration;
    }

    public IBlockItem Lower(IDeclarationScope scope)
    {
        var (storageClass, items) = _declaration;
        var newItems = new List<InitializerPart>();

        foreach (var (declaration, initializer) in items)
        {
            var (type, identifier, cliImportMemberName) = declaration;

            // TODO[#91]: A place to register whether {type} is const or not.

            if (identifier == null)
                throw new CompilationException("An anonymous local declaration isn't supported.");

            if (cliImportMemberName != null)
                throw new CompilationException(
                    $"Local declaration with a CLI import member name {cliImportMemberName} isn't supported.");

            type = scope.ResolveType(type);
            scope.AddVariable(storageClass, identifier, type);

            var initializerExpression = initializer;
            if (initializerExpression != null)
            {
                var initializerType = initializerExpression.Lower(scope).GetExpressionType(scope);
                if (scope.CTypeSystem.IsConversionAvailable(initializerType, type)
                    && !initializerType.Equals(type))
                {
                    initializerExpression = new TypeCastExpression(type, initializerExpression);
                }
            }

            if (initializerExpression != null)
            {
                initializerExpression = new AssignmentExpression(new IdentifierExpression(identifier), BinaryOperator.Assign, initializerExpression);

                if (initializerExpression.GetExpressionType(scope) is not PrimitiveType { Kind: PrimitiveTypeKind.Void })
                    initializerExpression = new ConsumeExpression(initializerExpression);
            }

            IExpression? primaryInitializerExpression = null;
            if (type is InPlaceArrayType i)
            {
                primaryInitializerExpression = new ConsumeExpression(
                    new AssignmentExpression(new IdentifierExpression(identifier), BinaryOperator.Assign, new LocalAllocationExpression(i))
                );
            }

            var initializableDeclaration = new InitializerPart(primaryInitializerExpression?.Lower(scope), initializerExpression?.Lower(scope));
            newItems.Add(initializableDeclaration);
        }

        return new InitializationBlockItem(newItems);
    }

    public void EmitTo(IEmitScope scope)
    {
        throw new AssertException("Should be lowered");
    }

    public bool TryUnsafeSubstitute(IBlockItem original, IBlockItem replacement) => false;
}
