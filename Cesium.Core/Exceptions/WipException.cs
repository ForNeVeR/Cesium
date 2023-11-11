namespace Cesium.Core;

public sealed class WipException : CesiumException
{
    /// <summary>A marker value for an issue number not yet assigned.</summary>
    public const int ToDo = -1;

    public WipException(int issueNo, string additionalMessage)
        : base($"This work is in progress. {additionalMessage}. See https://github.com/ForNeVeR/Cesium/issues/{issueNo}.")
    {
    }

    public WipException(int issueNo)
        : base($"This work is in progress. See https://github.com/ForNeVeR/Cesium/issues/{issueNo}.")
    {
    }
}
