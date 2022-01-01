using System.Reflection;
using Cesium.Ast;
using Cesium.CodeGen.Contexts;
using Mono.Cecil;
using static Cesium.CodeGen.Generators.Declarations;
using static Cesium.CodeGen.Generators.Functions;

namespace Cesium.CodeGen.Generators;

public static class Assemblies
{
    public static AssemblyDefinition Generate(
        TranslationUnit translationUnit,
        AssemblyNameDefinition name,
        ModuleKind kind,
        TargetRuntimeDescriptor? targetRuntime,
        Assembly[] importAssemblies)
    {
        var assembly = AssemblyDefinition.CreateAssembly(name, "Primary", kind);
        var module = assembly.MainModule;
        var assemblyContext = new AssemblyContext(assembly, module);

        targetRuntime ??= TargetRuntimeDescriptor.Net60;
        assembly.CustomAttributes.Add(targetRuntime.GetTargetFrameworkAttribute(module));
        module.AssemblyReferences.Add(targetRuntime.GetSystemAssemblyReference());

        var context = new TranslationUnitContext(assemblyContext, importAssemblies);
        foreach (var declaration in translationUnit.Declarations)
        {
            switch (declaration)
            {
                case FunctionDefinition f:
                    EmitFunction(context, f);
                    break;
                case SymbolDeclaration s:
                    EmitSymbol(context, s);
                    break;
            }
        }

        return assembly;
    }
}
