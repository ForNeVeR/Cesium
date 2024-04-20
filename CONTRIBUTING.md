Contributor Guide
=================

Building
--------

### Prerequisites

To build Cesium, install [.NET 8 SDK][dotnet.download] or later.

Testing
-------

**Want to add new tests to Cesium?** Read a separate [document on tests][docs.tests].

There are two kinds of tests in Cesium: unit tests and integration tests.

Run the unit and integration tests using this shell command:

```console
$ dotnet restore
$ dotnet nuke TestAll
```

Publishing
----------

To produce a standalone compiler executable, run the following shell command:

```shell
dotnet publish Cesium.Compiler/Cesium.Compiler.csproj -r win-x64 --self-contained
```

Then navigate to `Cesium.Compiler\bin\Debug\net7.0\win-x64\publish\` and that's your Cesium.

[docs.tests]: docs/tests.md
[dotnet.download]: https://dotnet.microsoft.com/en-us/download
[powershell]: https://github.com/PowerShell/PowerShell
