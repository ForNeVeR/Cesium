using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.CodeGen.Ir;
using Mono.Cecil.Cil;
using static Cesium.CodeGen.Generators.Expressions;

namespace Cesium.CodeGen.Generators;

public static class Declarations
{
    public static void EmitSymbol(TranslationUnitContext context, SymbolDeclaration symbolDeclaration)
    {
        var declaration = symbolDeclaration.Declaration;
        var cliImportSpecifier = declaration.Specifiers.OfType<CliImportSpecifier>().Single();
        if (declaration.InitDeclarators == null)
            throw new Exception($"Declaration without init declarations: {declaration}.");

        var method = context.MethodLookup(cliImportSpecifier);
        if (method == null) throw new Exception($"Cannot find CLI import member {cliImportSpecifier.MemberName}.");

        var declarator = declaration.InitDeclarators.Value.Single().Declarator;
        if (declarator.Pointer != null)
            throw new NotImplementedException($"Pointer at {declarator} not supported yet.");

        // TODO: Verify correct signature.
        context.Functions.Add(declarator.DirectDeclarator.GetIdentifier(), method);
    }

    public static void EmitLocalDeclaration(FunctionScope scope, Declaration declaration)
    {
        var method = scope.Method;

        if (declaration.InitDeclarators == null)
            throw new Exception($"Declaration has InitDeclarators == null: {declaration}.");

        var initDeclarator = declaration.InitDeclarators.Value.Single();
        var declarator = initDeclarator.Declarator;
        var name = declarator.DirectDeclarator.GetIdentifier();

        var typeReference = DeclarationInfo.Of(declaration.Specifiers, declarator.DirectDeclarator)
            .Type
            .Resolve(scope.Module.TypeSystem);
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
