using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Mono.Cecil.Cil;
using static Cesium.CodeGen.Generators.Expressions;

namespace Cesium.CodeGen.Generators;

public static class Declarations
{
    public static void EmitDeclaration(FunctionScope scope, Declaration declaration)
    {
        var method = scope.Method;

        if (declaration.InitDeclarators == null)
            throw new Exception($"Declaration has InitDeclarators == null: {declaration}.");

        var initDeclarator = declaration.InitDeclarators.Value.Single();
        var declarator = initDeclarator.Declarator;
        var name = declarator.DirectDeclarator.Name;
        if (declarator.Pointer != null)
            throw new Exception($"Pointer types aren't supported, yet: {name}.");

        var typeSpecifier = declaration.Specifiers.OfType<TypeSpecifier>().Single();
        var typeReference = typeSpecifier.GetTypeReference(scope.Module);

        var variable = new VariableDefinition(typeReference);
        method.Body.Variables.Add(variable);
        scope.Variables.Add(name, variable);

        var initializer = initDeclarator.Initializer;
        if (initializer == null) return;

        var expression = ((AssignmentInitializer)initializer).Expression;
        EmitExpression(scope, expression);
        scope.StLoc(variable);
    }
}
