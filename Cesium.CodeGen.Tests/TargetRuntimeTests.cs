using System.Runtime.Versioning;
using System.Text;
using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class TargetRuntimeTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTargetRuntimeTest(TargetRuntimeDescriptor? targetRuntime)
    {
        var assembly = GenerateAssembly(targetRuntime, "int main() {}");

        var targetFrameworkAttribute = assembly.CustomAttributes
            .Single(a => a.AttributeType.FullName == typeof(TargetFrameworkAttribute).FullName);
        var frameworkName = (string)targetFrameworkAttribute.ConstructorArguments.Single().Value;

        var result = new StringBuilder();
        result.AppendLine($"TargetFrameworkAttribute.FrameworkName: {frameworkName}");

        var verify = Verify(result, GetSettings());
        return targetRuntime != null
            ? verify.UseParameters(
                targetRuntime.Kind,
                targetRuntime.SystemLibraryVersion,
                targetRuntime.TargetFrameworkVersion)
            : verify;
    }

    [Fact]
    public Task DefaultedFrameworkTest() =>
        DoTargetRuntimeTest(default);

    [Theory]
    [InlineData(SystemAssemblyKind.MsCorLib, "4.0.0.0", "4.8")]
    [InlineData(SystemAssemblyKind.SystemRuntime, "4.2.2.0", "6.0")]
    [InlineData(SystemAssemblyKind.NetStandard, "2.1.0.0", "2.1")]
    public Task FrameworkTest(
        SystemAssemblyKind targetFramework,
        string systemAssemblyVersionString,
        string frameworkVersionString) =>
        DoTargetRuntimeTest(new TargetRuntimeDescriptor(
            targetFramework,
            new Version(systemAssemblyVersionString),
            new Version(frameworkVersionString)));
}
