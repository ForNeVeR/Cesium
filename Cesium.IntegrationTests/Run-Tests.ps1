param (
    [switch] $NoBuild,

    $SourceRoot = "$PSScriptRoot/..",
    $OutDir = "$PSScriptRoot/bin",
    $ObjDir = "$PSScriptRoot/obj",
    $TestCaseDir = "$PSScriptRoot"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function buildCompiler() {
    Write-Host 'Building the Cesium.Compiler project.'
    dotnet build Cesium.Compiler/Cesium.Compiler.csproj
    if (!$?) {
        throw "Couldn't build the compiler: dotnet build returned $LASTEXITCODE."
    }
}

function buildFileWithNativeCompiler($inputFile, $outputFile) {
    if ($IsWindows) {
        Write-Host "Compiling $inputFile with cl.exe."
        cl.exe /nologo $inputFile /Fo:$ObjDir/ /Fe:$outputFile | Out-Host
    } else {
        Write-Host "Compiling $inputFile with gcc."
        gcc $inputFile -o $outputFile | Out-Host
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
        dotnet run --no-build --project "$SourceRoot/Cesium.Compiler" -- $inputFile $outputFile | Out-Host
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

    dotnet $cesiumBinOutput | Out-File -Encoding utf8 $cesiumRunLog
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

Write-Host "Cleaning up $ObjDir and $OutDir."
if (Test-Path $ObjDir) {
    Remove-Item -Recurse $ObjDir
}
if (Test-Path $OutDir) {
    Remove-Item -Recurse $OutDir
}

New-Item $ObjDir -Type Directory | Out-Null
New-Item $OutDir -Type Directory | Out-Null

$allTestCases = Get-ChildItem "$TestCaseDir/*.c"
Write-Host "Running tests for $($allTestCases.Count) cases."

if (!$NoBuild) {
    buildCompiler
}

$failedTests = @()
foreach ($testCase in $allTestCases) {
    if (validateTestCase $testCase) {
        Write-Host "$($testCase): ok."
    } else {
        Write-Host "$($testCase): failed."
        $failedTests += $testCase
    }
}

if ($failedTests.Count -gt 0) {
    $testNames = $failedTests -join "`n"
    throw "Errors in the following $($failedTests.Count) tests: $testNames"
}
