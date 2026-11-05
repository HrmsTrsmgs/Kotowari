# JSON同期前後の簡易速度比較（2026-11-05）

## 結果

同じ4,172,062-byte入力を、同期前 `ae69e2a`、同期後 `cd1d29488af98f897ae7d4ef628e74bc033ac07f` のKotowari JSONパーサーで直列測定した。測定は各版3独立起動×5反復（各N=15）。大きな変化の有無を見る簡易比較で、精密な性能評価ではない。

| 版 | 平均 | 標準偏差 | 99.9% CI半幅 | 割り当て | 生成ノード数 |
|---|---:|---:|---:|---:|---:|
| `ae69e2a` | 116.37 ms | 17.35 ms | 18.55 ms | 35.60 MiB | 100,001 |
| `cd1d294` | 126.78 ms | 8.56 ms | 9.15 ms | 34.54 MiB | 104,120 |

観測値では同期後が約8.9%遅く、割り当ては約3.0%少ない。起動間のばらつきもあり、各版3起動だけのため、小差の優劣を確定しない。

実際の反復時間（ms/op、外れ値を除外しない）:

| 版 | 起動1 | 起動2 | 起動3 |
|---|---|---|---|
| `ae69e2a` | 137.054, 153.368, 132.403, 148.038, 110.637 | 113.768, 111.847, 102.454, 106.075, 107.960 | 102.948, 107.135, 104.119, 102.899, 104.888 |
| `cd1d294` | 132.726, 131.486, 131.596, 137.403, 130.457 | 114.576, 115.104, 115.099, 112.152, 120.914 | 131.950, 131.646, 129.938, 134.496, 132.190 |

両版で既存の `--validate` は成功した。追加確認として、各版の内部root parserを実行し、成功結果のカーソル位置が入力長と一致することを確認した（4,172,062 / 4,172,062）。部分解析の時間は含まれていない。

ただし出力ノード数は一致しない（100,001対104,120）。同じ入力全体を消費してはいるが、生成結果・処理量が同じとは確認できないため、厳密な同一仕事量比較ではない。文法同期によるこの差の内訳は今回調べていない。時間差を同期自体の純粋な性能影響と断定しない。

## 条件

