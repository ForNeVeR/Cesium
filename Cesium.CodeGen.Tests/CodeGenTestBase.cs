using System.Diagnostics.CodeAnalysis;
using System.Text;
using Cesium.CodeGen.Contexts;
using Cesium.Core;
using Cesium.Parser;
using Cesium.TestFramework;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Yoakke.Streams;
using Yoakke.SynKit.C.Syntax;

namespace Cesium.CodeGen.Tests;

[UseInvariantCulture]
public abstract class CodeGenTestBase : VerifyTestBase
{
    protected static AssemblyDefinition GenerateAssembly(TargetRuntimeDescriptor? runtime, params string[] sources)
    {
        var (assembly, _) = GenerateAssembly(
            sources,
            runtime,
            @namespace: "",
            globalTypeFqn: "",
            referencePaths: Array.Empty<string>());
        return assembly;
    }

    protected static AssemblyDefinition GenerateAssembly(
        TargetRuntimeDescriptor? runtime,
        TargetArchitectureSet arch = TargetArchitectureSet.Dynamic,
        string @namespace = "",
        string globalTypeFqn = "", params string[] sources)
    {
        var (assembly, _) = GenerateAssembly(sources, runtime, arch, @namespace, globalTypeFqn, Array.Empty<string>());
        return assembly;
    }

    protected static (AssemblyDefinition, byte[]) GenerateAssembly(
        string[] sources,
        TargetRuntimeDescriptor? runtime = null,
        TargetArchitectureSet arch = TargetArchitectureSet.Dynamic,
        string @namespace = "",
        string globalTypeFqn = "",
        string[]? referencePaths = null)
    {
        var context = CreateAssembly(runtime, arch, @namespace: @namespace, globalTypeFqn: globalTypeFqn, referencePaths);
        GenerateCode(context, sources);
        return EmitAssembly(context);
    }

    protected static void DoesNotCompile(
        [StringSyntax("cpp")] string source,
        string expectedMessage,
        TargetRuntimeDescriptor? runtime = null,
        TargetArchitectureSet arch = TargetArchitectureSet.Dynamic,
        string @namespace = "",
        string globalTypeFqn = "")
    {
        DoesNotCompile<CompilationException>(source, expectedMessage, runtime, arch, @namespace, globalTypeFqn);
    }

    protected static void DoesNotCompile<T>(
        string source,
        string expectedMessage,
        TargetRuntimeDescriptor? runtime = null,
        TargetArchitectureSet arch = TargetArchitectureSet.Dynamic,
        string @namespace = "",
        string globalTypeFqn = "") where T : CesiumException
    {
        var ex = Assert.Throws<T>(() => GenerateAssembly(runtime, arch, @namespace, globalTypeFqn, source));
        Assert.Contains(expectedMessage, ex.Message);
    }

    private static AssemblyContext CreateAssembly(
        TargetRuntimeDescriptor? targetRuntime,
        TargetArchitectureSet targetArchitectureSet = TargetArchitectureSet.Dynamic,
        string @namespace = "",
        string globalTypeFqn = "",
        string[]? referencePaths = null)
    {
        var allReferences = (referencePaths ?? Array.Empty<string>()).ToList();
        allReferences.Insert(0, typeof(Console).Assembly.Location);

        CompilationOptions compilationOptions = new(
            targetRuntime ?? CSharpCompilationUtil.DefaultRuntime,
            targetArchitectureSet,
            ModuleKind.Console,
            typeof(Math).Assembly.Location,
            typeof(Runtime.RuntimeHelpers).Assembly.Location,
            allReferences,
            @namespace,
            globalTypeFqn,
            Array.Empty<string>(),
            Array.Empty<string>(),
            ProducePreprocessedFile: false);
        return AssemblyContext.Create(
            new AssemblyNameDefinition("test", new Version()),
            compilationOptions);
    }

    private static void GenerateCode(AssemblyContext context, IEnumerable<string> sources)
    {
        foreach (var source in sources)
        {
            var lexer = new CLexer(source);
            var parser = new CParser(lexer);
            var translationUnit = parser.ParseTranslationUnit();
            if (translationUnit.IsError)
                throw new ParseException(translationUnit.GetErrorString() ?? "Unknown parse error");

            if (parser.TokenStream.Peek().Kind != CTokenType.End)
                throw new ParseException($"Excessive output after the end of a translation unit at {lexer.Position}.");

            context.EmitTranslationUnit("testInput", translationUnit.Ok.Value);
        }
    }

    private static (AssemblyDefinition, byte[]) EmitAssembly(AssemblyContext context)
    {
        var assembly = context.VerifyAndGetAssembly();

        // To resolve IL labels:
        using var stream = new MemoryStream();
        assembly.Write(stream);

        return (assembly, stream.ToArray());
    }

    [MustUseReturnValue]
    protected static Task VerifyTypes(AssemblyDefinition assembly, params object[] parameters)
    {
        var result = new StringBuilder();
        foreach (var module in assembly.Modules)
        {
            result.AppendLine($"Module: {module}");
            DumpTypes(module.Types, result, 1);
        }

        return Verify(result, GetSettings(parameters));
    }

    [MustUseReturnValue]
    protected static Task VerifyMethods(TypeDefinition type, params object[] parameters)
    {
        var result = new StringBuilder();
        DumpMethods(type, result);

        return Verify(result, GetSettings(parameters));
    }

    [MustUseReturnValue]
    protected static Task VerifyMethods(IEnumerable<TypeDefinition?> types, params object[] parameters)
    {
        var result = new StringBuilder();
        int i = 0;
        foreach (var type in types.Where(t => t is not null))
        {
            if (i != 0)
            {
                result.AppendLine();
            }

            DumpMethods(type!, result);
            i++;
        }

        return Verify(result, GetSettings(parameters));
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

            var isPinvoke = method.HasPInvokeInfo;

            result.Append($"{(isPinvoke ? $"pinvokeimpl({method.PInvokeInfo.Module.Name}, {method.PInvokeInfo.EntryPoint}) " : null)}{Indent(indent)}{method.ReturnType} {method.DeclaringType}::{method.Name}(");
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

            if (isPinvoke) continue;

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
