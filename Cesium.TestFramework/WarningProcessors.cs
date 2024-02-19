using Cesium.Core.Warnings;

namespace Cesium.TestFramework;

public class LambdaWarningProcessor(Action<PreprocessorWarning> onWarning) : IWarningProcessor
{
    public void EmitWarning(PreprocessorWarning warning) => onWarning(warning);
}

public sealed class ListWarningProcessor : IWarningProcessor, IDisposable
{
    public readonly List<PreprocessorWarning> Warnings = new();
    public void EmitWarning(PreprocessorWarning warning)
    {
        Warnings.Add(warning);
    }

    private void AssertNoWarnings()
    {
        Assert.Empty(Warnings);
    }

    public void Dispose()
    {
        AssertNoWarnings();
    }
}
