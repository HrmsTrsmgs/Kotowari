#Requires -Version 7.6
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $BenchmarkOutput,
    [Parameter(Mandatory)] [string] $Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

$buildPath = (Resolve-Path -LiteralPath $BenchmarkOutput).ProviderPath
$fixture = [IO.Directory]::CreateTempSubdirectory('kotowari-validate-')
try {
    Get-ChildItem -LiteralPath $buildPath | Copy-Item -Destination $fixture.FullName -Recurse
    [IO.File]::WriteAllText((Join-Path $fixture.FullName 'Data/test.json'), $Json)
    $benchmark = Join-Path $fixture.FullName 'Marimo.Kotowari.Benchmarks.dll'
    $output = & dotnet $benchmark --validate 2>&1
    $result = [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
    $output | Out-Host
    $result
}
finally {
    $tempRoot = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetTempPath())
    $fixturePath = (Resolve-Path -LiteralPath $fixture.FullName).ProviderPath
    if ((Split-Path $fixturePath -Parent) -ne $tempRoot) { throw "Unexpected fixture path: $fixturePath" }
    Remove-Item -LiteralPath $fixturePath -Recurse -Force
}
