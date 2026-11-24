# WordParserの文字蓄積方式の比較（2026-10-05）

## 結論と範囲

通常のランタイム設定でウォームアップを長くした全10方式の比較では、`DeferredArray` が5文字の成功・先頭失敗・末尾失敗すべてで最小の平均時間だった。
List比の平均時間短縮はそれぞれ29.8%、21.0%、10.8%。末尾失敗は容量指定Listと99.9%信頼区間が重なるため、唯一の最速とは断定しない。

採用候補は、最初の文字が成功した時点で必要な長さの `char[]` を確保する単純な実装とする。
`CharParser` への委譲、前後の空白処理、読み取った文字の保持は維持しており、文字照合の手書きインライン化は含めない。
本体の `WordParser` は今回の比較中に変更していない。既にあったジェネレーター＋`string.Concat` の未コミット試行のままであり、候補の採用は別の変更として確認する。

その後の本体への反映と、元のList版との本体同士の比較は [別の測定記録](2026-10-05-word-parser-production-deferred-array.md) に残した。下記は採用前の候補比較の記録である。

これは単語パーサー1回分の比較である。JSON全体は測っていない。全単語長・全環境・コールド実行での最速や、JSON全体の改善率を保証する結果ではない。

## 最終比較

`Run=normal-warmed-all`。単位はns/op、`±` はBenchmarkDotNetの99.9%信頼区間半幅。表示のみ小数第2位に丸め、CSVには元の数値精度を保存した。
各セルは2起動×8反復の統計（N=16）であり、16回の独立した起動ではない。

| 方式 | 成功 | 先頭失敗 | 末尾失敗 | 割り当てB/op（成功／先頭／末尾） |
|---|---:|---:|---:|---:|
| List | 69.55 ± 2.65 | 19.57 ± 1.84 | 45.66 ± 2.23 | 176 / 32 / 64 |
| ListCapacity | 67.45 ± 10.99 | 22.60 ± 2.54 | 42.75 ± 2.85 | 144 / 72 / 72 |
| StringBuilder | 128.74 ± 4.06 | 33.98 ± 3.82 | 58.84 ± 3.52 | 136 / 104 / 104 |
| StringBuilderCapacity | 158.05 ± 4.89 | 28.33 ± 3.36 | 58.51 ± 2.19 | 120 / 88 / 88 |
| IteratorConcat | 88.83 ± 12.94 | 29.81 ± 4.30 | 67.77 ± 2.43 | 128 / 96 / 128 |
| IteratorArray | 95.31 ± 4.85 | 35.09 ± 3.23 | 71.13 ± 2.84 | 168 / 96 / 128 |
| ResultLambda | 131.81 ± 3.53 | 35.07 ± 3.46 | 104.15 ± 2.64 | 624 / 144 / 528 |
| Array | 59.98 ± 2.67 | 37.74 ± 0.97 | 46.81 ± 2.23 | 72 / 40 / 40 |
| DeferredArray | 48.85 ± 2.00 | 15.45 ± 1.83 | 40.74 ± 2.15 | 72 / 0 / 40 |
| StackBuffer | 81.79 ± 1.83 | 18.26 ± 0.59 | 59.32 ± 1.81 | 32 / 0 / 0 |

ListとDeferredArrayの起動別平均（ns/op）:

| 経路 | List 起動1／2 | DeferredArray 起動1／2 |
|---|---:|---:|
| 成功 | 70.04 / 69.06 | 47.70 / 50.00 |
| 先頭失敗 | 20.72 / 18.41 | 17.15 / 13.76 |
| 末尾失敗 | 45.91 / 45.41 | 41.86 / 39.61 |

同じ条件の先行確認 `normal-warmed-confirmation` でも、DeferredArrayの平均は5文字3経路で48.81 / 15.19 / 40.62ns、4文字成功で41.01nsだった。
4文字成功はList 56.64ns、StackBuffer 85.23ns。主表の全10方式と混ぜて集計はしていない。

