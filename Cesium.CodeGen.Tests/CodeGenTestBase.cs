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
            DumpTypes(module.Types, result, 1);
        }

        return Verify(result);
    }

    protected static Task VerifyMethods(TypeDefinition type)
    {
        var result = new StringBuilder();
        DumpMethods(type, result);

        return Verify(result);
    }

    private static void DumpTypes(IEnumerable<TypeDefinition> types, StringBuilder result, int indent)
    {
        var first = true;
        foreach (var type in types)
        {
            if (!first)
                result.AppendLine();
            first = false;

            result.AppendLine($"{Indent(indent)}Type: {type}");
            if (type.PackingSize != -1)
                result.AppendLine($"{Indent(indent)}Pack: {type.PackingSize}");
            if (type.ClassSize != -1)
                result.AppendLine($"{Indent(indent)}Size: {type.ClassSize}");

            if (type.HasNestedTypes)
            {
                result.AppendLine($"{Indent(indent)}Types:");
                DumpTypes(type.NestedTypes, result, indent + 1);
            }

            if (type.HasFields)
            {
                result.AppendLine($"{Indent(indent)}Fields:");
                foreach (var field in type.Fields)
                {
                    result.AppendLine($"{Indent(indent + 1)}{field}");
                    var initialValue = field.InitialValue;
                    if (initialValue != null)
                    {
                        if (type.Name == AssemblyContext.ConstantPoolTypeName)
                        {
                            var length = initialValue.Length > 0 && initialValue.Last() == '\0'
                                ? initialValue.Length - 1
                                : initialValue.Length;
                            var value = Encoding.UTF8.GetString(initialValue, 0, length);
                            result.AppendLine(
                                $"{Indent(indent + 2)}Init with (UTF-8 x {initialValue.Length} bytes): \"{value}\"");
                        }
                        else
                        {
                            result.AppendLine($"{Indent(indent + 2)} Init with: [{string.Join(", ", initialValue)}]");
                        }
                    }
                }
            }

            if (type.HasMethods)
            {
                result.AppendLine($"{Indent(indent)}Methods:");
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
