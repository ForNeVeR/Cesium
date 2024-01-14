using System.Reflection;
using System.Text;

namespace Cesium.TestFramework;

/// <summary>A test to make sure there are no unused approved verification test results.</summary>
public static class TestFileVerification
{
    public static void Verify(IReadOnlyList<Type> types)
    {
        var testAssembly = GetTestAssembly(types);
        var testProjectSourceDirectory = GetTestProjectSourceDirectory(testAssembly);
        var acceptedFileNames = GetAcceptedFilePaths(testProjectSourceDirectory);
        var expectedFileNames = GetExpectedFilePaths(testProjectSourceDirectory, types);

        if (acceptedFileNames.SetEquals(expectedFileNames)) return;

        var redundantFiles = Relativize(acceptedFileNames.Except(expectedFileNames));
        var missingFiles = Relativize(expectedFileNames.Except(acceptedFileNames));
        throw new TestFileVerificationException(
            redundantFiles,
            missingFiles,
            types.Select(x => x.FullName!));

        List<string> Relativize(IEnumerable<string> paths) =>
            paths.Select(p => Path.GetRelativePath(testProjectSourceDirectory, p))
                .ToList();
    }

    public static void VerifyAllTestsFromAssembly(Assembly assembly)
    {
        var testClasses = assembly.GetTypes().Where(t => t.GetCustomAttributes<UsesVerifyAttribute>().Any()).ToList();
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
        return assembly!;
    }

    private static string GetTestProjectSourceDirectory(Assembly assembly)
    {
        var currentPath = Path.GetDirectoryName(assembly.Location)!;
        while (!Directory.EnumerateFileSystemEntries(currentPath, "*.csproj").Any())
        {
            currentPath = Path.GetDirectoryName(currentPath);
            if (currentPath == null)
                throw new InvalidOperationException(
                    $"Could not find the test project source directory for assembly \"{assembly.Location}\".");
        }

        return currentPath;
    }

    private static IReadOnlySet<string> GetAcceptedFilePaths(string sourceDirectory)
    {
        return Directory.EnumerateFiles(sourceDirectory, "*.verified.txt", SearchOption.AllDirectories).ToHashSet();
    }

    private static IReadOnlySet<string> GetExpectedFilePaths(string sourcePath, IEnumerable<Type> types)
    {
        return types.SelectMany(GetTestFilesFromTest).ToHashSet();

        IEnumerable<string> GetTestFilesFromTest(Type type)
        {
            var factMethods = type.GetMethods().Where(m => m.GetCustomAttributes<FactAttribute>().Any());
            var theoryMethods = type.GetMethods().Where(m => m.GetCustomAttributes<TheoryAttribute>().Any());
            return factMethods.Select(GetTestFileFromFactMethod)
                .Concat(theoryMethods.SelectMany(GetTestFilesFromTheoryMethod));
        }

        string GetTestFileFromFactMethod(MethodInfo method) =>
            Path.Combine(sourcePath, "verified", $"{method.DeclaringType!.Name}.{method.Name}.verified.txt");

        IEnumerable<string> GetTestFilesFromTheoryMethod(MethodInfo method)
        {
            var parameterNames = method.GetParameters().Select(p => p.Name).ToList();
            var dataAttributes = method.GetCustomAttributes<InlineDataAttribute>();

            return dataAttributes.Select(da =>
            {
                var data = da.GetData(method).Single();
                Assert.Equal(parameterNames.Count, data.Length);
                var dataString = string.Join(",", parameterNames.Zip(data).Select(pd => $"{pd.First}={pd.Second}"));
                return Path.Combine(sourcePath, "verified",
                    $"{method.DeclaringType!.Name}.{method.Name}_{dataString}.verified.txt");
            });
        }
    }
}

public class TestFileVerificationException : Exception
{
    internal TestFileVerificationException(IReadOnlyList<string> unusedFiles, IReadOnlyList<string> missingFiles, IEnumerable<string> testNames)
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

        string Quoted(IEnumerable<string> files) =>
            string.Join("\n", files.OrderBy(x => x).Select(f => '"' + f + '"'));
    }

    public override string Message { get; }
}
