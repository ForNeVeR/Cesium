# SPDX-FileCopyrightText: 2024-2025 Friedrich von Never <friedrich@fornever.me>
#
# SPDX-License-Identifier: MIT

param(
    [string] $RefName,
    [string] $RepositoryRoot = "$PSScriptRoot/.."
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Write-Host "Determining version from ref `"$RefName`"…"
if ($RefName -match '^refs/tags/v') {
    $version = $RefName -replace '^refs/tags/v', ''
    Write-Host "Pushed ref is a version tag, version: $version"
} else {
    $propsFilePath = "$RepositoryRoot/Directory.Build.props"
    [xml] $props = Get-Content $propsFilePath
    foreach ($group in $props.Project.PropertyGroup) {
        if ($group.Label -eq 'Versioning') {
            $version = $group.VersionPrefix
            break
        }
    }
    Write-Host "Pushed ref is a not version tag, got version from $($propsFilePath): $version"
}

Write-Output $version
