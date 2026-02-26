// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenWhileTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task SimpleWhile() => DoTest(
        @"int main()
{
    int i = 0;
    while (i < 10) ++i;
}");

    [Fact]
    public Task DoWhile() => DoTest(
        @"int main()
{
    int i = 0;
    do ++i; while (i < 10);
}");

    [Fact]
    public Task InfinityWhile() => DoTest(
        @"int main()
{
    int i = 0;
    while (1) i++;
}");

    [Fact]
    public Task InfinityDoWhile() => DoTest(
        @"int main()
{
    int i = 0;
    do i++; while (1);
}");

    [Fact]
    public Task NegativeWhile() => DoTest(
        @"int main()
{
    int i = 0;
    while (!1) i++;
}");

    [Fact]
    public Task NegativeDoWhile() => DoTest(
        @"int main()
{
    int i = 0;
    do i++; while (!1);
}");
}
