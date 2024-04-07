using Xunit.Sdk;

namespace Cesium.Sdk.Tests.Framework;

public class IncludesAssertFailedException<T>(
    IEnumerable<T> expected,
    Exception? innerException = null)
    : XunitException($"Expected elements are missing: [{string.Join(", ", expected)}]", innerException);
