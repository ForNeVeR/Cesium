﻿using Cesium.Ast;
using Mono.Cecil;
using static Cesium.CodeGen.Generators.Functions;

namespace Cesium.CodeGen.Generators;

public static class Assemblies
{
    public static AssemblyDefinition Generate(
        TranslationUnit translationUnit,
        AssemblyNameDefinition name,
        ModuleKind kind,
        TargetRuntimeDescriptor? targetRuntime)
    {
        var assembly = AssemblyDefinition.CreateAssembly(name, "Primary", kind);
        var module = assembly.MainModule;
        var moduleType = module.GetType("<Module>");

        targetRuntime ??= TargetRuntimeDescriptor.Net60;
        assembly.CustomAttributes.Add(targetRuntime.GetTargetFrameworkAttribute(module));
        module.AssemblyReferences.Add(targetRuntime.GetSystemAssemblyReference());

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
        var method = new MethodDefinition(
            definition.Declarator.DirectDeclarator.Name,
            MethodAttributes.Public | MethodAttributes.Static,
            GetReturnType(module, definition));

        if (definition.Declarator.DirectDeclarator.Name == "main")
            EmitMainFunction(method, definition);
        else
            EmitFunction(method, definition);

        return method;
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
