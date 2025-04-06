// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Reflection;
using System.Text;
using Cesium.Solution.Metadata;
using TruePath;

namespace Cesium.TestFramework;

/// <summary>A test to make sure there are no unused approved verification test results.</summary>
public static class TestFileVerification
{
    public static void Verify(IReadOnlyList<Type> types)
    {
        var testAssembly = GetTestAssembly(types);
        var testProjectSourceDirectory = GetTestProjectSourceDirectory(testAssembly);
        var acceptedFileNames = GetAcceptedFilePaths(testProjectSourceDirectory);
        var expectedFileNames = GetExpectedFilePaths(testAssembly, testProjectSourceDirectory, types);

        if (acceptedFileNames.SetEquals(expectedFileNames)) return;

        var redundantFiles = Relativize(acceptedFileNames.Except(expectedFileNames));
        var missingFiles = Relativize(expectedFileNames.Except(acceptedFileNames));
        throw new TestFileVerificationException(
            redundantFiles,
            missingFiles,
            types.Select(x => x.FullName!));

        List<LocalPath> Relativize(IEnumerable<AbsolutePath> paths) =>
            paths.Select(p => p.RelativeTo(testProjectSourceDirectory))
                .ToList();
    }

    public static void VerifyAllTestsFromAssembly(Assembly assembly)
    {
        var testClasses = assembly.GetTypes().Where(t => t.IsAssignableTo(typeof(VerifyTestBase))).ToList();
        Verify(testClasses);
    }

    private static Assembly GetTestAssembly(IEnumerable<Type> types)
    {
        Assembly? assembly = null;
        foreach (var type in types)
        {
            if (assembly == null) assembly = type.Assembly;
            else if (assembly != type.Assembly)
                throw new ArgumentException("All types must be from the same assembly.", nameof(types));
        }

        Assert.NotNull(assembly);
        return assembly;
    }

    private static AbsolutePath GetTestProjectSourceDirectory(Assembly assembly)
    {
        // Assuming that output assembly name (AssemblyName MSBuild property) is equal to the project name
        var projectName = assembly.GetName().Name;
        if (projectName is null)
            throw new InvalidOperationException(
                $"Name is missing for an assembly at location \"{assembly.Location}\".");

        var projectFolder = SolutionMetadata.SourceRoot / projectName;
        var projectFile = projectFolder /  $"{projectName}.csproj";
        if (projectFile.ReadKind() != FileEntryKind.File)
            throw new InvalidOperationException(
                $"Could not find the test project source directory for assembly \"{assembly.Location}\".");

        return projectFolder;
    }

    private static IReadOnlySet<AbsolutePath> GetAcceptedFilePaths(AbsolutePath sourceDirectory)
    {
        return Directory.EnumerateFiles(sourceDirectory.Value, "*.verified.txt", SearchOption.AllDirectories)
            .Select(x => new AbsolutePath(x))
            .ToHashSet();
    }

    private static IReadOnlySet<AbsolutePath> GetExpectedFilePaths(
        Assembly assembly,
        AbsolutePath projectDir,
        IEnumerable<Type> types)
    {
        var assemblyName = assembly.GetName().Name!;
        return types.SelectMany(GetTestFilesFromTest).ToHashSet();

        IEnumerable<AbsolutePath> GetTestFilesFromTest(Type type)
        {
            Assert.StartsWith(assemblyName, type.FullName!);
            var subNamespace = type.FullName![assemblyName.Length..^type.Name.Length].Trim('.');
            var verifiedDirectory = subNamespace.Length == 0
                ? projectDir / "verified"
                : projectDir / subNamespace / "verified";

            var noVerifyMethods = type.GetMethods().Where(m => m.GetCustomAttributes<NoVerifyAttribute>().Any())
                .ToList();
            var theoryMethods = type.GetMethods().Where(m => m.GetCustomAttributes<TheoryAttribute>().Any())
                .Except(noVerifyMethods)
                .ToList();
            var factMethods = type.GetMethods().Where(m => m.GetCustomAttributes<FactAttribute>().Any())
                .Except(noVerifyMethods)
                .Except(theoryMethods);
            return factMethods.Select(GetFileNameFromFactMethod)
                .Concat(theoryMethods.SelectMany(GetTestFilesFromTheoryMethod))
                .Select(f => verifiedDirectory / f);
        }

        string GetFileNameFromFactMethod(MethodInfo method) =>
            $"{method.DeclaringType!.Name}.{method.Name}.verified.txt";

        IEnumerable<string> GetTestFilesFromTheoryMethod(MethodInfo method)
        {
            var parameterNames = method.GetParameters().Select(p => p.Name).ToList();
            var dataAttributes = method.GetCustomAttributes<InlineDataAttribute>();

            return dataAttributes.Select(da =>
            {
                var data = da.GetData(method).Single();
                Assert.Equal(parameterNames.Count, data.Length);
                var dataString = string.Join("_", parameterNames.Zip(data).Select(pd => $"{pd.First}={pd.Second}"));
                return $"{method.DeclaringType!.Name}.{method.Name}_{dataString}.verified.txt";
            });
        }
    }
}

public class TestFileVerificationException : Exception
{
    internal TestFileVerificationException(
        IReadOnlyList<LocalPath> unusedFiles,
        IReadOnlyList<LocalPath> missingFiles,
        IEnumerable<string> testNames)
    {
        var message = new StringBuilder();
        if (unusedFiles.Count > 0)
            message.Append(
                $"The following files are not used by any tests ({unusedFiles.Count}):\n{Quoted(unusedFiles)}\n");

        if (missingFiles.Count > 0)
            message.Append(
                $"The following files are missing from the verified directories ({missingFiles.Count}):" +
                $"\n{Quoted(missingFiles)}\n");

        Message = $"""
                    {message}
                    Tests checked:
                    {string.Join("\n", testNames)}
                    """;
        return;

        string Quoted(IEnumerable<LocalPath> files) =>
            string.Join("\n", files.OrderBy(x => x.Value).Select(f => '"' + f.Value + '"'));
    }

    public override string Message { get; }
}

/// <summary>
/// This attribute marks tests that should not be expected to generate the verification files. In particular, these are
/// the negative compilation tests.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class NoVerifyAttribute : Attribute;
