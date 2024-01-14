using System.Reflection;
using Cesium.TestFramework;

namespace Cesium.CodeGen.Tests;

public class AssemblyFileVerifier
{
    [Fact]
    public void AssemblyHasNoUnusedTestFiles() =>
        TestFileVerification.VerifyAllTestsFromAssembly(Assembly.GetExecutingAssembly());
}
