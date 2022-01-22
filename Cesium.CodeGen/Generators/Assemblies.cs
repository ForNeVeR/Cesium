using System.Reflection;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Ir.TopLevel;
using Mono.Cecil;

namespace Cesium.CodeGen.Generators;

public static class Assemblies
{
    public static AssemblyContext Create(
        AssemblyNameDefinition name,
        ModuleKind kind,
        TargetRuntimeDescriptor? targetRuntime,
        Assembly[] importAssemblies)
    {
        var assembly = AssemblyDefinition.CreateAssembly(name, "Primary", kind);
        var module = assembly.MainModule;
        var assemblyContext = new AssemblyContext(assembly, module, importAssemblies);

        targetRuntime ??= TargetRuntimeDescriptor.Net60;
        assembly.CustomAttributes.Add(targetRuntime.GetTargetFrameworkAttribute(module));
        module.AssemblyReferences.Add(targetRuntime.GetSystemAssemblyReference());

        return assemblyContext;
    }

    public static void EmitTranslationUnit(AssemblyContext assemblyContext, IEnumerable<ITopLevelNode> nodes)
    {
        var context = new TranslationUnitContext(assemblyContext);
        foreach (var node in nodes)
            node.Emit(context);
    }
}
