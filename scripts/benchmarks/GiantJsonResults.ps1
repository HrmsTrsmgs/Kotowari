# BenchmarkDotNet may exit successfully without producing a valid measurement.
function Read-GiantJsonRun([string] $Directory, [int] $Pair, [string] $Side, [switch] $DotNetMigration) {
    $files = @(Get-ChildItem -LiteralPath "$Directory/results" -Filter '*-report-full-compressed.json' -File)
    if ($files.Count -ne 1) { throw "Expected one full BenchmarkDotNet report: $Directory" }
    $report = Get-Content -Raw -LiteralPath $files[0].FullName | ConvertFrom-Json
    $bench = $report.Benchmarks
    if ($bench.Count -ne 1 -or $bench[0].FullName -cne 'Marimo.Kotowari.Benchmarks.JsonParserBenchmarks.Kotowari') {
        throw "Expected exactly the Kotowari JSON benchmark: $Directory"
    }
    $bench = $bench[0]
    $results = @($bench.Measurements | Where-Object { $_.IterationMode -eq 'Workload' -and $_.IterationStage -eq 'Result' })
    if ($bench.Statistics.N -ne 5 -or ($results.IterationIndex -join ',') -ne '1,2,3,4,5' -or
        $null -eq $bench.Memory.BytesAllocatedPerOperation -or $bench.Memory.BytesAllocatedPerOperation -lt 0 -or
        $bench.Memory.TotalOperations -le 0) {
        throw "Incomplete workload or allocation results: $Directory"
    }
    $ns = 0.0
    $ops = 0L
    foreach ($r in $results) {
        if ($r.LaunchIndex -ne 1 -or $r.Operations -le 0 -or $r.Nanoseconds -le 0 -or
            ![double]::IsFinite($r.Nanoseconds)) { throw "Invalid workload measurement: $Directory" }
        $ns += $r.Nanoseconds
        $ops += $r.Operations
    }
    $info = $report.HostEnvironmentInfo
    $childRuntime = if ($DotNetMigration) { (Get-Content -Raw -LiteralPath "$Directory/runtime.txt").Trim() }
    [pscustomobject]@{
        Pair = $Pair; Side = $Side; Milliseconds = $ns / $ops / 1e6; Bytes = $bench.Memory.BytesAllocatedPerOperation
        Processor = $info.ProcessorName; OS = $info.OsVersion; Architecture = $info.Architecture
        HostRuntime = $info.RuntimeVersion; SDK = $info.DotNetCliVersion; BDN = $info.BenchmarkDotNetVersion
        Environment = "$($info.ProcessorName) | $($info.OsVersion) | $($info.Architecture) | $($info.RuntimeVersion) | SDK $($info.DotNetCliVersion) | BDN $($info.BenchmarkDotNetVersion)"
        ChildRuntime = $childRuntime
    }
}

function Write-GiantJsonSummary([string] $Directory, [object[]] $Rows, $Evidence, [switch] $DotNetMigration) {
    if ($Rows.Count -ne 8) { throw 'Expected eight valid launches.' }
    if ($DotNetMigration) {
        if (@($Rows | ForEach-Object { "$($_.Processor)|$($_.OS)|$($_.Architecture)|$($_.BDN)" } | Sort-Object -Unique).Count -ne 1) {
            throw 'Migration comparison requires the same CPU, OS, architecture, and BenchmarkDotNet version.'
        }
        foreach ($row in $Rows) {
            $expectedMajor = if ($row.Side -eq 'baseline') { 10 } else { 11 }
            if ($row.HostRuntime -notmatch "^\.NET $expectedMajor\." -or $row.ChildRuntime -notmatch "^\.NET $expectedMajor\.") {
                throw "Unexpected host/child runtime for $($row.Side): $($row.HostRuntime) / $($row.ChildRuntime)"
            }
        }
    } elseif (@($Rows.Environment | Sort-Object -Unique).Count -ne 1) {
        throw 'Expected eight valid launches in the same environment.'
    }
    $Rows | ConvertTo-Json | Set-Content -LiteralPath "$Directory/launches.json"
    $kind = if ($Evidence.Baseline.Sha -eq $Evidence.Candidate.Sha) { 'A/A (same commit)' } else { 'A/B' }
    $logRatios = @()
    $summary = @(
        "## Giant JSON — $kind"
        ''
        ('Baseline: `{0}` · Candidate: `{1}`' -f $Evidence.Baseline.Sha, $Evidence.Candidate.Sha)
        ('Verified: {0:N0} input bytes, {1:N0}/{2:N0} characters consumed, {3:N0} matching nodes.' -f
            $Evidence.Baseline.InputBytes, $Evidence.Baseline.Consumed, $Evidence.Baseline.Characters, $Evidence.Baseline.Nodes)
        $(if ($DotNetMigration) {
            ('Host: {0} · baseline host/child {1}/{2}; candidate host/child {3}/{4} · affinity {5}, PGO on, R2R off.' -f
                "$($Rows[0].Processor) · $($Rows[0].OS) · $($Rows[0].Architecture) · BDN $($Rows[0].BDN)",
                $Rows[0].HostRuntime, $Rows[0].ChildRuntime, $Rows[1].HostRuntime, $Rows[1].ChildRuntime, $Evidence.Affinity)
        } else {
            ('Host: {0} · affinity {1}, PGO on, R2R off.' -f $Rows[0].Environment.Replace('|', '·'), $Evidence.Affinity)
        })
        ''
        '| Pair | Order | Baseline ms/parse | Candidate ms/parse | Baseline B/parse | Candidate B/parse |'
        '|---:|:---|---:|---:|---:|---:|'
        foreach ($pair in 1..4) {
            $p = @($Rows | Where-Object Pair -eq $pair)
            $a = @($p | Where-Object Side -eq 'baseline')
            $b = @($p | Where-Object Side -eq 'candidate')
            if ($a.Count -ne 1 -or $b.Count -ne 1) { throw "Invalid pair $pair" }
            $logRatios += [Math]::Log($b[0].Milliseconds / $a[0].Milliseconds)
            '| {0} | {1} | {2:F2} | {3:F2} | {4:N0} | {5:N0} |' -f $pair, ($p.Side -join ' → '),
                $a[0].Milliseconds, $b[0].Milliseconds, $a[0].Bytes, $b[0].Bytes
        }
    )
    # Each adjacent pair of processes is one observation, not each iteration inside a process.
    $mean = ($logRatios | Measure-Object -Average).Average
    $squares = ($logRatios | ForEach-Object { [Math]::Pow($_ - $mean, 2) } | Measure-Object -Sum).Sum
    $margin = 3.182446305 * [Math]::Sqrt($squares / 3 / 4) # 95% paired t interval, n=4, df=3
    $summary += @(
        ''
        ('Candidate/baseline time ratio (paired geometric mean): **{0:F3}**; approximate 95% interval **{1:F3}–{2:F3}** (4 pairs).' -f
            [Math]::Exp($mean), [Math]::Exp($mean - $margin), [Math]::Exp($mean + $margin))
        'Below 1 means the candidate took less time. This is not a pass/fail threshold; compare only within this run.'
    )
    $summary | Set-Content -LiteralPath "$Directory/summary.md"
}
