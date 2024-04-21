Cesium Tests
============

Being a compiler, Cesium requires a complicated test suite checking every feature.

There are three kinds of tests in Cesium: unit tests (directly calling various internal APIs in the compiler), integration tests (interacting with the compiler executable and comparing the resulting programs' behavior with programs compiled by other compilers) and SDK tests (testing integration with MSBuild via MSBuild project SDK).

Running Tests
-------------
To run all tests from solution, make sure to restore locally installed tools:
```console
dotnet tool restore
```

Then, run `TestAll` target using NUKE:
```console
dotnet nuke TestAll
```

You could also execute test from specific corresponding test projects:
- `dotnet nuke TestParser`
- `dotnet nuke TestCompiler`
- `dotnet nuke TestCodeGen`
- `dotnet nuke TestRuntime`
- `dotnet nuke TestIntegration`
- `dotnet nuke TestSdk`

Unit Tests
----------
Unit tests in Cesium are normal .NET tests.

There are two kinds of unit tests: a few of "the real" unit tests (e.g. `Cesium.Parser.Tests.LexerTests.IdentifierTests`) and a set of [characterization tests][wiki.characterization-tests]. The real unit tests verify certain facts using assertions of the Xunit testing framework, but their usage in the compiler is small. The characterization tests, on the other hand, invoke parts of the compiler on various sources, and then dump the results (e.g. a full parse tree of a code fragment, or a whole compiled assembly).

The characterization tests comprise the biggest part of the compiler test suite, and it helps us to thoroughly verify the most aspects of the compiler behavior.

To write a new unit test, then see the following test classes as examples:
- lexer test: `Cesium.Parser.Tests.LexerTests.LexerTests`
- parser test: `Cesium.Parser.Tests.ParserTests.FullParserTests`
- preprocessor parsing test: `Cesium.Parser.Tests.PreprocessorTests.PreprocessorTests`
- code generator test: `Cesium.CodeGen.Tests.CodeGenTypeTests`

Just add a new method to one of the existing test classes or write a new test class similar to the existing ones.

If you add a new characterization test, it will fail, because there's no "gold output" generated for it, yet. After the first run of the test, or in a situation when you changed output for a lot of tests (say, added a byte code optimization), regenerate the gold output set using the following shell command:

```console
$ pwsh -c ./scripts/approve-all.ps1
```

Integration Tests
-----------------
Actual execution of the programs compiled by Cesium is controlled by the integration tests.

There are two categories of integration tests: .NET interop tests and compiler verification.

1. .NET interop tests compile a C# program using Roslyn and then a C program referencing the C# one, and then run the program. This helps to verify certain .NET API interop aspects on different architectures.

   **To add a new .NET interop test**, see the `Cesium.CodeGen.Tests.CodeGenNetInteropTests` class.
2. The compiler verification test suite takes a set of valid C programs, compiles them using several compilers, including Cesium, and then compares the programs' behaviors (their exit codes and the standard output contents). This allows us to catch differences between Cesium-compiled programs and the same programs compiled by other compilers.

   Currently, the following compilers are used by the integration test suite:
   - Cesium (all platforms)
   - Visual Studio's `cl.exe` (Windows)
   - `gcc` (Linux)

   These tests are controlled by the `Cesium.IntegrationTests` assembly that is a normal Xunit test project.

   **To add a new integration test**, just put a `.c` file into the `Cesium.IntegrationTests` directory, and then run the test suite locally to make sure your new test works.

[wiki.characterization-tests]: https://en.wikipedia.org/wiki/Characterization_test

SDK Tests
---------
#### How SDK tests work
SDK tests check correctness of integration with MSBuild. They are focused on build output, including artifacts existence asserting.

SDK tests (and Cesium Project SDK itself) require compiler bundle to be built and packed. A compiler bundle is a special platform-specific NuGet package containing a published compiler executable with dependencies. It is not intended to be used as a runtime dependency and only used while building project.

Compiler packing is done by 2 NUKE targets:
- `PublishCompilerBundle`: a target that make platform-specific `dotnet publish` of compiler bundle to corresponding artifacts' folder.
- `PackCompilerBundle`: a target that wraps a published compiler bundle into a NuGet package which is then used by SDK to deliver compiler to user's project

If you want to run these tests without Nuke (e.g. from the IDE), run the targets using a shell command:
```console
$ dotnet nuke PackCompilerBundle
```
After that, run the tests in your preferred way.

Both targets are called automatically when `TestSdk` target is invoked.

SDK itself should also be built to be used in test projects. This is done by dependent target `PackSdk` which produces `Cesium.Sdk` NuGet package, suitable as Project SDK.

Having all necessary dependencies, SDK tests are invoking `dotnet build` CLI with test projects, representing different layouts and configurations. Test result is determined by MSBuild output and artifacts presence.

#### Adding new tests
Adding new tests is quite straightforward.
1. Add a test project if needed to the `TestProjects` directory. All items from that folder will be automatically included into temporary test execution directory.
2. Write a test with the new test project in use. Look for the examples at `CesiumCompileTests.cs`.