最終全10方式でも多峰性の警告が7件、最短反復時間の警告が1件（95.658ms）あった。
最終確認12件ではそれぞれ3件と1件（72.774ms）。先の4～9倍の起動差は再現しなかったが、ばらつきがなくなったとは扱わない。
最終全10方式の最大起動平均比は1.303（IteratorConcatの先頭失敗）。

## 比較した実装

実装は [WordParserAccumulationCandidates.cs](../WordParserAccumulationCandidates.cs)、呼び出しと入力は [WordParserAccumulationBenchmarks.cs](../WordParserAccumulationBenchmarks.cs) に保存する。

- `List`: 基点の `List<char>` と `new string([.. returnValue])`。本体の基点コミットと蓄積処理の一致を機械的に確認した。
- `ListCapacity`: Listの初期容量を単語長に指定。
- `StringBuilder` / `StringBuilderCapacity`: 既定容量／単語長で作成し、成功後に `ToString()`。
- `IteratorConcat`: `IEnumerable<char>` とキャプチャした成功フラグ、`string.Concat`。途中失敗時には生成した部分文字列を捨てる。
- `IteratorArray`: 同じ文字ジェネレーターを `[..]` で配列化し、成功確認後に文字列化。
- `ResultLambda`: ジェネレーターから `ParseResult<char>` を受け、成功した文字を一段ずつラムダへキャプチャする。全成功後に連鎖を辿り、`string.Create` で一度だけ文字列を作る。部分文字列の反復連結ではない。
- `Array`: 最初から単語長の配列を確保し、順に格納。
- `DeferredArray`: 1文字目の成功まで上記配列の確保を遅らせる。空単語では `string.Empty`。
- `StackBuffer`: 256文字以下は `stackalloc` のSpan、超過時は配列。単語長に応じてスタック使用を512 bytes以内に制限。

すべて同じ基底実装の構築・空白処理を使い、文字照合は既存の `CharParser.Parse` を呼ぶ。
測定メソッドの配列参照と `Parser<string>.Parse` 呼び出しも共通にした。
本体そのものの絶対時間ではなく、この共通ハーネス内の候補比較である。
パーサー構築、入力生成、正しさの確認は測定区間外。各候補のParse結果をベンチマークから返している。

## 環境・版

- CPU: Intel Core i7-13700H、1 CPU / 14 physical cores / 20 logical cores。
- OS: Windows 11、10.0.26200.9457 / 25H2。
- SDK: 10.0.401。Runtime: .NET 10.0.12（10.0.1226.42308）、X64 / RyuJIT。
- BenchmarkDotNet: 0.15.8。Release、MemoryDiagnoser、デバッガー接続なし。
- Affinity: CLI値4、記録は20桁のビットマスク `00000000000000000100`。OutlierMode: DontRemove。
- InvocationCountは自動決定。IterationTimeは目標値であり、Actual反復の実時間とは異なる。
- UnrollFactorは全実行でCLI既定の16（上書きなし）。最終全10方式では生成された全30ループに16呼び出しがあることも確認した。前の各実行の生成ホストは後の実行で上書きされており、各原本からの個別確認はしていない。
- 測定を直列に実行し、同時の開発・ビルド・テストを行わなかった。ユーザーから他作業を停止してよい旨の回答があった条件を使用した。
- 本体の対象フレームワークは、既に了承されていた未コミットのnet10.0移行差分を使用した。今回、その移行差分は変更していない。

基点: `15dbfedbb6df18d816e8264d66f7798d8338ade7`。
測定した候補はこの記録と一緒に保存する。未コミット時は次の作業ツリーSHA-256で版を特定する（候補ソースはBOM付きUTF-8 / CRLF）。

