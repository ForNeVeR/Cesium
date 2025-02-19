// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenEnumTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task EnumDeclarationTest() => DoTest(@"
enum Colour { Red, Green, Blue };

void test()
{
    enum Colour x = Green;
}");

    [Fact]
    public Task EnumDeclarationIntTest() => DoTest(@"
enum Colour { Red, Green, Blue };

void test()
{
    enum Colour x = 42;
}");

    [Fact]
    public Task EnumUsageInIf() => DoTest(@"
enum Colour { Red, Green, Blue };

void test()
{
    enum Colour x = 42;
    if (x == Green) ;
}");

    [Fact]
    public Task EnumViaTypeDef() => DoTest(@"
typedef enum { Red, Green, Blue } Colour;

void test()
{
    Colour x = 42;
    if (x == Green) ;
}");

    [Fact]
    public Task EnumInFuncionCall() => DoTest(@"
enum Colour { Red, Green, Blue };

void work(enum Colour){}
void test()
{
    work(Green);
}");

    [Fact]
    public Task EnumInCase() => DoTest(@"
enum Colour { Red, Green, Blue };

void test()
{
    enum Colour x = 42;
    switch (x) {
        case Blue:
            break;
    }
}");

    [Fact]
    public Task EnumValueInInitializer() => DoTest(@"
enum Colour { Red, Green, Blue };

void test()
{
    int x = {Green};
}");

    [Fact]
    public Task GlobalEnumValueInInitializer() => DoTest(@"
enum Colour { Red, Green, Blue };
int x = {Green};
");

    [Fact]
    public Task GlobalEnumValueInArrayInitializer() => DoTest(@"
enum Colour { Red, Green, Blue };
int x[] = {Green};
");

    [Fact]
    public Task EnumUseValuesInDeclaration() => DoTest(@"
enum Colour { Red, Green, Blue = Green + 10 };
int x = {Blue};
");

    [Fact]
    public Task EnumUseValuesInStructPointerDeclaration() => DoTest(@"
enum Colour { Red, Green, Blue = Green + 10 };
typedef struct  { int x; } TestStruct;
TestStruct *x = &(TestStruct){Blue};
");

    [Fact]
    public Task EnumUseValuesInStructDeclaration() => DoTest(@"
enum Colour { Red, Green, Blue = Green + 10 };
typedef struct  { int x; } TestStruct;
TestStruct x = {Blue};
");

    [Fact]
    public Task EnumInsideStructDeclaration() => DoTest(@"
typedef struct { enum Colour { Red, Green, Blue = Green + 10 } x; } TestStruct;
TestStruct x = {Blue};
");
}
