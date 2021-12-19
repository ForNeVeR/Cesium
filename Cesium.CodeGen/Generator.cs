using Cesium.Ast;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cesium.CodeGen;

public class Generator
{
    public static AssemblyDefinition GenerateAssembly(
        TranslationUnit translationUnit,
        AssemblyNameDefinition name,
        ModuleKind kind)
    {
        var assembly = AssemblyDefinition.CreateAssembly(name, "Primary", kind);
        var module = assembly.MainModule;
        var moduleType = module.GetType("<Module>");

        foreach (var declaration in translationUnit.Declarations)
        {
            var method = GenerateMethod(module, (FunctionDefinition)declaration);
            moduleType.Methods.Add(method);
            if (method.Name == "main")
            {
                var currentEntryPoint = assembly.EntryPoint;
                if (currentEntryPoint != null)
                    throw new Exception($"Cannot override entrypoint for assembly {assembly} by method {method}.");

                assembly.EntryPoint = method;
            }
        }

        return assembly;
    }

    private static MethodDefinition GenerateMethod(ModuleDefinition module, FunctionDefinition definition)
    {
        var def = new MethodDefinition(
            definition.Declarator.DirectDeclarator.Name,
            MethodAttributes.Public | MethodAttributes.Static,
            GetReturnType(module, definition));

        if (!definition.Statement.Block.IsEmpty)
            throw new Exception("Non-empty function bodies aren't supported, yet.");

        def.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        def.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        return def;
    }

    private static TypeReference GetReturnType(ModuleDefinition module, FunctionDefinition definition)
    {
        var typeSpecifier = definition.Specifiers.OfType<TypeSpecifier>().Single();
        return typeSpecifier.TypeName switch
        {
            "int" => module.TypeSystem.Int32,
            var unknown => throw new Exception($"Unknown type specifier: {unknown}")
        };
    }
}