| ファイル | SHA-256 |
|---|---|
| WordParserAccumulationCandidates.cs | `EEFA77E6CDBF845D879D63609D35FB2EDF5DF8BC45A52B40298D1A32E1D23132` |
| WordParserAccumulationBenchmarks.cs | `EF18A4A0B464F5631CDE5FEF5F7DDFD10E99E7B916DBF9F05864F94D9E310703` |
| Kotowari/Marimo.Kotowari.csproj（net10.0移行差分込み） | `9E3F391585776526DECFDE4FA4B9FCE79A7530A2E1136732ACB093C7F9774798` |
| 一時hostのcsproj | `3855B435D54C4C88D5C40DA64AAC222FF5009FAAAB0B3214FC4FF30399F55867` |
| 一時hostのProgram.cs（最初の3実行） | `50838D656E9E254CCC0021C023D8DCABCC24D0659F32D35DBD4D680828380C03` |
| 一時hostのProgram.cs（後の2実行） | `C3E5822BA53E3B793D1E7F52DF273B98547BFF0CB988BB955909EE6B4AC3D924` |

候補2ソースは全5実行を通して不変。ホスト変更は最終確認の入力filterと実ケース表示だけであり、全10方式の実行ではfilterを有効にしていない。
本体、ホスト、生成された測定プログラムのRelease属性とJIT最適化有効を確認した。
ホストには `DisableOptimizationsValidator` があるが、最適化を無効にする設定ではない。

## 入力

`ignoreCase=true`、既定の空白パーサー。5文字は `false`、4文字は `true`、32/128文字はそれぞれ `a` を31/127個と末尾の `z`。
成功入力は `word + ","`、先頭失敗は `"!" + word[1..] + ","`、末尾失敗は `word[..^1] + "!,"`。
末尾のカンマによりEOF例外処理の費用をこの比較へ入れない。失敗経路は意図した失敗を測っている。
測定前のGlobalSetupで各候補の成功・失敗、値、カーソルを検証する。

サイズとSHA-256は入力全体のUTF-8、BOMなし。すべてASCIIなので文字数とbytesが一致する。

| Case | bytes | SHA-256 |
|---|---:|---|
| False5Success | 6 | `3721E7F2DB98C23F7E5BFCA0220E57FF4A8F3EAF59BBE3A7D579EC0720BAE680` |
| False5FirstFailure | 6 | `81B0FA11992F6DEC4228FB8EC518E6257C817B7EF7448815EF99849F8AC05F93` |
| False5LastFailure | 6 | `9147E5D367B9660802AFD2F436EE32A2FDD60478A76CFF1090D2CFB17F3BE9B1` |
| True4Success | 5 | `9524AA6B0B64C4CBD405337863880985C135199923FC4E5D1A2CE36203E20BB8` |
| True4FirstFailure | 5 | `0763DCAF9F3F0120152A074882570F70DC016EFDEC65639F8A70374C8433D706` |
| True4LastFailure | 5 | `9AB963ED123BF0947624433B4EBAC1AF59EA5964FAEEE12CA68125A11109B611` |
| Long32Success | 33 | `09AA7274701135D2A3418CE40E8C3AA4A76CC44F6AE119722082930B85C64A1C` |
| Long32FirstFailure | 33 | `35FB0F7B03764AAD4C1C3085C60375E88B5EFD8D1B62DAAC2766299AF24E8B28` |
| Long32LastFailure | 33 | `D9AE8F35132164B64A7C7FE7A02F50B7943D88A2E7D90BDA69D6A9EDFC56BE59` |
| Long128Success | 129 | `A55D9EACE31C81CF3861A16A0C884572036EED834C38AA220E5BBF8D9F0A259D` |
| Long128FirstFailure | 129 | `385DFC90A454C01912E9794AFD8477CFA5B9F4BA0F81057CF9910B1A34585740` |
| Long128LastFailure | 129 | `1F7D9A37C9866AC08EA369C9E85860CE30865FD2F430E94C2496E0A7A662F10E` |

## 実行の区別

CSVの `Experiment` は `2026-10-05-accumulation`。Launch/Warmup/Iterationは回数、Timeは目標ms。

