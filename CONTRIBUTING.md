<!--
SPDX-FileCopyrightText: 2021-2026 Cesium contributors <https://github.com/ForNeVeR/Cesium>

SPDX-License-Identifier: MIT
-->

Contributor Guide
=================

Prerequisites
-------------
To work on the Cesium solution, install the latest [.NET SDK][dotnet.download] (currently .NET 10).

To execute integration tests on **Windows**, the following components must be installed via **Visual Studio Installer** (either as **individual components** or as part of the **Desktop development with C++** workload):
* VC++ compiler (**MSVC Build Tools for x64/x86, latest version**)
* Windows 10 SDK (**Windows 11 SDK** also works)

Building
--------
To build the compiler executable, run the following shell command:
```console
$ dotnet build
```

Running
-------
To run the compiler from sources directly, execute the following shell command:

```console
$ dotnet run --project Cesium.Compiler -- [compiler arguments go here]
```

Read more about the compiler arguments in [the README document][docs.readme].

Testing
-------

**Want to add new tests to Cesium?** Read a separate [document on tests][docs.tests].

There are two kinds of tests in Cesium: unit tests and integration tests.

Run the unit and integration tests using this shell command:

```console
$ dotnet restore
$ dotnet nuke TestAll
```

Testing Templates
-------
If you want to test changes in **Cesium.Templates** locally, you can install the templates directly from the Cesium folder.

```console
$ cd <PATH_TO_CESIUM>/Cesium
$ dotnet new install Cesium.Templates
```
This installs the templates locally and lets you use template changes without going through the NuGet cache.
More info: https://learn.microsoft.com/en-us/dotnet/core/install/templates?pivots=os-windows#folder

Publishing
----------
To produce a standalone compiler executable, run the following shell command:

```console
$ dotnet nuke PackAllCompilerRuntimeSpecificBundles --configuration release
```

This will prepare runtime-specific ZIP archives in the `artifacts/package/release` folder.

File Encoding Changes
---------------------
If the automation asks you to update the file encoding (line endings or UTF-8 BOM) in certain files, run the following PowerShell script ([PowerShell Core][powershell] is recommended to run this script):
```console
$ pwsh -File scripts/Test-Encoding.ps1 -AutoFix
```

The `-AutoFix` switch will automatically fix the encoding issues, and you'll only need to commit and push the changes.

License Automation
------------------
<!-- REUSE-IgnoreStart -->
If the CI asks you to update the file licenses, follow one of these:
1. Update the headers manually (look at the existing files), something like this:
   ```
   // SPDX-FileCopyrightText: %year% %your name% <%your contact info, e.g. email%>
   //
   // SPDX-License-Identifier: MIT
   ```
   (accommodate to the file's comment style if required).
2. Alternately, use [REUSE][reuse] tool:
   ```console
   $ reuse annotate --license MIT --copyright '%your name% <%your contact info, e.g. email%>' %file names to annotate%
   ```

(Feel free to attribute the changes to "Cesium contributors <https://github.com/ForNeVeR/Cesium>" instead of your name in a multi-author file, or if you don't want your name to be mentioned in the project's source: this doesn't mean you'll lose the copyright.)
<!-- REUSE-IgnoreEnd -->

[docs.readme]: README.md
[docs.tests]: docs/tests.md
[dotnet.download]: https://dotnet.microsoft.com/en-us/download
[powershell]: https://github.com/PowerShell/PowerShell
[reuse]: https://reuse.software/
