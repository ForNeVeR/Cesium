using System.Text;
using Cesium.CodeGen.Contexts;
using Cesium.CodeGen.Extensions;
using Cesium.Parser;
using Cesium.Test.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Yoakke.Streams;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.CodeGen.Tests;

public abstract class CodeGenTestBase : VerifyTestBase
{
    protected static AssemblyDefinition GenerateAssembly(TargetRuntimeDescriptor? runtime, params string[] sources)
    {
        var context = CreateAssembly(runtime);
        GenerateCode(context, sources);
        return EmitAssembly(context);
    }
    protected static AssemblyDefinition GenerateAssembly(TargetRuntimeDescriptor? runtime, string @namespace = "", string globalTypeFqn = "", params string[] sources)
    {
        var context = CreateAssembly(runtime, @namespace, globalTypeFqn);
        GenerateCode(context, sources);
        return EmitAssembly(context);
    }

    protected static void DoesNotCompile(string source, string expectedMessage)
    {
        var ex = Assert.Throws<NotSupportedException>(() => GenerateAssembly(default, source));
        Assert.Contains(expectedMessage, ex.Message);
    }

    private static AssemblyContext CreateAssembly(TargetRuntimeDescriptor? targetRuntime, string @namespace = "", string globalTypeFqn = "") =>
        AssemblyContext.Create(
            new AssemblyNameDefinition("test", new Version()),
            ModuleKind.Console,
            targetRuntime,
            new [] { typeof(Console).Assembly.Location },
            typeof(Math).Assembly.Location,
            typeof(Cesium.Runtime.RuntimeHelpers).Assembly.Location,
            @namespace,
            globalTypeFqn);

    private static void GenerateCode(AssemblyContext context, IEnumerable<string> sources)
    {
        foreach (var source in sources)
        {
            var lexer = new CLexer(source);
            var parser = new CParser(lexer);
            var translationUnit = parser.ParseTranslationUnit();
            if (translationUnit.IsError)
            {
                throw new InvalidOperationException(translationUnit.GetErrorString());
            }

            if (parser.TokenStream.Peek().Kind != CTokenType.End)
                throw new Exception($"Excessive output after the end of a translation unit at {lexer.Position}.");

            context.EmitTranslationUnit(translationUnit.Ok.Value.ToIntermediate());
        }
    }

    private static AssemblyDefinition EmitAssembly(AssemblyContext context)
    {
        var assembly = context.VerifyAndGetAssembly();

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

        return Verify(result, GetSettings());
    }

    protected static Task VerifyMethods(TypeDefinition type)
    {
        var result = new StringBuilder();
        DumpMethods(type, result);

        return Verify(result, GetSettings());
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

            if (type.HasCustomAttributes)
                PrintCustomAttributes(indent, type.CustomAttributes);

            if (type.HasNestedTypes)
            {
                result.AppendLine($"{Indent(indent)}Nested types:");
                DumpTypes(type.NestedTypes, result, indent + 1);
            }

            if (type.HasFields)
            {
                result.AppendLine($"{Indent(indent)}Fields:");
                foreach (var field in type.Fields)
                {
                    result.AppendLine($"{Indent(indent + 1)}{field}");

                    if (field.HasCustomAttributes)
                        PrintCustomAttributes(indent + 1, field.CustomAttributes);

                    var initialValue = field.InitialValue;
                    if (initialValue.Length > 0)
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

        void PrintCustomAttributes(int nestedIndent, IEnumerable<CustomAttribute> customAttributes)
        {
            result.AppendLine($"{Indent(nestedIndent)}Custom attributes:");
            foreach (var customAttribute in customAttributes)
            {
                var arguments = string.Join(", ", customAttribute.ConstructorArguments.Select(a => a.Value));
                result.AppendLine($"{Indent(nestedIndent)}- {customAttribute.AttributeType.Name}({arguments})");
            }

            result.AppendLine();
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
