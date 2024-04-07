namespace Cesium.Sdk.Tests.Framework;

public static class AssertEx
{
    public static void Includes<T>(IReadOnlyCollection<T> expected, IReadOnlyCollection<T> all)
    {
        var foundItems = all.Where(expected.Contains).ToList();
        var remainingItems = expected.Except(foundItems).ToList();
        if (remainingItems.Count != 0)
            throw new IncludesAssertFailedException<T>(remainingItems);
    }
}
