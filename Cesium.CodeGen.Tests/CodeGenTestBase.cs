using System.Text;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Generators;
using Cesium.Parser;
using Cesium.Test.Framework;
using Mono.Cecil;
using Yoakke.C.Syntax;

namespace Cesium.CodeGen.Tests;

public abstract class CodeGenTestBase : VerifyTestBase
{
    protected static AssemblyDefinition GenerateAssembly(string source, TargetRuntimeDescriptor? targetRuntime)
    {
        var translationUnit = new CParser(new CLexer(source)).ParseTranslationUnit().Ok.Value;
        var assembly = Assemblies.Generate(
            translationUnit,
            new AssemblyNameDefinition("test", new Version()),
            ModuleKind.Console,
            targetRuntime);

        // To resolve IL labels:
        using var stream = new MemoryStream();
        assembly.Write(stream);

        return assembly;
    }

    protected static Task VerifyTypes(AssemblyDefinition assembly)
    {
        var result = new StringBuilder();
        foreach (var module in assembly.Modules)
        {
            result.AppendLine($"Module: {module}");
            DumpTypes(module, result);
        }

        return Verify(result);
    }

    protected static Task VerifyMethods(TypeDefinition type)
    {
        var result = new StringBuilder();
        DumpMethods(type, result);

        return Verify(result);
    }

    private static void DumpTypes(ModuleDefinition module, StringBuilder result)
    {
        var first = true;
        foreach (var type in module.Types)
        {
            if (!first)
                result.AppendLine();
            first = false;

            result.AppendLine($"{Indent()}Type: {type}");
            if (type.PackingSize != -1)
                result.AppendLine($"{Indent()}Pack: {type.PackingSize}");
            if (type.ClassSize != -1)
                result.AppendLine($"{Indent()}Size: {type.ClassSize}");

            if (type.HasFields)
            {
                result.AppendLine($"{Indent()}Fields:");
                foreach (var field in type.Fields)
                {
                    result.AppendLine($"{Indent(2)}{field}");
                    if (field.InitialValue != null)
                    {
                        if (type.Name == AssemblyContext.ConstantPoolTypeName)
                        {
                            var value = Encoding.UTF8.GetString(field.InitialValue);
                            result.AppendLine($"{Indent(3)}Init with (UTF-8 x {field.InitialValue.Length}): \"{value}\"");
                        }
                        else
                        {
                            result.AppendLine($"{Indent(3)} Init with: [{string.Join(", ", field.InitialValue)}]");
                        }
                    }
                }
            }

            if (type.HasMethods)
            {
                result.AppendLine($"{Indent()}Methods:");
                DumpMethods(type, result, 2);
            }
        }
    }

    private static string Indent(int n = 1) => new(' ', n * 2);

    private static void DumpMethods(TypeDefinition type, StringBuilder result, int indent = 0)
    {
        var first = true;
        foreach (var method in type.Methods)
        {
            if (!first)
                result.AppendLine();
            first = false;

            result.AppendLine(Indent(indent) + method);
            var variables = method.Body.Variables;
            if (variables.Count > 0)
            {
                result.AppendLine($"{Indent(indent + 1)}Locals:");
                foreach (var local in variables)
                    result.AppendLine($"{Indent(indent + 2)}{local.VariableType} {local}");
            }


            foreach (var instruction in method.Body.Instructions)
                result.AppendLine($"{Indent(indent + 1)}{instruction}");
        }
    }
}
