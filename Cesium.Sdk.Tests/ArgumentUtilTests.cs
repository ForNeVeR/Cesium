namespace Cesium.Sdk.Tests;

public class ArgumentUtilTests
{
    [Fact]
    public void PerformTests()
    {
        Assert.Equal("\"\"", ArgumentUtil.ToCommandLineString([""]));
        Assert.Equal("a b c", ArgumentUtil.ToCommandLineString(["a", "b", "c"]));
        Assert.Equal("\"a b\" c", ArgumentUtil.ToCommandLineString(["a b", "c"]));
        Assert.Equal("a\\b c", ArgumentUtil.ToCommandLineString([@"a\b", "c"]));
        Assert.Equal("\"\\\"\"", ArgumentUtil.ToCommandLineString(["\""]));
        Assert.Equal("\"a \\\"b\\\"\"", ArgumentUtil.ToCommandLineString(["a \"b\""]));
        Assert.Equal("\"\\\\\\\"\"", ArgumentUtil.ToCommandLineString(["\\\""]));
        Assert.Equal("\"a\\ \\\\\\\"b\\\"\"", ArgumentUtil.ToCommandLineString(["a\\ \\\"b\""]));
    }
}
