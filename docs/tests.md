Cesium Tests
============

Being a compiler, Cesium requires a complicated test suite checking every feature.

There are two kinds of tests in Cesium: unit tests (directly calling various internal APIs in the compiler) and integration tests (interacting with the compiler executable and comparing the resulting programs' behavior with programs compiled by other compilers).

Unit Tests
----------
Unit tests in Cesium are normal .NET tests, so they are runnable by the following shell command:
s
```console
$ dotnet test
```

There are two kind of unit tests: a few of "the real" unit tests (e.g. `Cesium.Parser.Tests.LexerTests.IdentifierTests`) and a set of [characterization tests][wiki.characterization-tests]. The real unit tests verify certain facts using assertions of the Xunit testing framework, but their usage in the compiler is small. The characterization tests, on the other hand, invoke parts of the compiler on various sources, and then dump the results (e.g. a full parse tree of a code fragment, or a whole compiled assembly).

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
For now, there are no unit tests that try to run the compiled programs. So, the existing unit tests cannot control the behavior of the programs produced by the compiler, and this task is solved by the integration tests.

The integration test suite takes a set of valid C programs, compiles them using several compilers, including Cesium, and then compares the programs' behaviors (their exit codes and the standard output contents). This allows us to catch differences between Cesium-compiled programs and the same programs compiled by other compilers.

Currently, the following compilers are used by the integration test suite:
- Cesium (all platforms)
- Visual Studio's `cl.exe` (Windows)
- `gcc` (Linux)

To run the integration test suite, use the following shell command:

```console
$ pwsh ./Cesium.IntegrationTests/Run-Tests.ps1
```

On Windows, it's recommended to run the script from the Visual Studio's Developer Command Prompt (though you can just add a directory containing `cl.exe` to your `PATH` environment variable).

Consult the script documentation block for various options. The most important ones are:
- `-NoBuild` will disable the automatic compiler rebuild before running the tests (useful if you have already built the compiler)
- `-TestCaseName <file_name.c>` will run only one test: `file_name.c` (useful when you debug a particular test)

**To add a new integration test**, just put a `.c` file into the `Cesium.IntegrationTests` directory, and then run the `Run-Tests.ps1` script to make sure your new test works.

[wiki.characterization-tests]: https://en.wikipedia.org/wiki/Characterization_test