| Run | 候補×入力 | Launch / Warmup / Iteration / Time | Runtime設定 | summary / iterations / launches |
|---|---|---|---|---:|
| normal-short-exploration | 全10×5文字3経路 | 1 / 3 / 5 / 150 | 既定 | 30 / 300 / 30 |
| normal-short-confirmation | List/Deferred/Stack×全12入力 | 3 / 5 / 8 / 150 | 既定 | 36 / 1,728 / 108 |
| tiered-disabled-exploration | 全10×5文字3経路 | 2 / 3 / 8 / 150 | DOTNET_TieredCompilation=0 | 30 / 960 / 60 |
| normal-warmed-confirmation | List/Deferred/Stack×5文字3経路＋4文字成功 | 2 / 10 / 8 / 500 | 既定 | 12 / 384 / 24 |
| normal-warmed-all | 全10×5文字3経路 | 2 / 10 / 8 / 500 | 既定 | 30 / 960 / 60 |
| 追加合計 | | | | 138 / 4,332 / 282 |

最初の単一起動ではStackBufferが最小だった。その後の通常設定の確認では起動間に4～9倍の差が出た。
この確認には多峰性警告4件、最短反復時間警告7件（最小16.4001ms）があり、864 Actual反復のうち42反復が100ms未満だった。
その不安定な全体平均だけから候補を選ばなかった。

段階的JIT無効の診断実行では成功時StackBuffer 141.55ns、DeferredArray 148.02ns、List 193.14ns。
先頭失敗はDeferredArray 30.72nsが最小、末尾失敗はArray 128.21nsとStackBuffer 128.43nsが同等範囲だった。
この実行に多峰性／最短反復時間の警告はなかったが、既定ランタイム条件の代わりにはしない。
長いウォームアップの通常条件では順位が変わったため、最終的に全10方式を同じ通常条件で比較し直した。