* 入力: `Kotowari.Benchmarks/Data/test.json`。4,172,062 bytes、SHA-256 `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。両コミットの内容一致。
* コミット: `ae69e2a` (`git rev-parse ae69e2a`), `cd1d29488af98f897ae7d4ef628e74bc033ac07f`。
* CPU: Intel Core i7-13700H。OS: Windows 11 10.0.26200.9711 / 25H2。SDK 10.0.401、.NET 10.0.12 (10.0.1226.42308)、x64 RyuJIT、Release、Concurrent Workstation GC。
* BenchmarkDotNet 0.15.8。`Short`, LaunchCount=3, WarmupCount=4, IterationCount=5, IterationTime=250 ms, UnrollFactor=1, affinity=4, OutlierMode=DontRemove。`DOTNET_TieredPGO=1`, `DOTNET_ReadyToRun=0` を測定子プロセスへ設定。両版同じ条件。
* BDNは電源プラン変更に失敗した旨を表示。CPU名等のCIM情報は取得不能でBDNの環境表記がUnknownとなった。OS・CPUはPowerShellから別途確認。測定中、他のビルド・重い開発作業は実行していない。
* BenchmarkDotNetの生JSONと起動ログ、一時snapshot/hostはローカル `artifacts/json-speed-snapshots-2026-11-05/` に保存。snapshotはgit archiveで作成し、Git履歴を書き換えていない。本体・既存ベンチマークソースは変更していない。

各起動の反復値、平均・ばらつきは `artifacts/json-speed-snapshots-2026-11-05/ae69e2a/artifacts/run-ae69e2a/results/` と `artifacts/json-speed-snapshots-2026-11-05/cd1d294/artifacts/run-cd1d294/results/` の完全JSONを参照。MiBは1,048,576 bytes。

## 最新SpracheJsonとの比較

同じ入力を、Kotowari `cd1d29488af98f897ae7d4ef628e74bc033ac07f` と IanWold/SpracheJson `aa8092aa4f5a7f1675200ac908d20c06e285aec4`（Sprache `3.0.0-develop-00049`）で、同一ホスト・同一条件の直列測定を行った。各実装3独立起動×5反復（各N=15）。これはこの入力・実行環境で観測した簡易比較で、一般的な速度優劣や参照元同期の純粋な因果効果を確定するものではない。

| 実装 | 平均 | 標準偏差 | 99.9% CI半幅 | 割り当て/解析 | ノード数 |
|---|---:|---:|---:|---:|---:|
| Kotowari `cd1d294` | 123.390 ms | 5.195 ms | 5.554 ms | 34.54 MiB | 104,120 |
| SpracheJson `aa8092aa` | 1,033.9 ms | 38.38 ms | 41.03 ms | 2,600.2 MiB | 104,120 |

この条件ではSpracheJsonの平均時間はKotowariの約8.379倍、割り当ては約75.3倍だった。信頼区間は重ならない。SpracheJsonで2反復がBDNの外れ値判定対象と表示されたが、`DontRemove` 設定により除外せず統計へ含めた。サンプル数は各15であり、この観測を他の入力や環境へ一般化しない。

各起動の実測時間（ms/op、外れ値を除外しない）:

| 実装 | 起動1 | 起動2 | 起動3 |
|---|---|---|---|
| Kotowari | 124.0241, 122.9268, 125.6346, 129.7657, 133.5398 | 118.6411, 117.5712, 116.2278, 115.4588, 126.3337 | 122.2356, 120.9195, 124.1308, 123.6397, 129.8063 |
| SpracheJson | 1102.2375, 1124.9779, 1022.8181, 988.2845, 1056.7913 | 1032.3327, 1012.1011, 1038.6046, 1020.1486, 1053.4479 | 1006.7676, 1026.3898, 1031.3503, 1005.5485, 986.3771 |

計時メソッドはそれぞれ `Marimo.Parser.Json.Parse(json)` と `SpracheJson.Json.Parse(json)` を呼び、解析結果を返す。両者とも `GlobalSetup` で同じファイルを文字列に読み込むため、ファイルI/Oは計時に含まない。計時前の確認では、両内部root parserが入力末尾まで消費し、各104,120ノードを生成した。ノード種別、literalの型・値、objectのキーと順序、arrayの順序も全て一致した。

なお、両公開 `Json.Parse` 入口は、同じ入力末尾に `!` を加えた文字列も受理した。したがって任意の後続文字を拒否する文書終端検査を保証するAPIではない。一方、今回の測定入力そのものでは内部root parserの最終位置が各4,172,062/4,172,062文字であり、測定対象の入力全体を解析したことを別途確認している。末尾文字を加えた入力は計時していない。

### 最新版比較の条件

* 入力: `Kotowari.Benchmarks/Data/test.json`。4,172,062 bytes/chars、SHA-256 `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。
* 実装: Kotowari `cd1d29488af98f897ae7d4ef628e74bc033ac07f` と `SpracheJson.Reference/UPSTREAM.json` に記録した IanWold/SpracheJson `aa8092aa4f5a7f1675200ac908d20c06e285aec4`、Sprache `3.0.0-develop-00049`。参照プロジェクトは固定snapshot内のソースを使用。
* CPU: 13th Gen Intel Core i7-13700H（レジストリから確認。CIMアクセスは不可）。OS: Windows 11 10.0.26200.9711 / 25H2。SDK 10.0.401、.NET 10.0.12 (10.0.1226.42308)、x64 RyuJIT、Release、Concurrent Workstation GC。
* BenchmarkDotNet 0.15.8。`Short-3x5`, LaunchCount=3, WarmupCount=4, IterationCount=5, IterationTime=250 ms, UnrollFactor=1, affinity mask=4, OutlierMode=DontRemove。測定子プロセスに `DOTNET_TieredPGO=1`, `DOTNET_ReadyToRun=0` を設定。順にKotowari、SpracheJsonを測定し、同時に重い処理は行っていない。
* BDNは電源プラン変更に失敗し、CIMからCPU情報を取得できず環境表のCPUはUnknown。CPU名・OSは個別確認した。外部脆弱性DBへのアクセスを避けるため一時ホストの復元ではNuGetAuditを無効化。新しい依存は追加せず、既存のSprache 3.0.0-develop-00049とBenchmarkDotNet 0.15.8を参照した。固定snapshot内の既存Release DLLとSpracheJson参照DLLを使い、本体・固定原本・ベンチマーク本体のソースは変更していない。
* 成功した測定ログとCSV・HTMLはローカル `artifacts/json-speed-snapshots-2026-11-05/artifacts/compare-json-latest/` に保存。準備中のBDN build/setup失敗試行ログも同ディレクトリにあり、いずれも workload measurements は0件で本結果に含めていない。MiBは1,048,576 bytes。
