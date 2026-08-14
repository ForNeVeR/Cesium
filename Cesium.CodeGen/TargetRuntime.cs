// SPDX-FileCopyrightText: 2025 Cesium contributors <https://github.com/ForNeVeR/Cesium>
//
// SPDX-License-Identifier: MIT

using Cesium.CodeGen.Contexts;
using Cesium.Core;
using Mono.Cecil;

namespace Cesium.CodeGen;

public enum SystemAssemblyKind
{
    MsCorLib,
    SystemRuntime,
    NetStandard,
}

public record TargetRuntimeDescriptor(
    SystemAssemblyKind Kind,
    Version SystemLibraryVersion,
    Version TargetFrameworkVersion)
{
    public static readonly TargetRuntimeDescriptor Net60 = new(
        SystemAssemblyKind.SystemRuntime,
        Version.Parse("6.0"),
        Version.Parse("6.0"));

    public static readonly TargetRuntimeDescriptor NetStandard20 = new(
        SystemAssemblyKind.NetStandard,
        Version.Parse("2.0"),
        Version.Parse("2.0"));

    public static readonly TargetRuntimeDescriptor Net48 = new(
        SystemAssemblyKind.NetStandard,
        Version.Parse("4.0"),
        Version.Parse("4.8"));

    public AssemblyNameReference GetSystemAssemblyReference()
    {
        var (assemblyName, publicKeyToken) = Kind switch
        {
            SystemAssemblyKind.MsCorLib =>
                ("mscorlib", new byte[] { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 }),
            SystemAssemblyKind.SystemRuntime =>
                ("System.Runtime", new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a }),
            SystemAssemblyKind.NetStandard =>
                ("netstandard", new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a }),
            _ => throw new CompilationException($"Unknown assembly kind: {Kind}")
        };
        return new AssemblyNameReference(assemblyName, SystemLibraryVersion)
        {
            PublicKeyToken = publicKeyToken
        };
    }

    public CustomAttribute GetTargetFrameworkAttribute(AssemblyContext context)
    {
        var frameworkName = Kind switch
        {
            SystemAssemblyKind.MsCorLib => ".NETFramework",
            SystemAssemblyKind.SystemRuntime => ".NETCoreApp",
            SystemAssemblyKind.NetStandard => ".NETStandard",
            _ => throw new CompilationException($"Unknown target runtime kind: {Kind}")
        } + $",Version=v{TargetFrameworkVersion}";

        var targetFrameworkAttributeRef = context.Module.ImportReference(new TypeReference("System.Runtime.Versioning", "TargetFrameworkAttribute", context.MscorlibAssembly.MainModule, context.MscorlibAssembly.MainModule));
        var constructorRef = new MethodReference(".ctor", context.Module.TypeSystem.Void, targetFrameworkAttributeRef);
        constructorRef.Parameters.Add(new ParameterDefinition(context.Module.TypeSystem.String));
        constructorRef = context.Module.ImportReference(constructorRef);
        return new CustomAttribute(constructorRef)
        {
            ConstructorArguments = { new CustomAttributeArgument(context.Module.TypeSystem.String, frameworkName) }
        };
    }
}
