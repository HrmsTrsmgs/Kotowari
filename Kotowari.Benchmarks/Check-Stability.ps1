<#
.SYNOPSIS
Checks launch-to-launch and within-launch variation in BenchmarkDotNet full JSON reports.
.DESCRIPTION
Pass reports for the same source, input, runtime and job. Result iterations are used once;
Actual iterations are not counted again. Thresholds are measurement acceptance criteria,
not a confidence interval or proof that a performance difference is significant.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string[]]$ReportPath,
    [ValidateRange(2, 1000)][int]$MinimumLaunches = 5,
    [ValidateRange(0.01, 100)][double]$MaxLaunchCvPercent = 3,
    [ValidateRange(0.01, 100)][double]$MaxMeanIterationCvPercent = 5
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-Statistics([double[]]$Values) {
    if ($Values.Count -lt 2 -or @($Values | Where-Object { ![double]::IsFinite($_) -or $_ -le 0 }).Count) {
        throw 'At least two finite, positive measurements are required.'
    }
    $mean = ($Values | Measure-Object -Average).Average
    $squaredDeviations = ($Values | ForEach-Object { [Math]::Pow($_ - $mean, 2) } | Measure-Object -Sum).Sum
    $sd = [Math]::Sqrt($squaredDeviations / ($Values.Count - 1))
    [pscustomobject]@{ Mean = $mean; CV = 100 * $sd / $mean }
}

$paths = @($ReportPath | ForEach-Object { (Resolve-Path -LiteralPath $_).Path })
if (@($paths | Select-Object -Unique).Count -ne $paths.Count) {
    throw 'A report was supplied more than once; do not duplicate launches.'
}
$groups = @{}
foreach ($path in $paths) {
    $report = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    foreach ($benchmark in $report.Benchmarks) {
        # DisplayInfo contains the job and its explicit settings; omit only its generated ID.
        $job = $benchmark.DisplayInfo -replace '^.*?: [^(]+', ''
        $key = $benchmark.FullName + '|' + $job
        if (!$groups.ContainsKey($key)) {
            $groups[$key] = [Collections.Generic.List[object]]::new()
        }
        $results = @($benchmark.Measurements | Where-Object {
            $_.IterationMode -eq 'Workload' -and $_.IterationStage -eq 'Result'
        })
        $actual = @($benchmark.Measurements | Where-Object {
            $_.IterationMode -eq 'Workload' -and $_.IterationStage -eq 'Actual'
        })
        if ($results.Count -ne $benchmark.Statistics.N -or $results.Count -ne $actual.Count -or $results.Count -eq 0) {
            throw "Missing or excluded Result iterations in $path"
        }
        foreach ($launch in $results | Group-Object LaunchIndex) {
            $values = @($launch.Group | ForEach-Object { $_.Nanoseconds / $_.Operations })
            $statistics = Get-Statistics $values
            $groups[$key].Add([pscustomobject]@{
                Report = Split-Path -Leaf (Split-Path -Parent (Split-Path -Parent $path))
                Launch = [int]$launch.Name
                Iterations = $values.Count
                MeanMs = $statistics.Mean / 1e6
                IterationCvPercent = $statistics.CV
            })
        }
    }
}
if ($groups.Count -eq 0) { throw 'No benchmarks found.' }
$failed = $false
foreach ($key in $groups.Keys | Sort-Object) {
    $launches = $groups[$key]
    if ($launches.Count -lt 2) { throw "Not enough launches: $key" }
    $statistics = Get-Statistics @($launches | ForEach-Object MeanMs)
    $iterationCv = ($launches.IterationCvPercent | Measure-Object -Average).Average
    $passed = $launches.Count -ge $MinimumLaunches -and $statistics.CV -le $MaxLaunchCvPercent -and
        $iterationCv -le $MaxMeanIterationCvPercent
    $failed = $failed -or !$passed
    [pscustomobject]@{
        BenchmarkAndJob = $key
        Launches = $launches.Count
        MeanMs = $statistics.Mean
        LaunchCvPercent = $statistics.CV
        MeanIterationCvPercent = $iterationCv
        Passed = $passed
        LaunchResults = @($launches.ToArray())
    }
}
if ($failed) { exit 1 }
exit 0
