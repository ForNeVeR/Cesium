using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen.Generators;

public static class Declarations // TODO[F]: Delete this class.
{
    public static void EmitLocalDeclaration(FunctionScope scope, Declaration declaration)
    {
        var method = scope.Method;

        if (declaration.InitDeclarators == null)
            throw new Exception($"Declaration has InitDeclarators == null: {declaration}.");

        var initDeclarator = declaration.InitDeclarators.Value.Single();
        var declarator = initDeclarator.Declarator;
        var name = declarator.DirectDeclarator.GetIdentifier();

        var typeReference = DeclarationInfo.Of(declaration.Specifiers, declarator)
            .Type
            .Resolve(scope.Module.TypeSystem);
        var variable = new VariableDefinition(typeReference);
        method.Body.Variables.Add(variable);
        scope.Variables.Add(name, variable);

        var initializer = initDeclarator.Initializer;
        if (initializer == null) return;

        var expression = ((AssignmentInitializer)initializer).Expression.ToIntermediate().Lower();
        expression.EmitTo(scope);
        scope.StLoc(variable);
    }
}