段階的JITの切替は [公式ランタイム設定](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/compilation#tiered-compilation) に従った。
値と順位がJIT設定・ウォームアップ条件で変わったことは観測したが、起動差の正確な原因は特定していない。JITイベントのトレースやプロファイルは行っていない。
32/128文字の通常確認はこの不安定な短い条件だけであり、それらの順位を最終結論には使わない。
256文字超のStackBuffer配列フォールバックは正しさだけを検証し、速度は未確認。

## 再実行

新しい作業ツリーで重複するベンチマークcsprojがなければ、最終全10方式は既存プロジェクトで次のように実行できる。

```powershell
dotnet run --project Kotowari.Benchmarks/Marimo.Kotowari.Benchmarks.csproj -c Release -- --filter '*WordParserAccumulationBenchmarks.*' --launchCount 2 --warmupCount 10 --iterationCount 8 --iterationTime 500 --affinity 4 --outliers DontRemove --exporters json --keepFiles --artifacts artifacts/word-parser-accumulation-all-default-warmed
```

実際には古い `artifacts/` 内の同名csprojまでBenchmarkDotNetが探索してしまい、最初の2試行は起動前に失敗した（測定0件）。
その記録は `artifacts/word-parser-accumulation-exploration` と `artifacts/word-parser-accumulation-exploration-r1` に残し、結果CSVへは入れていない。
古い記録を削除せず、固有名の一時ホスト `artifacts/word-parser-accumulation-host/WordParserAccumulation.Measurement.csproj` で上記と同じ候補ソースを直接リンクした。
再現に必要な一時csprojは次のとおり（パッケージ版はリポジトリの中央管理を継承）。

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <AssemblyName>WordParserAccumulation.Measurement</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" />
    <ProjectReference Include="../../Kotowari/Marimo.Kotowari.csproj" />
    <Compile Include="../../Kotowari.Benchmarks/WordParserAccumulationCandidates.cs" Link="WordParserAccumulationCandidates.cs" />
    <Compile Include="../../Kotowari.Benchmarks/WordParserAccumulationBenchmarks.cs" Link="WordParserAccumulationBenchmarks.cs" />
  </ItemGroup>
</Project>
```

最初の3実行のProgram.csは次の版。

```csharp
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Marimo.Kotowari.Benchmarks.MeasurementHost;

public static class Program
{
    public static void Main(string[] args)
    {
        var config = DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator);
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}
```

後の2実行では次の版を使った。通常はfilter無効、12件確認のときだけ `--short-final-confirmation` を渡した。
`--show-final-cases` は12実ケースの事前確認用で、測定実行には渡していない。

```csharp
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using System;
using System.Linq;

namespace Marimo.Kotowari.Benchmarks.MeasurementHost;

public static class Program
{
    public static void Main(string[] args)
    {
        var config = DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator);
        if (args.Contains("--short-final-confirmation"))
        {
            var filter = new SimpleFilter(it => it.Parameters.Items.Any(parameter =>
                parameter.Name == "Case" && parameter.Value is WordParserAccumulationCase.False5Success
                    or WordParserAccumulationCase.False5FirstFailure
                    or WordParserAccumulationCase.False5LastFailure
                    or WordParserAccumulationCase.True4Success));
            config.AddFilter(filter);
            if (args.Contains("--show-final-cases"))
            {
                var cases = BenchmarkConverter.TypeToBenchmarks(typeof(WordParserAccumulationBenchmarks), config)
                    .BenchmarksCases.Concat(BenchmarkConverter.TypeToBenchmarks(
                        typeof(WordParserAccumulationConfirmationBenchmarks), config).BenchmarksCases)
                    .Where(filter.Predicate)
                    .Where(it => it.Descriptor.WorkloadMethod.Name is "List" or "DeferredArray" or "StackBuffer")
                    .ToArray();
                foreach (var benchmark in cases)
                {
                    Console.WriteLine(benchmark.DisplayInfo);
                }
                Console.WriteLine($"Selected benchmark cases: {cases.Length}");
                return;
            }
            args = [.. args.Where(it => it != "--short-final-confirmation")];
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}
```

実測では上のrunコマンドのprojectをこの一時ホストへ置き換えた。
各RunのLaunch/Warmup/Iteration/Timeは前表の値に置き換える。
全12入力の3方式確認は `--filter '*WordParserAccumulation*.List' '*WordParserAccumulation*.DeferredArray' '*WordParserAccumulation*.StackBuffer'` を使い、12件の最終確認だけこれに `--short-final-confirmation` を加える。
段階的JIT無効時だけ `--envVars DOTNET_TieredCompilation:0` を加え、他実行には渡していない。
旧Programは `artifacts/word-parser-accumulation-snapshots/Program-before-final-confirmation.cs` に保持している。

## 保存データ

CSVは増やさず [summary.csv](word-parser/summary.csv)、[iterations.csv](word-parser/iterations.csv)、[launches.csv](word-parser/launches.csv) へ追記した。`stacks.csv` は変更なし。
旧データ部分の全bytesが追記前と一致することをSHA-256で確認した。新しい反復記録はWorkloadのActualとResultだけで、Pilot/Warmup/Overheadは完全JSONに残す。
ActualとResultの4,332行は2,166反復に対応する。起動別平均・標本標準偏差はResultの時間/操作数から計算した。
新summaryのGCは完全JSONの診断合計/TotalOperations×1,000。完全JSONに独立した起動別のメモリ診断がないため、launchesの割り当て・GCは空欄とし、合計を各起動へ複製していない。

元ログ・完全JSON・生成ホストはGit対象外の `artifacts/` に保持。各Runの結果ファイルとSHA-256:

- `normal-short-exploration`: `artifacts/word-parser-accumulation-exploration-r2/results/Marimo.Kotowari.Benchmarks.WordParserAccumulationBenchmarks-report-full-compressed.json` — `A2978B1F494C7F6A7D345B57A2816480679291D4BE957E2FACB8DB469F72B201`
- `normal-short-confirmation`: `artifacts/word-parser-accumulation-confirmation/results/BenchmarkRun-joined-2026-10-05-02-06-11-report-full-compressed.json` — `AFB56445A7A0AE985FE313F603A5103661EC768A7697AAB1E70A4ABD03E5F4A1`
- `tiered-disabled-exploration`: `artifacts/word-parser-accumulation-tiered-disabled-exploration/results/Marimo.Kotowari.Benchmarks.WordParserAccumulationBenchmarks-report-full-compressed.json` — `3FCFF8C2459BE4AEF0F33F8A97BBADA720AE093AB5FE9F9AF50C15BB8F29C42D`
- `normal-warmed-confirmation`: `artifacts/word-parser-accumulation-final-default-confirmation/results/BenchmarkRun-joined-2026-10-05-02-35-58-report-full-compressed.json` — `A4EFC7F0FC18B9EC05E0E04E370BDA603D643280C31BECA4226BCF1AC045D267`
- `normal-warmed-all`: `artifacts/word-parser-accumulation-all-default-warmed/results/Marimo.Kotowari.Benchmarks.WordParserAccumulationBenchmarks-report-full-compressed.json` — `452DE3977856A74D2302BD689C8EC16D26A89CD0D49A77135FC5D06A41038A83`

## 正しさの検証

既存WordParserの25ケースを各10候補に適用した250件、全12入力のGlobalSetup確認1件、長さ256/257/1,024の成功・先頭失敗・末尾失敗9件の、一時テスト計260件が成功した。
カルチャー変更、入力配列の変更、前後の空白・副作用、空単語、負のカーソル、EOFのNUL、変更可能な番兵、null引数など、既存の契約を弱めていない。
振る舞いを変える依頼ではないので形式的なRedは作っていない。

測定後の検査結果:

- `dotnet test Kotowari.sln -c Release --no-restore -p:UseSharedCompilation=false`: 242件成功、失敗・Skip 0、6.88秒。
- Releaseソリューションbuild: 成功、3.25秒。既存のSpracheJSON 1.0.1に対するNU1701警告あり。
- `dotnet build Kotowari.sln --no-restore --no-incremental -p:UseSharedCompilation=false -p:EnforceCodeStyleInBuild=true -p:RunAnalyzersDuringBuild=true -p:ErrorLog=obj/ide-diagnostics.sarif`: 成功、17.15秒。
- 5プロジェクトのSARIFを確認。既存7診断（CA2211、IDE0090×2、IDE0200、CA1854、IDE0066、CA1862）のみで、候補2ファイルおよびWordParserにはSARIF診断なし。
- `dotnet format Kotowari.sln --no-restore --verify-no-changes --severity info`: 失敗（exit 2）、10.59秒。既存73指摘で、候補2ファイルは0。WordParserの未変更constructor行28のIDE0290も蓄積方式の比較範囲外として残した。全体のformatが成功したとは扱わない。
- 候補2ファイルの限定format検査は先行検証で成功している。ソース不変のため260件の候補検査と限定formatを受け渡しのためだけに再実行していない。

CSV整合検査:

- 既存3CSVをスプレッドシート用のCSV読取機能で再読込し、列数26/15/19、実験別行数、時間単位、平均の有限値を確認した。
- summary 210行、iterations 5,572行、launches 350行。追加分は138/4,332/282行で、索引と一致する。
- 新しい138 summaryの平均が対応する起動別平均と一致すること、起動数とジョブ設定が一致することを確認した。
- 既存データ部分のSHA-256は追記前と一致し、旧行を再計算・再整形していない。
- 編集ファイルの改行・BOMに混在なし。候補はBOM付きUTF-8/CRLF、測定記録・CSVは既存記録と同じUTF-8/LF。`git diff --check` 成功。

本体試行および先行net10移行差分は維持した。ステージング・コミット・pushは行っていない。
