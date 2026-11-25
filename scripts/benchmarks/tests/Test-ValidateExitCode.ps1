#Requires -Version 7.6
[CmdletBinding()]
param([Parameter(Mandatory)] [string] $BenchmarkOutput)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$validation = & "$PSScriptRoot/Invoke-ValidationFixture.ps1" -BenchmarkOutput $BenchmarkOutput -Json '{} invalid'
if ($validation.ExitCode -eq 0) {
    throw 'Expected a nonzero exit code when --validate reported a parser failure; got 0.'
}
if (-not ($validation.Output -cmatch '^Kotowari: FAILED - ')) {
    throw 'Expected --validate to report a Kotowari failure when non-whitespace input remains.'
}
if (-not ($validation.Output -cmatch '^SpracheJsonPackage: FAILED - ')) {
    throw 'Expected --validate to report a SpracheJsonPackage failure when non-whitespace input remains.'
}
