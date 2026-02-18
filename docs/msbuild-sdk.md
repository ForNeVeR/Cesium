<!--
SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>

SPDX-License-Identifier: MIT
-->

Cesium MSBuild Project SDK
--------------------------

Cesium provides its own project SDK that could be used to simplify building of Cesium programs and libraries.

Cesium MSBuild SDK inherits default behavior from a `Microsoft.NET.Sdk` SDK and tries to integrate with it the same way as C# does.

[This page is a reference][MSBuild_props] for the MSBuild properties and items that you can use to configure .NET projects.

Cesium MSBuild SDK only supports SDK-style projects.

> Note: Some of the common MSBuild properties and items those are not stated in this document could be also used in Cesium project files. Not all of them are tested so something may not work as expected.

### Source files
By default, SDK includes all the `.c` and `.h` files from the project directory as `<Compile>` items. (While `.h` files aren't really the direct compiler inputs, but they are included for proper incremental compilation support, so that the assembly is recompiled on any header file change.)

In cases when this is unwanted, add an element `<EnableDefaultItems>false</EnableDefaultItems>` into any `<PropertyGroup>` in your project file.

If the default items are disabled, then you'll need to manage the inputs manuallt. In particular, the source files might be defined with `<Compile>` items, very similar to other .NET languages:
```xml
<ItemGroup>
    <Compile Include="hello.c" />
    <Compile Include="folder/*.c" />
</ItemGroup>
```

### References

#### Packages
Not supported yet.

#### Projects
Not supported yet.

#### Assemblies
Not supported yet.

### Preprocessor directives
`<DefineConstants>` property is directly mapped to a list of preprocessor items. So, you could define such constants in `.ceproj`:
```xml
<PropertyGroup>
    <DefineConstants>$(DefineConstants);FOO;BAR</DefineConstants>
</PropertyGroup>
```

And then use it in your .c code:
```c
#ifdef FOO
int foo() { return 0; }
#endif

#ifdef BAR
int bar() { return 1; }
#endif
```

### Output files
Output assembly and additional artifacts will be placed in `bin` folder. Depending on the target framework, output type and a platform you're compiling on, `.runtimeconfig.json` and `.deps.json` files will also be generated.

### Properties
- `SkipCesiumCompilerInstallation`: if set to `true`, doesn't automatically install a compiler bundle package. In that case it should be explicitly provided by `CesiumCompilerPackageName` and `CesiumCompilerPackageVersion` properties. Default: `false`
- `SkipCesiumRuntimeInstallation`: if set to `true`, doesn't automatically install a `Cesium.Runtime` package. In that case it should be explicitly installed. Default: `false`
- `CesiumCompilerPackageName`: an optional platform-specific compiler bundle package name. Should be specified if `SkipCesiumCompilerInstallation` set to `true`. Default: `Cesium.Compiler.Pack.{RID}`
- `CesiumCompilerPackageVersion`: an optional platform-specific compiler bundle package version. Should be specified if `SkipCesiumCompilerInstallation` set to `true`. Default: Cesium SDK version
- `CesiumCompilerPath`: an optional path to compiler executable. Use this property to specify a path to the compiler not coming from a compiler package.
- `CesiumCompilerRuntime`: path to the runtime executable used to run the compiler (for the compiler in a `.dll` file). `dotnet` by default.
- `CesiumCoreLibAssemblyPath`: an optional path to .NET runtime assembly: `System.Runtime` or `mscorlib`, depending on the target framework.

### Items
- `Compile`: a C source file to be included into compiler execution command

[MSBuild_props]: https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props