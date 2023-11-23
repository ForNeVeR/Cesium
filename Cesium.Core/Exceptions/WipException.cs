using JetBrains.Annotations;

namespace Cesium.Core;

public sealed class WipException : CesiumException
{
    /// <summary>A marker value for an issue number not yet assigned.</summary>
    /// <remarks>
    /// Should only be used in PRs while developing new features. Before merging to the main branch, all instances of
    /// this constant should be replaced with newly assigned issue numbers.
    /// </remarks>
    [PublicAPI]
    public const int ToDo = -1;

    public WipException(int issueNo, string additionalMessage)
        : base($"This work is in progress. {additionalMessage}. See https://github.com/ForNeVeR/Cesium/issues/{issueNo}.")
    {
    }

    [PublicAPI]
    public WipException(int issueNo)
        : base($"This work is in progress. See https://github.com/ForNeVeR/Cesium/issues/{issueNo}.")
    {
    }
}
