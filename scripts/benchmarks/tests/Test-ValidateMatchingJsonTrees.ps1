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
    $json = '{"items":["1",1,true,false,null,{"name":"second"}]}'
    [IO.File]::WriteAllText((Join-Path $fixture.FullName 'Data/test.json'), $json)

    $benchmark = Join-Path $fixture.FullName 'Marimo.Kotowari.Benchmarks.dll'
    $validation = & dotnet $benchmark --validate 2>&1
    $exitCode = $LASTEXITCODE
    $validation | Out-Host

    if ($exitCode -ne 0) {
        throw "Expected --validate to succeed for the matching JSON trees; got exit code $exitCode."
    }
    $missingMatches = @('Pidgin', 'Sprache', 'Superpower').Where({
        -not ($validation -cmatch "^${_}: OK\b.*\bmatches SystemTextJson\b")
    })
    if ($missingMatches.Count -ne 0) {
        throw "Expected --validate to report a match to SystemTextJson for: $($missingMatches -join ', ')."
    }
}
finally {
    $tempRoot = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetTempPath())
    $fixturePath = (Resolve-Path -LiteralPath $fixture.FullName).ProviderPath
    if ((Split-Path $fixturePath -Parent) -ne $tempRoot) { throw "Unexpected fixture path: $fixturePath" }
    Remove-Item -LiteralPath $fixturePath -Recurse -Force
}
