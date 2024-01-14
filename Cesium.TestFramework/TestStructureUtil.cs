using System.Reflection;

namespace Cesium.TestFramework;

public static class TestStructureUtil
{
    public static readonly string SolutionRootPath = GetSolutionRoot();

    private static string GetSolutionRoot()
    {
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var currentDirectory = assemblyDirectory;
        while (currentDirectory != null)
        {
            if (File.Exists(Path.Combine(currentDirectory, "Cesium.sln")))
                return currentDirectory;

            currentDirectory = Path.GetDirectoryName(currentDirectory);
        }

        throw new Exception($"Could not find the solution directory going up from directory \"{assemblyDirectory}\".");
    }
}
