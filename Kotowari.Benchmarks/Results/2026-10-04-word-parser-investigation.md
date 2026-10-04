# WordParserの性能差の追加調査 — 2026-10-04

## 結論

[先行測定](2026-10-04-word-parser.md)でJSON全体が遅く見えた理由を調査した。今回、新しい最適化や採用判断の実装は行っていない。

- 案BのWordParser自体の改善は、JSON解析中のスタックでも観測できた。WordParser.ParseCoreを含むサンプルの割合は13.8%から4.1%へ減った。
- 通常条件で測定順を変更前→B→B→変更前とした結果、変更前258.34ms、B232.54msだった。ただし起動間の揺れが大きく、約10%の高速化率を確定したとは扱わない。前回の単一プロセスの結果から「BがJSON全体を遅くする」とも断定できない。
- 解析1回のマネージド割り当ては80.31MiBから63.09MiBへ約21.5%減り、全プロセスで再現した。
- ウォームアップ後の本測定にも段階的JITのイベントが残っていた。しかし無効にした条件でも大きな外れ値があり、JITだけを揺れの原因とは特定できなかった。
- したがってBを失敗案とは扱わない。採用するなら「再利用時の単体高速化と割り当て削減」として評価し、JSON全体の安定した高速化率の主張は保留する。一回だけ利用する場合の生成コスト増加は先行記録のとおり残る。

## ソースの固定と変更範囲

基点は `9d9ecd83c9942d521be15beacbc8e9bc7582a6e6`、ブランチは `codex/optimize-word-parser`。既存の未コミットの案B、回帰テスト、単体ベンチマークを保ったまま、Git対象外の `artifacts/word-parser-investigation-2026-10-04/sources/` にoriginalとcandidateを作成した。

本体、JSON使用例、測定プロジェクトのソース、入力、ビルド設定等37ファイルをコピーし、originalだけWordParserの以下の2行を変更前へ戻した。コピーしたファイルのハッシュ比較で、そのファイルだけが異なることを確認した。

| 実装 | 非公開Parsersの型 | コンストラクターの代入 |
|---|---|---|
| original | `IEnumerable<Parser<char>>` | `word.Select(c => new CharParser(c, ignoreCase))` |
| candidate（B） | `Parser<char>[]` | 上記に `.ToArray()` を追加 |

WordParser.csのSHA-256:

- original: `6EA0BA5D6676ED72144850A53706E96E5A1196A72DB2739FA12919C7AB1DCE06`
- candidateおよび元の作業ツリー: `09056172CD3E87349124FBC1F4D141A2F02BF2A3FBA9261EED6960C4585A0415`

元の本体・テスト・測定コードやプロジェクト設定は、この調査では変更していない。追加したGit対象のファイルはこの文書と3つの集計CSVだけ。過去の測定記録は書き換えていない。

## 入力・環境・バージョン

