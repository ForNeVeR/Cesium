// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenDeclarationsTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task StaticDeclarationsInDifferentFunctions() => DoTest(
        @"int main()
{
    static int i = 0;
}
int test()
{
    static int i = 2;
}");

    [Fact]
    public Task StaticDeclarationsFuncionAndGlobalContext() => DoTest(
        @"int main()
{
    static int i = 0;
}

    static int i = 2;
");

    [Fact]
    public Task ConstTypeDef() => DoTest(
        @"
typedef int myint;
static const myint i = 0;");

    [Fact]
    public Task ЕнзуВуаЕцшсу() => DoTest(
        @"
typedef int myint;
static const myint i = 0;");
}
