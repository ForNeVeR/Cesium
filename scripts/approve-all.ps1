param (
    $SolutionRoot = "$PSScriptRoot/.."
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-Not $PSScriptRoot) {
    Write-Error "\`$PSScriptRoot variable isn't set. Do you run this code as a script? Please, follow instructions at https://github.com/ForNeVeR/Cesium/blob/main/docs/tests.md"
}
else {
    Get-ChildItem -Recurse $SolutionRoot -Filter "*.received.txt" | ForEach-Object {
        $receivedTestResult = $_.FullName
        $approvedTestResult = $receivedTestResult.Replace('.received.txt', '.verified.txt')
        Move-Item -Force -LiteralPath $receivedTestResult $approvedTestResult
    }
}
