#Requires -Version 7.6
[CmdletBinding()]
param([Parameter(Mandatory)] [string] $BenchmarkOutput)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$json = '{"items":["1",1,true,false,null,{"name":"second"}]}'
$validation = & "$PSScriptRoot/Invoke-ValidationFixture.ps1" -BenchmarkOutput $BenchmarkOutput -Json $json
if ($validation.ExitCode -ne 0) {
    throw "Expected --validate to succeed for the matching JSON trees; got exit code $($validation.ExitCode)."
}
$missingMatches = @('Pidgin', 'Sprache', 'Superpower').Where({
    -not ($validation.Output -cmatch "^${_}: OK\b.*\bmatches SystemTextJson\b")
})
if ($missingMatches.Count -ne 0) {
    throw "Expected --validate to report a match to SystemTextJson for: $($missingMatches -join ', ')."
}
