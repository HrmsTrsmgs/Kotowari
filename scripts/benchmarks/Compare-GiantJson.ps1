# Compares two trusted, clean checkouts on one Windows machine. No results are written to Git.
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Baseline,
    [Parameter(Mandatory)] [string] $Candidate,
    [Parameter(Mandatory)] [string] $OutputDirectory,
    [switch] $ValidateOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
. (Join-Path $PSScriptRoot 'GiantJsonResults.ps1')

function Invoke-LoggedDotnet([string] $Log, [string[]] $Arguments) {
    & dotnet @Arguments 2>&1 | Tee-Object -FilePath $Log | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "dotnet exited with ${LASTEXITCODE}: see $Log" }
}

function Get-CheckoutEvidence([string] $Root, [string] $Label) {
    $logs = (New-Item -ItemType Directory -Path (Join-Path $output $Label)).FullName
    Push-Location -LiteralPath $Root
    try {
        $sha = git rev-parse --verify 'HEAD^{commit}'
        if ($LASTEXITCODE -ne 0 -or $sha -notmatch '^[0-9a-f]{40}$') { throw "Not a Git checkout: $Root" }
        $sdk = dotnet --version
        if ($LASTEXITCODE -ne 0) { throw "Could not resolve the SDK in $Root" }
        Invoke-LoggedDotnet "$logs/dotnet-info.txt" @('--info')
        Invoke-LoggedDotnet "$logs/build.log" @('build', $project, '-c', 'Release')

        $inputPath = (Resolve-Path -LiteralPath "$bin/Data/test.json").Path
        $hash = (Get-FileHash -LiteralPath $inputPath -Algorithm SHA256).Hash
        if ($hash -ne 'DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D') {
            throw "Unexpected sample SHA-256 in ${Label}: $hash. Use the archived Windows (CRLF) sample."
        }
        Copy-Item -LiteralPath "$bin/Marimo.Kotowari.Benchmarks.deps.json" -Destination "$logs/dependencies.json"
        # The file-based app must resolve its project reference against this revision, not the control checkout.
        $probe = Join-Path $Root 'VerifyJson.cs'
        [IO.File]::Copy((Join-Path $PSScriptRoot 'VerifyJson.cs'), $probe, $false)
        try {
            Invoke-LoggedDotnet "$logs/verify.log" @(
                'run', '--file', $probe, '-c', 'Release', '--', $inputPath, "$logs/verification.json"
            )
        } finally {
            Remove-Item -LiteralPath $probe
        }
        $verified = Get-Content -Raw -LiteralPath "$logs/verification.json" | ConvertFrom-Json
        if ($verified.Consumed -ne $verified.Characters -or $verified.Nodes -le 0 -or
            $verified.TreeHash -notmatch '^[0-9a-f]{64}$') { throw "Invalid verification for $Label" }
        [ordered]@{
            Sha = $sha; Sdk = $sdk; InputBytes = (Get-Item -LiteralPath $inputPath).Length; InputHash = $hash
            HarnessHash = (Get-FileHash -LiteralPath 'Kotowari.Benchmarks/JsonParserBenchmarks.cs').Hash
            EntryPointHash = (Get-FileHash -LiteralPath 'Kotowari.Benchmarks/Program.cs').Hash
            Characters = $verified.Characters; Consumed = $verified.Consumed
            Nodes = $verified.Nodes; TreeHash = $verified.TreeHash
        }
    } finally {
        Pop-Location
    }
}

if (!$IsWindows) { throw 'Use PowerShell 7 on Windows for this benchmark.' }
$roots = @{ baseline = (Resolve-Path -LiteralPath $Baseline).Path; candidate = (Resolve-Path -LiteralPath $Candidate).Path }
$output = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw "Choose a new results directory: $output" }
$null = New-Item -ItemType Directory -Path $output
$project = 'Kotowari.Benchmarks/Marimo.Kotowari.Benchmarks.csproj'
$bin = 'Kotowari.Benchmarks/bin/Release/net10.0'
$dll = "$bin/Marimo.Kotowari.Benchmarks.dll"
$allowed = [Diagnostics.Process]::GetCurrentProcess().ProcessorAffinity.ToInt64()
$mask = if ($allowed -band 4) { 4 } else { $allowed -band (-$allowed) }
if ($mask -le 0) { throw "Cannot select a shared CPU from affinity mask $allowed" }
$evidence = [ordered]@{
    RunnerImage = "$env:ImageOS $env:ImageVersion".Trim(); Affinity = $mask
    Baseline = Get-CheckoutEvidence $roots.baseline 'baseline'
    Candidate = Get-CheckoutEvidence $roots.candidate 'candidate'
}
$evidence | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath "$output/comparison.json"
foreach ($key in $evidence.Baseline.Keys) {
    if ($key -ne 'Sha' -and $evidence.Baseline[$key] -cne $evidence.Candidate[$key]) {
        throw "Comparison refused: $key differs. See comparison.json."
    }
}
if ($ValidateOnly) { Write-Host 'Both revisions passed preflight; timing was not requested.'; return }

$rows = foreach ($pair in 1..4) {
    $order = if ($pair % 2) { 'baseline', 'candidate' } else { 'candidate', 'baseline' }
    foreach ($side in $order) {
        $run = (New-Item -ItemType Directory -Path "$output/pair-$pair-$side").FullName
        Push-Location -LiteralPath $roots[$side]
        try {
            Invoke-LoggedDotnet "$run/console.log" @(
                $dll, '--filter', '*JsonParserBenchmarks.Kotowari', '--launchCount', '1',
                '--warmupCount', '4', '--iterationCount', '5', '--iterationTime', '250', '--unrollFactor', '1',
                '--outliers', 'DontRemove', '--affinity', "$mask", '--envVars', 'DOTNET_TieredPGO:1',
                'DOTNET_ReadyToRun:0', '--exporters', 'json', '--artifacts', $run
            )
        } finally {
            Pop-Location
        }
        Read-GiantJsonRun $run $pair $side
    }
}
Write-GiantJsonSummary $output $rows $evidence
