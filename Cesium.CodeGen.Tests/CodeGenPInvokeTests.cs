// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.TestFramework;
using TruePath;

namespace Cesium.CodeGen.Tests;

public class CodeGenPInvokeTests : CodeGenTestBase
{
    private static readonly string _mainMockedFilePath = OperatingSystem.IsWindows() ? @"C:\a\b\c.c" : "/a/b/c.c";

    private static async Task DoTest(string source)
    {
        var processed = await PreprocessorUtil.DoPreprocess(new AbsolutePath(_mainMockedFilePath), source);
        var assembly = GenerateAssembly(null, processed);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        await VerifyMethods(moduleType);
    }

    [Fact]
    public Task SinglePinvokePragma() => DoTest(@"
#pragma pinvoke(""mydll.dll"")
int not_pinvoke(void);
int foo_bar(int*);

int main() {
    return foo_bar(0);
}

int not_pinvoke(void) { return 1; }
");

    [Fact] // win_puts -> pinvokeimpl(msvcrt, puts) int win_puts();
    public Task PInvokePrefixPragma() => DoTest(@"
#pragma pinvoke(""msvcrt"", win_)
int win_puts(const char*);
");
}
