$programsToTest = @(
    "Cesium.Samples/functions.c"
    "Cesium.Samples/arithmetics.c"
)

dotnet build Cesium.Compiler/Cesium.Compiler.csproj

function Validate-TestCase([string]$TestCase)
{
    Write-Host "Compiling $TestCase using CL"
    & cl $testCase /Feout_cl.exe
    ./out_cl.exe | Out-File "output_cl.log"
    $extiCode = $LASTEXITCODE
    if ($extiCode -ne 42)
    {
        Write-Host "Compiling $TestCase using CL failed with unexpected exit code $extiCode"
        return $False;
    }

    dotnet run --no-build --project Cesium.Compiler -- $TestCase out_cesium.exe
    ./out_cesium.exe | Out-File "output_cesium.log"
    $extiCode = $LASTEXITCODE
    if ($extiCode -ne 42)
    {
        Write-Host "Compiling $TestCase using Cesium failed with unexpected exit code $extiCode"
        return $False;
    }

    $clOutput = $(Get-Content "output_cl.log")
    $cesiumOutput = $(Get-Content "output_cesium.log")
    if ($clOutput -ne $cesiumOutput)
    {
        Write-Host "Output of $TestCase has different content"
        return $False;
    }

    $True
}

foreach ($testCase in $programsToTest) {
    if (-not $(Validate-TestCase $testCase))
    {
        Write-Host "Failed"
        return;
    }
}

Write-Host "Ok!"
