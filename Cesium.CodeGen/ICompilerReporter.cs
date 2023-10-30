namespace Cesium.CodeGen;

public interface ICompilerReporter
{
    void ReportError(string message);
    void ReportInformation(string message);
}
