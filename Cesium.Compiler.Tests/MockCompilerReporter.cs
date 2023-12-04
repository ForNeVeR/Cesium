using Cesium.CodeGen;

namespace Cesium.Compiler.Tests;

internal sealed class MockCompilerReporter : ICompilerReporter
{
    public List<string> Errors { get; set; } = new();
    public List<string> InformationMessages { get; set; } = new();
    public void ReportError(string message)
    {
        this.Errors.Add(message);
    }

    public void ReportInformation(string message)
    {
        this.InformationMessages.Add(message);
    }
}
