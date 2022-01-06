using System.Text;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Generators;
using Cesium.Parser;
using Cesium.Test.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Yoakke.C.Syntax;

namespace Cesium.CodeGen.Tests;

public abstract class CodeGenTestBase : VerifyTestBase
{
    protected static AssemblyDefinition GenerateAssembly(string source, TargetRuntimeDescriptor? targetRuntime)
    {
        var translationUnitParseResult = new CParser(new CLexer(source)).ParseTranslationUnit();
        if (translationUnitParseResult.IsError)
        {
            
        }

        var translationUnit = translationUnitParseResult.Ok.Value;
        var assembly = Assemblies.Generate(
            translationUnit,
            new AssemblyNameDefinition("test", new Version()),
            ModuleKind.Console,
            targetRuntime,
            new [] { typeof(Console).Assembly });

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

            result.Append($"{Indent(indent)}{method.ReturnType} {method.DeclaringType}::{method.Name}(");
            var firstParam = true;
            foreach (var param in method.Parameters)
            {
                if (!firstParam)
                    result.Append(", ");
                firstParam = false;
                result.Append($"{param.ParameterType}");
                if (param.Name != null)
                    result.Append($" {param.Name}");
            }

            result.AppendLine(")");
            var variables = method.Body.Variables;
            if (variables.Count > 0)
            {
                result.AppendLine($"{Indent(indent + 1)}Locals:");
                foreach (var local in variables)
                {
                    result.Append($"{Indent(indent + 2)}{local.VariableType}");
                    if (local.IsPinned)
                        result.Append(" (pinned)");
                    result.AppendLine($" {local}");
                }
            }

            foreach (var instruction in method.Body.Instructions)
                result.AppendLine($"{Indent(indent + 1)}{instruction}");

            if (method.Body.HasExceptionHandlers)
                result.AppendLine($"{Indent(indent + 1)}Exception handlers:");

            foreach (var handler in method.Body.ExceptionHandlers)
            {
                static string Label(Instruction i) => $"IL_{i.Offset:x4}";

                result.AppendLine($"{Indent(indent + 2)}{handler.HandlerType}:");
                result.AppendLine($"{Indent(indent + 3)}try: {Label(handler.TryStart)}..{Label(handler.TryEnd)}");
                result.AppendLine(
                    $"{Indent(indent + 3)}handler: {Label(handler.HandlerStart)}..{Label(handler.HandlerEnd)}");
            }
        }
    }
}
