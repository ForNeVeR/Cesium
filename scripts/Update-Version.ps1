# SPDX-FileCopyrightText: 2025 Friedrich von Never <friedrich@fornever.me>
#
# SPDX-License-Identifier: MIT

param (
    $NewVersion = '0.1.2',
    $RepoRoot = "$PSScriptRoot/.."
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Update-PowerShellFile($relativePath) {
    $file = Resolve-Path "$RepoRoot/$relativePath"
    $oldContent = [IO.File]::ReadAllText($file)
    $newContent = $oldContent -replace "\`$NewVersion = '[\d.]*?'", "`$NewVersion = '$NewVersion'"
    [IO.File]::WriteAllText($file, $newContent)
    Write-Output "Updated file `"$file`"."
}

function Update-PropsFile($relativePath, $propName) {
    $file = Resolve-Path "$RepoRoot/$relativePath"

    # NOTE: I really tried to play nice and load the file via [xml], but didn't found a way to preserve the formatting.
    #       PreserveWhitespace = $true helps a lot, but still doesn't preserve the line breaks between attributes on the
    #       same node.
    $oldContent = [IO.File]::ReadAllText($file)
    $regex = [Regex]::Escape($propName)
    $newContent = $oldContent -replace "<($regex)( ?.*?)>.*?</$regex>", "<`$1`$2>$NewVersion</`$1>"
    [IO.File]::WriteAllText($file, $newContent)
    Write-Output "Updated file `"$file`"."
}

function Update-TemplateJson($relativePath, $symbolName) {
    Get-ChildItem -Recurse "$RepoRoot/$relativePath" | ForEach-Object {
        $file = $_.FullName
        $found = $false

        $content = Get-Content $file | ConvertFrom-Json
        $symbol = $content.symbols.$symbolName
        if ($symbol) {
            $symbol.parameters.value = $NewVersion
            $found = $true
        }

        if (!$found) {
            throw "Cannot find symbol $symbolName in file `"$file`"."
        }

        $content = ($content | ConvertTo-Json -Depth 4) -replace '  ', '    ' # 4-space indent
        [IO.File]::WriteAllText($file, $content + "`n")
        Write-Output "Updated file `"$file`"."
    }
}

Update-PowerShellFile 'scripts/Update-Version.ps1'
Update-PropsFile 'Directory.Build.props' 'VersionPrefix'
Update-PropsFile 'Cesium.Sdk/Sdk/Sdk.props' 'CesiumCompilerPackageVersion'
Update-TemplateJson '**/template.json' 'CesiumVersion'
