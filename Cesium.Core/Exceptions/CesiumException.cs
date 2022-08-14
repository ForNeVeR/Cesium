namespace Cesium.Core.Exceptions;

public abstract class CesiumException : Exception
{
    protected CesiumException(string message) : base(message)
    {

    }
}
