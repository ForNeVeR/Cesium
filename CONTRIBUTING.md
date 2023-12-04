Contributor Guide
=================

Building
--------

### Prerequisites

To build Cesium, install [.NET 7 SDK][dotnet.download] or later.

Testing
-------

**Want to add new tests to Cesium?** Read a separate [document on tests][docs.tests].

There are two kinds of tests in Cesium: unit tests and integration tests.

Run the unit tests using this shell command:

```console
$ dotnet test
```

Run the integration tests using this shell command ([PowerShell][powershell] is required):

```console
$ pwsh -c ./Cesium.IntegrationTests/Run-Tests.ps1 -NoBuild
```

(don't pass `-NoBuild` if you want to automatically rebuild the compiler before running the integration tests)

If you debug integration tests and want to run just a single test, use this shell command:

```console
pwsh -c ./Cesium.IntegrationTests/Run-Tests.ps1 -TestCaseName quoted_include_fallback.c
```

where `quoted_include_fallback.c` is path within `Cesium.IntegrationTests` folder.

Publishing
----------

For producing standalone compiler executable run

```shell
dotnet publish Cesium.Compiler/Cesium.Compiler.csproj -r win-x64 --self-contained
```

Then navigate to `Cesium.Compiler\bin\Debug\net7.0\win-x64\publish\` and that's your Cesium.

[docs.tests]: docs/tests.md
[dotnet.download]: https://dotnet.microsoft.com/en-us/download
[powershell]: https://github.com/PowerShell/PowerShell
