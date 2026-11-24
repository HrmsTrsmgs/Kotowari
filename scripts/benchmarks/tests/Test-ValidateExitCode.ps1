#Requires -Version 7.6
[CmdletBinding()]
param([Parameter(Mandatory)] [string] $BenchmarkOutput)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

$buildPath = (Resolve-Path -LiteralPath $BenchmarkOutput).ProviderPath
$fixture = [IO.Directory]::CreateTempSubdirectory('kotowari-validate-')
try {
    Get-ChildItem -LiteralPath $buildPath | Copy-Item -Destination $fixture.FullName -Recurse
    [IO.File]::WriteAllText((Join-Path $fixture.FullName 'Data/test.json'), '{} invalid')
    $benchmark = Join-Path $fixture.FullName 'Marimo.Kotowari.Benchmarks.dll'
    $validation = & dotnet $benchmark --validate 2>&1
    $exitCode = $LASTEXITCODE
    $validation | Out-Host

    if (-not ($validation -cmatch ': FAILED - ')) {
        throw 'Expected --validate to report a parser failure.'
    }
    if ($exitCode -eq 0) {
        throw 'Expected a nonzero exit code when --validate reported a parser failure; got 0.'
    }
    if (-not ($validation -cmatch '^Kotowari: FAILED - ')) {
        throw 'Expected --validate to report a Kotowari failure when non-whitespace input remains.'
    }
    if (-not ($validation -cmatch '^SpracheJsonPackage: FAILED - ')) {
        throw 'Expected --validate to report a SpracheJsonPackage failure when non-whitespace input remains.'
    }
}
finally {
    $tempRoot = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetTempPath())
    $fixturePath = (Resolve-Path -LiteralPath $fixture.FullName).ProviderPath
    if ((Split-Path $fixturePath -Parent) -ne $tempRoot) { throw "Unexpected fixture path: $fixturePath" }
    Remove-Item -LiteralPath $fixturePath -Recurse -Force
}
