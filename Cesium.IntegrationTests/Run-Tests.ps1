# TODO: Remove this file
param (
    $SourceRoot = "$PSScriptRoot/..",
    $OutDir = "$PSScriptRoot/bin",
    $ObjDir = "$PSScriptRoot/obj",
    $TestCaseDir = "$PSScriptRoot",
    $TargetFramework = "Net",
    $Configuration = "Release",
    $TestCaseName = $null
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function buildFileWithNativeCompiler($inputFile, $outputFile) {
    if ($IsWindows) {
        Write-Host "Compiling $inputFile with cl.exe."
        cl.exe /nologo $inputFile -D__TEST_DEFINE /Fo:$ObjDir/ /Fe:$outputFile | Out-Host
    } else {
        Write-Host "Compiling $inputFile with gcc."
        gcc $inputFile -o $outputFile -D__TEST_DEFINE | Out-Host
    }

    if (!$?) {
        Write-Host "Error: native compiler returned exit code $LASTEXITCODE."
        return $false
    }

    return $true
}

function buildFileWithCesium($inputFile, $outputFile) {
    # $env:Platform will override the output directory for dotnet run, so let's remove it temporarily.
    $oldPlatform = $env:Platform
    $env:Platform = $null
    try {
        Write-Host "Compiling $inputFile with Cesium."
        if ($TargetFramework -eq "NetFramework")
        {
            $CoreLib = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\mscorlib.dll"
            $CesiumRuntime = "$SourceRoot/Cesium.Runtime/bin/$Configuration/netstandard2.0/Cesium.Runtime.dll"
            dotnet run --no-build --configuration $Configuration --project "$SourceRoot/Cesium.Compiler" -- --nologo $inputFile --out $outputFile -D__TEST_DEFINE --framework $TargetFramework --corelib $CoreLib --runtime $CesiumRuntime | Out-Host
        }
        else
        {
            dotnet run --no-build --configuration $Configuration --project "$SourceRoot/Cesium.Compiler" -- --nologo $inputFile --out $outputFile -D__TEST_DEFINE --framework $TargetFramework | Out-Host
        }

        if (!$?) {
            Write-Host "Error: Cesium.Compiler returned exit code $LASTEXITCODE."
            return $false
        }

        return $true
    } finally {
        $env:Platform = $oldPlatform
    }
}

function validateTestCase($testCase) {
    $nativeCompilerBinOutput = "$outDir/out_native.exe"
    $cesiumBinOutput = "$outDir/out_cs.exe"

    $nativeCompilerRunLog = "$outDir/out_native.log"
    $cesiumRunLog = "$outDir/out_cs.log"

    $expectedExitCode = 42

    if (!(buildFileWithNativeCompiler $testCase $nativeCompilerBinOutput)) {
        return $false
    }

    & $nativeCompilerBinOutput | Out-File -Encoding utf8 $nativeCompilerRunLog
    if ($LASTEXITCODE -ne $expectedExitCode) {
        Write-Host "Binary $nativeCompilerBinOutput returned code $LASTEXITCODE, but $expectedExitCode was expected."
        return $false
    }

    if (!(buildFileWithCesium $testCase $cesiumBinOutput)) {
        return $false
    }

    if ($TargetFramework -eq "NetFramework")
    {
        & $cesiumBinOutput | Out-File -Encoding utf8 $cesiumRunLog
    }
    else
    {
        dotnet $cesiumBinOutput | Out-File -Encoding utf8 $cesiumRunLog
    }

    if ($LASTEXITCODE -ne $expectedExitCode) {
        Write-Host "Binary $cesiumBinOutput returned code $LASTEXITCODE, but $expectedExitCode was expected."
        return $false
    }

    $nativeCompilerOutput = Get-Content -LiteralPath $nativeCompilerRunLog -Raw
    $cesiumOutput = Get-Content -LiteralPath $cesiumRunLog -Raw
    if ($nativeCompilerOutput -ne $cesiumOutput) {
        Write-Host "Output for $testCase differs between native- and Cesium-compiled programs."
        Write-Host "cl.exe ($testCase):`n$nativeCompilerOutput`n"
        Write-Host "Cesium ($testCase):`n$cesiumOutput"
        return $false
    }

    $true
}

function formatCount($count, $singular, $plural) {
    if ($count -eq 1) {
        return "$count $singular"
    } else {
        return "$count $plural"
    }
}

Write-Host "Cleaning up $ObjDir and $OutDir."
if (Test-Path $ObjDir) {
    Remove-Item -Recurse $ObjDir
}
if (Test-Path $OutDir) {
    Remove-Item -Recurse $OutDir
}

New-Item $ObjDir -Type Directory | Out-Null
New-Item $OutDir -Type Directory | Out-Null

$successfulTests = @()
$failedTests = @()
if ($TestCaseName) {
    Write-Host "Running tests for single case $TestCaseName."
    $testCase = "$TestCaseDir/$TestCaseName"
    if (validateTestCase $testCase) {
        Write-Host "$($testCase): ok."
        $successfulTests += $testCase
    } else {
        Write-Host "$($testCase): failed."
        $failedTests += $testCase
    }
} else {
    $allTestCases = Get-ChildItem "$TestCaseDir/*.c" -Exclude "*.ignore.c" -Recurse
    Write-Host -ForegroundColor White "Running tests for $($allTestCases.Count) cases."
    foreach ($testCase in $allTestCases) {
        $currentTestName = [IO.Path]::GetRelativePath($TestCaseDir, $testCase)
        Write-Host -ForegroundColor White "# $currentTestName"

        if (validateTestCase $testCase) {
            Write-Host -ForegroundColor Green "$($currentTestName): ok."
            $successfulTests += $testCase
        } else {
            Write-Host -ForegroundColor Red "$($currentTestName): failed."
            $failedTests += $testCase
        }

        Write-Host ''
    }
}

if ($failedTests.Count -gt 0) {
    $testNames = $failedTests -join "`n"
    throw "Errors in the following $(formatCount $failedTests.Count "test" "tests"): $testNames"
} else {
    Write-Host -ForegroundColor Green "$(formatCount $successfulTests.Count "test has" "tests have") been executed successfully."
}
