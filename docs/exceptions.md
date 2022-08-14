Exceptions in the Compiler Code
===============================

The compiler often needs to throw an exception to terminate processing of some code branches. This document describes the rules for throwing exceptions in the compiler code.

All the Cesium-produced exceptions are derived from `Cesium.Core.Exceptions.CesiumException`. Any other exceptions thrown from the compiler code should be treated as critical internal compiler exceptions.

- `Cesium.Core.Exceptions.PreprocessorException` should be thrown in cas of preprocessor-related errors.
- `Cesium.Core.Exceptions.ParseException` should be thrown if we were unable to parse something.
- `Cesium.Core.Exceptions.CompilationException` should be thrown if an error happened during compilation (i.e. a type or a variable was not found): this is most likely due to the user code being incorrect.
- `Cesium.Core.Exceptions.AssertException` is thrown when the compiler code failed its own assertions. Most likely, it means a bug in the compiler.
- `Cesium.Core.Exceptions.WipException` is an exception which gets thrown from areas where certain features aren't implemented, but they will sometime (ideally, at least). Every such exception should be linked with an issue on GitHub.

  When writing new code, please use `throw new WipException(WipException.ToDo, "some optional description")`. The project maintainers will convert these to the issues while merging your contribution.

  The names are designed to easily find any remaining work by performing a text search for the `TODO` word.
