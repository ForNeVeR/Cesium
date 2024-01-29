namespace Cesium.Core.Warnings;

public interface IWarningProcessor
{
    public void EmitWarning(PreprocessorWarning warning);
}
