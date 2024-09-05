using Cesium.TestFramework;
using JetBrains.Annotations;

namespace Cesium.CodeGen.Tests;

public class CodeGenPointersTests : CodeGenTestBase
{
    [MustUseReturnValue]
    private static Task DoTest(string source)
    {
        var assembly = GenerateAssembly(default, source);

        var moduleType = assembly.Modules.Single().GetType("<Module>");
        return VerifyMethods(moduleType);
    }

    [Fact]
    public Task AddressOfTest() => DoTest("int main() { int x; int *y = &x; }");

    [Fact]
    public Task IndirectionGetTest() => DoTest("int foo (int *x) { return *x; }");

    [Fact]
    public Task AddToPointerFromRight() => DoTest("void foo (int *x) { x = x+1; }");

    [Fact]
    public Task AddToPointerFromLeft() => DoTest("void foo (int *x) { x = 1+x; }");

    [Fact]
    public Task IncrementPrimitivePointer() => DoTest("void foo (int *x) { x++; }");

    [Fact]
    public Task IncrementStructPointer() => DoTest("struct test { char* x; int y; } ; void foo () { const struct test *x; x++; }");

    [Fact]
    public Task IndexOverPointer() => DoTest("void foo (int *x) { int y = x[1]; }");

    [Fact]
    public Task PointerToPointer() => DoTest("void foo (int **x) { int y = *x[1]; }");

    [Fact]
    public Task ConstPointerToPointer() => DoTest("void foo (const int **x) { int y = *x[1]; }");

    [Fact]
    public Task PointerToConstPointer() => DoTest("void foo (int *const *x) { int y = *x[1]; }");

    [Fact, NoVerify]
    public void CannotMultiplyPointerTypes() => DoesNotCompile(
        "void foo (int *x) { x = 1*x; }",
        "Operator Multiply is not supported for value/pointer operands");

    [Fact, NoVerify]
    public void CannotAddPointerTypes() => DoesNotCompile(
        "void foo (int *x) { x = x +x; }",
        "Operator Add is not supported for pointer/pointer operands");

    [Fact]
    public Task CanSubtractPointerTypes() => DoTest("void foo (int *x, int *y) { int d = x - y; }");

    [Fact]
    public Task CanSubtractPointerWithConstTypes() => DoTest("void foo (int *x, const int *y) { int d = x - y; }");

    [Fact]
    public Task CanUseBuiltinOffsetOfOnDeclaredStruct() => DoTest(
        "typedef struct { int x; } a; int m() { return &__builtin_offsetof_instance((a*) 0).x; }"
    );

    [Fact]
    public Task CanUseBuiltinOffsetOfOnTaggedStruct() => DoTest(
        "struct a { int x; }; int m() { return &__builtin_offsetof_instance((struct a*) 0).x; }"
    );

    [Fact]
    public Task CanUseBuiltinOffsetOfOnBothDeclaredAndStruct() => DoTest(
        "typedef struct a_s { int x; } a_t; int m() { return (int)&__builtin_offsetof_instance((a_t*)0).x + (int)&__builtin_offsetof_instance((struct a_s*) 0).x; }"
    );

    [Fact]
    public Task CanUseBuiltinOffsetOfOnDeepMembers() => DoTest(
        "typedef struct { int x; } a; typedef struct { a a; } b; int m() { return &__builtin_offsetof_instance((b*)0).a.x; }"
    );

    [Fact, NoVerify]
    public void CannotUseBuiltinOffsetOfOnInvalidMembers() => DoesNotCompile(
        "typedef struct { int x; } a; int m() { return &__builtin_offsetof_instance((a*)0).y; }",
        "\"a\" has no member named \"y\""
    );

    [Fact, NoVerify]
    public void CannotUseBuiltinOffsetOfOnPlainType() => DoesNotCompile(
        "int m() { return &__builtin_offsetof_instance((int*)0).x; }",
        "__builtin_offsetof_instance: type \"PrimitiveType { Kind = Int }\" is not a struct type."
    );

    [Fact, NoVerify]
    public void CannotUseBuiltinOffsetOfOnPointerType() => DoesNotCompile(
        "typedef struct { int x; } a; int m() { return &__builtin_offsetof_instance((a**)0).x; }",
        "__builtin_offsetof_instance: type \"Cesium.CodeGen.Ir.Types.PointerType\" is not a struct type."
    );

    [Fact, NoVerify]
    public void CannotUseBuiltinOffsetOfOnUndeclaredType() => DoesNotCompile(
        "int m() { return &__builtin_offsetof_instance((undeclared*) 0).x; }",
        "Cannot resolve type undeclared"
    );

    [Fact, NoVerify]
    public void CannotUseBuiltinOffsetOfOnUndeclaredTag() => DoesNotCompile(
        "int m() { return &__builtin_offsetof_instance((struct undeclared*) 0).x; }",
        "__builtin_offsetof_instance: struct type \"undeclared\" has no members - is it declared?"
    );

    /* TODO[#390]: Tests below rely on preprocessor, which is not supported in tests now

    [Fact]
    public Task CanUseOffsetOfOnDeclaredStruct() => DoTest(
        "#include <stddef.h>\ntypedef struct { int x; } a; int m() { return offsetof(a, x); }"
    );

    [Fact]
    public Task CanUseOffsetOfOnTaggedStruct() => DoTest(
        "#include <stddef.h>\nstruct a { int x; }; int m() { return offsetof(struct a, x); }"
    );

    [Fact]
    public Task CanUseOffsetOfOnBothDeclaredAndStruct() => DoTest(
        "#include <stddef.h>\ntypedef struct a_s { int x; } a_t; int m() { return offsetof(a_t, x) + offsetof(struct a_s, x); }"
    );

    [Fact]
    public Task CanUseOffsetOfOnDeepMembers() => DoTest(
        "#include <stddef.h>\ntypedef struct { int x; } a; typedef struct { a a; } b; int m() { return offsetof(b, a.x); }"
    );

    [Fact]
    public void CannotUseOffsetOfOnInvalidMembers() => DoesNotCompile(
        "#include <stddef.h>\ntypedef struct { int x; } a; int m() { return offsetof(a, y); }",
        "asd"
    );

    [Fact]
    public void CannotUseOffsetOfOnPlainType() => DoesNotCompile(
        "#include <stddef.h>\nint m() { return offsetof(int, x); }",
        ""
    );

    [Fact]
    public void CannotUseOffsetOfOnPointerType() => DoesNotCompile(
        "#include <stddef.h>\ntypedef struct { int x; } a; int m() { return offsetof(a*, x); }",
        ""
    );

    [Fact]
    public void CannotUseOffsetOfOnUndeclaredType() => DoesNotCompile(
        "#include <stddef.h>\nint m() { return offsetof(undeclared, x); }",
        ""
    );

    [Fact]
    public void CannotUseOffsetOfOnUndeclaredTag() => DoesNotCompile(
        "#include <stddef.h>\nint m() { return offsetof(struct undeclared, x); }",
        ""
    );

    */
}
