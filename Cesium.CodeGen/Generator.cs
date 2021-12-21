using Cesium.Ast;
using Mono.Cecil;
using static Cesium.CodeGen.Functions;

namespace Cesium.CodeGen;

public class Generator
{
    public enum TargetFrameworks
    {
        mscorlib,
        SystemRuntime,
        netstandard,
    }

    public record TargetRuntimeIdentifier(
        TargetFrameworks targetFramework,
        Version version);

    private static readonly TargetRuntimeIdentifier defaultTargetRuntime =
        new(TargetFrameworks.netstandard, new Version(2, 1, 0, 0));

    public static AssemblyDefinition GenerateAssembly(
        TranslationUnit translationUnit,
        AssemblyNameDefinition name,
        ModuleKind kind,
        TargetRuntimeIdentifier? targetRuntime = default)
    {
        var assembly = AssemblyDefinition.CreateAssembly(name, "Primary", kind);
        var module = assembly.MainModule;

        var tr = targetRuntime ?? defaultTargetRuntime;
        var (assemblyName, publicKeyToken) =
            tr.targetFramework switch
            {
                TargetFrameworks.mscorlib =>
                    ("mscorlib", new byte[] { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 }),
                TargetFrameworks.SystemRuntime =>
                    ("System.Runtime", new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a }),
                _ =>
                    ("netstandard", new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a })
            };
        var corlibReference = new AssemblyNameReference(
            assemblyName, tr.version)
        {
            PublicKeyToken = publicKeyToken
        };
        module.AssemblyReferences.Add(corlibReference);

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
