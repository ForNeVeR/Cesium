using Cesium.Core.Warnings;

namespace Cesium.Compiler;

public class WarningProcessor : IWarningProcessor
{
    public void EmitWarning(PreprocessorWarning warning)
    {
        Console.Error.WriteLine($"{warning.Location}: warning: {warning.Message}");
    }
}
