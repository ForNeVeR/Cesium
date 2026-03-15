// SPDX-FileCopyrightText: 2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using Cesium.TestFramework;

namespace Cesium.CodeGen.Tests;

public class ReturnCheckerForEntirePathsTests : CodeGenTestBase
{
    private static void ShouldCompile([StringSyntax("cpp")] string source)
    {
        GenerateAssembly(default, source);
    }

    [Fact, NoVerify]
    public void MissedReturnAfterPositiveIfValid() => ShouldCompile(
        @"int foo()
{
    int i = 0;
    if (1) return i;
    i++;
}");

    [Fact, NoVerify]
    public void MissedReturnInNegationIfValid() => ShouldCompile(
        @"int foo()
{
    int i = 0;
    if (!1) i++;
    return i;
    i++;
}");

    [Fact, NoVerify]
    public void MissedReturnAfterNegationIfInvalid() => DoesNotCompile(
        @"int foo()
{
    int i = 0;
    if (!1) return i;
    i++;
}", "Not all control flow paths in function foo return a value.");

    [Fact, NoVerify]
    public void MissedReturnInPositiveIfInvalid() => DoesNotCompile(
        @"int foo()
{
    int i = 0;
    if (1) i++;
    i++;
}", "Not all control flow paths in function foo return a value.");

    [Fact, NoVerify]
    public void MissedReturnAfterPositiveWhileValid() => ShouldCompile(
        @"int foo()
{
    int i = 0;
    while (1) return i;
    i++;
}");

    [Fact, NoVerify]
    public void MissedReturnInNegationWhileValid() => ShouldCompile(
        @"int foo()
{
    int i = 0;
    while (!1) i++;
    return i;
    i++;
}");

    [Fact, NoVerify]
    public void MissedReturnInPositiveWhileValid() => ShouldCompile(
        @"int foo()
{
    int i = 0;
    while (1) i++;
    i++;
}");

    [Fact, NoVerify]
    public void MissedReturnAfterNegationWhileInvalid() => DoesNotCompile(
        @"int foo()
{
    int i = 0;
    while (!1) return i;
    i++;
}", "Not all control flow paths in function foo return a value.");

    [Fact, NoVerify]
    public void MissedReturnAfterPositiveDoWhileValid() => ShouldCompile(
        @"int foo()
{
    int i = 0;
    do return i; while (1);
    i++;
}");

    [Fact, NoVerify]
    public void MissedReturnInNegationDoWhileValid() => ShouldCompile(
        @"int foo()
{
    int i = 0;
    do i++; while (!1);
    return i;
    i++;
}");

    [Fact, NoVerify]
    public void MissedReturnInPositiveDoWhileValid() => ShouldCompile(
        @"int foo()
{
    int i = 0;
    do i++; while (1);
    return i;
    i++;
}");

    [Fact, NoVerify]
    public void MissedReturnAfterNegationDoWhileValid() => ShouldCompile(
        @"int foo()
{
    int i = 0;
    do return i; while (-1);
    i++;
}");

    [Fact, NoVerify]
    public void MissedReturnAfterNegationDoWhileInvalid() => DoesNotCompile(
        @"int foo()
{
    int i = 0;
    do i++; while (!1);
    i++;
}", "Not all control flow paths in function foo return a value.");

    [Fact, NoVerify]
public void ElseIfChainAllReturns() => ShouldCompile(
    @"int foo(int x)
{
    if (x > 0) return 1;
    else if (x < 0) return -1;
    else return 0;
}");

[Fact, NoVerify]
public void ElseIfChainMissingReturnInElse() => DoesNotCompile(
    @"int foo(int x)
{
    if (x > 0) return 1;
    else if (x < 0) return -1;
    // else missing
}", "Not all control flow paths in function foo return a value.");

[Fact, NoVerify]
public void ElseIfChainMissingReturnInOneIf() => DoesNotCompile(
    @"int foo(int x)
{
    if (x > 0) return 1;
    else if (x < 0) { int y = x; }
    else return 0;
}", "Not all control flow paths in function foo return a value.");

[Fact, NoVerify]
public void NestedIfAllReturns() => ShouldCompile(
    @"int foo(int x)
{
    if (x > 0)
    {
        if (x < 10) return 1;
        else return 2;
    }
    else return 3;
}");

[Fact, NoVerify]
public void NestedIfMissingReturnInInnerElse() => DoesNotCompile(
    @"int foo(int x)
{
    if (x > 0)
    {
        if (x < 10) return 1;
        // else missing
    }
    else return 3;
}", "Not all control flow paths in function foo return a value.");

[Fact, NoVerify]
public void NestedIfMissingReturnInOuterElse() => DoesNotCompile(
    @"int foo(int x)
{
    if (x > 0)
    {
        if (x < 10) return 1;
        else return 2;
    }
    // outer else missing
}", "Not all control flow paths in function foo return a value.");

[Fact, NoVerify]
public void WhileWithBreakAndReturnAfter() => ShouldCompile(
    @"int foo(int x)
{
    int i = 0;
    while (i < x)
    {
        if (i == 5) break;
        i++;
    }
    return i;
}");

[Fact, NoVerify]
public void WhileWithBreakInsideIfWithoutReturnAfter() => DoesNotCompile(
    @"int foo(int x)
{
    int i = 0;
    while (i < x)
    {
        if (i == 5) break;
        i++;
    }
    // no return after loop
}", "Not all control flow paths in function foo return a value.");

[Fact, NoVerify]
public void WhileWithContinue() => ShouldCompile(
    @"int foo(int x)
{
    int i = 0;
    while (i < x)
    {
        i++;
        if (i == 5) continue;
        return i;
    }
    return 0;
}");

[Fact, NoVerify]
public void InfiniteLoopWithBreakAndReturnAfter() => ShouldCompile(
    @"int foo(int x)
{
    while (1)
    {
        if (x) break;
    }
    return 0;
}");

[Fact, NoVerify]
public void InfiniteLoopWithBreakAndNoReturnAfter() => DoesNotCompile(
    @"int foo(int x)
{
    while (1)
    {
        if (x) break;
    }
    // no return
}", "Not all control flow paths in function foo return a value.");

[Fact, NoVerify]
public void DoWhileWithBreakAndReturnAfter() => ShouldCompile(
    @"int foo(int x)
{
    int i = 0;
    do
    {
        if (i == 5) break;
        i++;
    } while (i < x);
    return i;
}");

[Fact, NoVerify]
public void DoWhileWithBreakInsideIfWithoutReturnAfter() => DoesNotCompile(
    @"int foo(int x)
{
    int i = 0;
    do
    {
        if (i == 5) break;
        i++;
    } while (i < x);
    // no return
}", "Not all control flow paths in function foo return a value.");

[Fact, NoVerify]
public void DoWhileWithReturnInBodyOnly() => ShouldCompile(
    @"int foo(int x)
{
    do { return 1; } while (x);
}");

[Fact, NoVerify]
public void IfInsideWhileWithReturnOnlyInIfBranch() => ShouldCompile(
    @"int foo(int x)
{
    while (x)
    {
        if (x > 0) return 1;
        x--;
    }
    return 0;
}");

[Fact, NoVerify]
public void IfInsideWhileWithMissingReturnInElseAndNoReturnAfterLoop() => DoesNotCompile(
    @"int foo(int x)
{
    while (x)
    {
        if (x > 0) return 1;
        // else does nothing, infinite loop if condition false
    }
}", "Not all control flow paths in function foo return a value.");

[Fact, NoVerify]
public void NestedLoopsWithReturnInInnerLoop() => ShouldCompile(
    @"int foo()
{
    int i = 0;
    while (i < 10)
    {
        int j = 0;
        while (j < 10)
        {
            if (i + j > 5) return i + j;
            j++;
        }
        i++;
    }
    return -1;
}");

[Fact, NoVerify]
public void NestedLoopsMissingReturnAfterOuterLoop() => DoesNotCompile(
    @"int foo()
{
    int i = 0;
    while (i < 10)
    {
        int j = 0;
        while (j < 10)
        {
            if (i + j > 5) return i + j;
            j++;
        }
        i++;
    }
    // no return
}", "Not all control flow paths in function foo return a value.");

[Fact, NoVerify]
public void WhileNeverExecutesWithReturnAfter() => ShouldCompile(
    @"int foo(int x)
{
    while (x) { return 1; }
    return 0;
}");

[Fact, NoVerify]
public void WhileNeverExecutesWithoutReturnAfter() => DoesNotCompile(
    @"int foo(int x)
{
    while (x) { return 1; }
    // no return after
}", "Not all control flow paths in function foo return a value.");

[Fact, NoVerify]
public void FunctionWithGotoAndReturn() => ShouldCompile(
    @"int foo(int x)
{
    if (x) goto a;
    return 0;
a:
    return 1;
}");

[Fact, NoVerify]
public void FunctionWithGotoMissingReturn() => DoesNotCompile(
    @"int foo(int x)
{
    if (x) goto a;
    goto b; // a way without return
a:
    return 1;
b:
    ; // empty
}", "Not all control flow paths in function foo return a value.");

[Fact, NoVerify]
public void FunctionWithGotoCycleAndReturn() => ShouldCompile(
    @"int foo(int x)
{
a:
    if (x) goto a;
    return 0;
}");

[Fact, NoVerify]
public void FunctionWithGotoCycleNoReturn() => DoesNotCompile(
    @"int foo(int x)
{
a:
    if (x) goto a;
    // no return
}", "Not all control flow paths in function foo return a value.");

[Fact, NoVerify]
public void FunctionWithNestedConstConditions() => ShouldCompile(
    @"int foo()
{
    if (1)
    {
        if (0) return 1;
        else return 2;
    }
    else { }; // unreachable
}");

[Fact, NoVerify]
public void FunctionWithSwitchLikeGoto() => ShouldCompile(
    @"int foo(int x)
{
    if (x) goto case1;
    else goto case2;
case1:
    return 1;
case2:
    return 2;
}");

[Fact, NoVerify]
public void FunctionWithSwitchLikeGotoMissingReturn() => DoesNotCompile(
    @"int foo(int x)
{
    if (x) goto case1;
    else goto case2;
case1:
    return 1;
case2:
    ; // empty
}", "Not all control flow paths in function foo return a value.");


[Fact, NoVerify]
public void FunctionWithComplexLoopAndContinueNoReturnAfterLoop() => DoesNotCompile(
    @"int foo(int x)
{
    int i = 0;
    while (i < x)
    {
        i++;
        if (i == 5) continue;
        return i;
    }
}", "Not all control flow paths in function foo return a value.");

[Fact, NoVerify]
public void FunctionWithReturnOnlyInLoopButLoopMayNotExecute() => DoesNotCompile(
    @"int foo(int x)
{
    while (x)
    {
        return 1;
    }
    // return is missing
}", "Not all control flow paths in function foo return a value.");

}
