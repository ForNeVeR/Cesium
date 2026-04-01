// SPDX-FileCopyrightText: 2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

namespace Cesium.Core.Tests;

public class StringFormatExtensionsTest
{
    [Theory]
    [InlineData("", "")]
    [InlineData("hello", "Hello")]
    [InlineData("hello-world", "HelloWorld")]
    [InlineData("-hello", "Hello")]
    [InlineData("hello-", "Hello")]
    [InlineData("hello--world", "HelloWorld")]
    [InlineData("---", "")]
    [InlineData("hello-123", "Hello123")]
    [InlineData("a-b-123", "AB123")]
    [InlineData("HELLO-WORLD", "HELLOWORLD")]
    [InlineData("a", "A")]
    [InlineData("a-b", "AB")]
    public void StringFromKebabToCamelTest(string input, string expected)
        => Assert.Equal(expected, input.FromKebabToCamel());

    [Theory]
    [InlineData("", "")]
    [InlineData("Hello", "hello")]
    [InlineData("HelloWorld", "hello-world")]
    [InlineData("hello", "hello")]
    [InlineData("HELLO", "hello")]
    [InlineData("XMLHttpRequest", "xml-http-request")]
    [InlineData("Hello123", "hello-123")]
    [InlineData("Hello123World", "hello-123-world")]
    [InlineData("A", "a")]
    [InlineData("a", "a")]
    [InlineData("123", "123")]
    [InlineData("A1B2", "a-1-b-2")]
    [InlineData("AbcDef", "abc-def")]
    public void StringFromCamelToKebabTest(string input, string expected)
        => Assert.Equal(expected, input.FromCamelToKebab());
}
