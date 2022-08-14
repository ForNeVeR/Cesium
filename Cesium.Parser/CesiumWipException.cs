namespace Cesium;

public class CesiumWipException : CesiumException
{
    public CesiumWipException(int issueNo, string additionalMessage)
        : base($"This work is in progress. {additionalMessage}. See https://github.com/ForNeVeR/Cesium/issues/{issueNo}.")
    {
    }

    public CesiumWipException(int issueNo)
        : base($"This work is in progress. See https://github.com/ForNeVeR/Cesium/issues/{issueNo}.")
    {
    }
}
