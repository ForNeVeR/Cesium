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

File Encoding Changes
---------------------
If the automation asks you to update the file encoding (line endings or UTF-8 BOM) in certain files, run the following PowerShell script ([PowerShell Core][powershell] is recommended to run this script):
```console
$ pwsh -File scripts/Test-Encoding.ps1 -AutoFix
```

The `-AutoFix` switch will automatically fix the encoding issues, and you'll only need to commit and push the changes.

[docs.tests]: docs/tests.md
[dotnet.download]: https://dotnet.microsoft.com/en-us/download
[powershell]: https://github.com/PowerShell/PowerShell