- 入力: 既存の `Kotowari.Benchmarks/Data/test.json`。置換・縮小なし。
- サイズ: 4,172,062 bytes / 4,172,062 UTF-16コード単位。
- SHA-256: `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。
- OS: Windows 11 25H2、10.0.26200.9457。
- CPU: Intel Core i7-13700H 2.90GHz、14物理 / 20論理コア。
- SDK: 10.0.401。Runtime: .NET 10.0.12 (10.0.1226.42308)、X64 RyuJIT x86-64-v3。
- GC: Concurrent Workstation。
- BenchmarkDotNet: 0.15.8。TraceEvent: 3.1.21（既存の測定依存に含まれるもの）。
- Pidgin 3.5.1、Sprache 2.3.1、SpracheJSON 1.0.1、Superpower 3.2.1。System.Text.Jsonはランタイム付属。
- 依存の追加・更新、グローバルツールのインストールはしていない。コピーの初回復元は空のローカルfeedと既存キャッシュだけで実施した。
- BenchmarkDotNetは高パフォーマンス電源プランを使用し、各実行後に元のプランへ復元した。

他ライブラリの時間は今回測り直していない。既存のSpracheJSONの手動DLL参照・NU1701警告、Optimization Validator無効化は変更していない。System.Text.Jsonは参考対象のまま。

## 通常条件の比較

Release / Shortジョブを基に、LaunchCount=3、WarmupCount=10、IterationCount=10、InvocationCount=4、UnrollFactor=1、OutlierMode=DontRemove、Affinity=4（論理CPU 2）に固定した。前回の適応的な反復内実行数とは異なり、1反復につき必ず文書全体を4回解析する。

各プロセスで本測定前に40回のウォームアップ、本測定で40回の解析を行う（Jittingおよび診断用の追加呼び出しは別）。ファイル読み込みはGlobalSetupで測定外、文字配列化・解析・結果構築は測定内。測定・ビルド・テスト・プロファイルを同時には走らせていない。

| 実行順 | 実装 | 各プロセスの平均 (ms) | 実行全体の平均 (ms) |
|---|---|---:|---:|
| 01-original | 変更前 | 190.26 / 265.99 / 261.84 | 239.36 |
| 02-candidate | 案B | 221.48 / 256.67 / 212.41 | 230.19 |
| 03-candidate | 案B | 211.68 / 247.84 / 245.16 | 234.89 |
| 04-original | 変更前 | 288.89 / 275.24 / 267.80 | 277.31 |

実装別に6プロセスを集計した平均は変更前258.34ms、B232.54ms。プロセス平均の標本標準偏差はそれぞれ34.68ms、19.69msで、変更前の範囲は190.26〜288.89ms、Bは211.68〜256.67msだった。

この120反復を、起動間の揺れも含めた120個の独立した試行とみなさない。平均が小さかったことと、安定した改善率を確定したことを区別する。BenchmarkDotNetの各実行のErrorは99.9%信頼区間の半幅であり、複数起動の因果関係の保証ではない。外れ値も除去していない。

割り当ては変更前80.31MiB、B63.09MiB。Gen0は1,000解析あたり6,750→5,250、Gen1は2,750→2,250、Gen2は750→750だった。GC回数の減少を、そのままGC時間の減少とは扱わない。Allocatedは累積マネージド割り当てであり、保持量やピーク使用量ではない。

## プロファイルで確認できたこと

通常測定とは別に、各実装をLaunchCount=1・その他同じ条件で `--profiler EP` により記録した。実際のトレースにはJitting・ウォームアップも含まれていたため、その全体を解析時間の内訳には使用していない。

既存TraceEvent 3.1.21を参照する一時解析プログラムで、BenchmarkDotNet.EngineEventSourceのWorkloadActual/Start・Stop（ID 15/16）に挟まれた10区間だけを抽出し、各区間のベンチマークスレッドに限定した。同じサンプル内で同じメソッドが再帰的に複数回現れる場合、子呼び出し込みの集計は1回だけ数えた。自身の集計は先頭フレームだけを数えた。メソッド名は記録に含まれる情報を使用し、外部シンボルの取得はしていない。

| 対象 | 変更前：サンプル数 / 割合 | B：サンプル数 / 割合 |
|---|---:|---:|
| WordParser.ParseCore（子呼び出し込み） | 49 / 13.8% | 14 / 4.1% |
| WordParser.ParseCore（自身） | 34 / 9.6% | 10 / 2.9% |
| WithWhiteSpaceParser&lt;char&gt;（自身） | 62 / 17.4% | 55 / 16.1% |
| WithWhiteSpaceParser&lt;参照型&gt;（自身） | 39 / 11.0% | 55 / 16.1% |
| Cursol.Current（自身） | 38 / 10.7% | 44 / 12.9% |
| ZeroOrMoreParser&lt;char&gt;（自身） | 15 / 4.2% | 10 / 2.9% |
| Thread.PollGC（自身） | 29 / 8.1% | 27 / 7.9% |

実測区間のスレッドサンプルは変更前356件、B341件。EventsLost=0、抽出サンプルの欠落スタック=0だった。割合はこのサンプル集合に対する比率であり、壁時計時間の正確な比率ではない。別プロセス1回ずつの粗いサンプリングで、インライン化やスタックの打ち切り、記録の負荷、本測定中のJITの影響もある。子呼び出し込みの割合は互いに重なるので合計しない。

WordParserにかかる処理が減る方向は、単体測定と一致した。ただしWordParserは文書解析の全体ではなく、空白処理、Cursolからの文字取得、文字列の走査、結果構築などの処理が残る。単体の約2倍の改善を、文書全体の約2倍へ外挿できない。

JSONのnull/true/false用WordParserは静的な構文オブジェクトとして作られ、解析ごとに再生成されない。そのため先行測定の「生成して一度だけ解析」の悪化だけで、この全文解析の時間を説明することはできない。また単体ベンチマークの空白はスペース1文字だが、JSONではスペース・タブ・CR・LFをOrParserで判定しており、同一の周辺処理ではない。

### 本測定中のJIT

本測定区間にTieredCompilation/BackgroundJitStartが変更前22件、B13件あった。ウォームアップ10回を増やしただけでは、段階的な再コンパイルが終わった定常状態を保証できていなかった。

段階的JITを無効にする環境変数 `DOTNET_TieredCompilation=0` の意味は[Microsoftの公式文書](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/compilation#tiered-compilation)で確認し、BenchmarkDotNetの `--envVars` により子プロセスだけへ指定した。恒久的なプロジェクト・マシン設定変更ではない。

## ランタイム条件を変えた切り分け

LaunchCount=3、その他は通常測定と同じで、変更前→Bの順に測った。この条件は通常実行ともプロファイルとも混ぜない。

| 実装 | Mean (ms) | Error (ms) | StdDev (ms) | 割り当て (MiB) |
|---|---:|---:|---:|---:|
| 変更前・段階的JIT無効 | 294.0 | 22.77 | 34.09 | 80.78 |
| B・段階的JIT無効 | 270.2 | 16.79 | 25.13 | 63.56 |

変更前では443.32ms、Bでは392.54msの反復も残った。段階的JITを無効にしても揺れはなくならず、環境変動を含む原因は特定できていない。この実験を、段階的JITの無効化を製品へ推奨する根拠にはしない。通常条件と割り当て量もわずかに異なる。

プロファイル付きの平均は変更前295.6ms（Error 69.01ms / StdDev 45.64ms）、B274.0ms（Error 12.22ms / StdDev 8.08ms）だったが、記録の負荷が含まれるため主比較へ追加していない。

## 記録と検証

Git対象の追加ファイル:

- この調査記録: `2026-10-04-word-parser-investigation.md`。
- [反復CSV](2026-10-04-word-parser-investigation-iterations.csv): 全200反復。通常120、段階的JIT無効60、プロファイル付き20をMeasurementKindで分離。
- [プロセス別CSV](2026-10-04-word-parser-investigation-launches.csv): 全20プロセスの平均、標準偏差、最小・最大、GC回数、割り当て。
- [スタックCSV](2026-10-04-word-parser-investigation-stacks.csv): 各実装の自身・子呼び出し込みの上位50メソッドまで、サンプル数と割合。

完全なログ・元CSV・nettrace・speedscope・etlx・固定ソース・一時解析プログラムは、Git対象外の `artifacts/word-parser-investigation-2026-10-04/` に残している。ホスト固有の絶対パスをこの記録や集計CSVへ含めていない。

- 変更前・BのコピーをそれぞれReleaseでビルドし成功。測定時の自動ビルドも全8実行で成功。既存NU1701警告は維持。
- 測定前に各コピーの非公開JObjectパーサーを測定外で確認し、成功、JSONObjectの生成、4,172,062 / 4,172,062文字の全消費を確認した。
- 両コピーの `--validate` で全6対象の成功と結果生成を確認。Kotowari / Pidgin / Sprache / Superpower / System.Text.Jsonは100,001ノード、旧SpracheJSONは104,120ノード。旧比較対象の生成結果の差を隠していない。
- 一時解析プログラムの最初のビルドはSampleProfilerTraceEventParserの名前空間指定で失敗し、実際のアセンブリのメタデータを確認して修正。最終ビルド・双方の集計は成功した。
- 本体・テストの追加変更がないため、この調査では全テストとソリューションの静的解析を再実行していない。前回最終Bの230件成功を今回の新しいテスト実行結果とは扱わない。
- CSVの200反復・20プロセス・各プロセス10反復と平均の一致、173行のスタック集計の割合を機械的に確認した。追加4ファイルはUTF-8 BOMなし・LFで既存の先行結果記録と同じ形式、元の本体・テストはBOMあり・CRLFのままで、改行の混在はない。差分検査に成功し、記録へのホスト固有パスや認証情報の混入がないことを確認した。

全消費とノード数の確認はこのサンプルでの検証であり、全キー・値の比較や任意入力のJSON規格適合を保証しない。性能は.NET 10のみで、netstandard2.0を利用する別ランタイムでは未測定。CPUの実効クロック、温度、他プロセスの負荷は記録しておらず、大きな揺れの原因を特定するには不足している。

## 再現方法と次の判断

固定コピーのoriginal/candidateを作り、上記2行の差分だけで比較する。コピーのルートを作業ディレクトリとし、実装ごとに別のartifactsパスを指定する。通常測定はoriginal→candidate→candidate→originalの順に、順次実行する。

```powershell
dotnet build Kotowari.Benchmarks -c Release --no-restore -p:UseSharedCompilation=false
dotnet run --project Kotowari.Benchmarks -c Release --no-build -- --validate
dotnet run --project Kotowari.Benchmarks -c Release --no-build -- --filter '*JsonParserBenchmarks.Kotowari*' --job short --launchCount 3 --warmupCount 10 --iterationCount 10 --invocationCount 4 --unrollFactor 1 --outliers DontRemove --affinity 4 --artifacts artifacts/reproduction/normal
```

JIT切り分けは同じコマンドへ `--envVars DOTNET_TieredCompilation:0` を追加し、別のartifactsパスにする。プロファイルはLaunchCountを1にし、`--profiler EP` を追加して別に記録する。本調査時にBenchmarkDotNet 0.15.8のヘルプでenvVarsの書式を確認し、プロファイラーの実行とトレース出力も確認した。

全体速度を採用根拠にする前に、通常ランタイム条件でJITが落ち着くウォームアップと複数起動の比較を整え、環境変動の影響を再確認するのが次の測定上の課題である。別の最適化を混ぜて差の帰属を失わない。Span、結果構築、空白処理、Cursolの変更はこの調査では実施していない。コミット・pushも未実施。
