# 巨大JSON測定のばらつき調査（2026-10-05）

## 結論

**通常設定で約170msになる状態を安定して再現する対策は、まだ見つかっていない。**
PGO有効のままウォームアップを10→50→200反復へ増やして検証した。
200反復はJSON解析800回に相当するが、その後の3起動平均は250.84 / 256.65 / 253.21msだった。
単に長く待てば約170msへ収束するという結果にはならなかった。さらに長い時間の可能性まで否定するものではない。

PGO無効は原因の切り分けだけに使用した。PGO有効時の最適化を目指す主比較には採用しない。
実際、PGO無効の再検証も今回の安定性基準を満たさなかった。
遅い速度帯で値がそろうことを、目標達成とは扱わない。速い起動や外れ値は削除していない。
ライブラリ本体・公開API・ランタイムの恒久設定は変更していない。

## 観測結果

CVは標本標準偏差 / 平均 × 100。起動間CVは起動平均から計算し、起動内CVは各起動の反復CVの平均。
起動内の反復を独立起動と数えない。次の表は別の測定系列を一括平均したものではない。

| Run | 条件 | 起動数 | 各起動の平均 (ms) | 起動間CV | 起動内CV平均 |
|---|---|---:|---|---:|---:|
| 01/04/05/08/09 | 通常、warmup 10、actual 8 | 5 | 254.78 / 260.99 / **169.77** / 252.14 / 253.30 | 16.12% | 4.63% |
| 02/03/06/07/10 | PGO無効、warmup 10、actual 8 | 5 | 263.56 / 278.08 / 268.97 / 280.55 / 260.82 | 3.22% | 7.70% |
| 17 | 通常、warmup 50、actual 16、外部負荷あり | 5 | 280.65 / 252.95 / 250.39 / 249.72 / 254.14 | 5.06% | 5.32% |
| 18 | 通常、warmup 10、actual 16、編集・追加端末起動を停止 | 5 | 252.35 / 250.35 / 248.62 / 247.06 / 253.42 | 1.04% | 2.48% |
| 19 | 通常、warmup 50、actual 16、同上 | 5 | 247.60 / 254.16 / **216.80 / 211.95** / 251.69 | 8.61% | 3.69% |
| 20 | PGO無効、warmup 50、actual 16、同上 | 5 | 254.62 / 260.29 / 263.94 / 278.68 / 258.91 | 3.50% | 5.54% |
| 21/22/23 | 通常、warmup 200、actual 16、同上 | 3 | 250.84 / 256.65 / 253.21 | — | — |

11～16はwarmup 50 / actual 16の予備試行。順に通常252.66、PGO無効263.37、PGO無効262.15、通常251.77、通常257.13、PGO無効263.37ms。
全23runを既存CSVへ保存した。18だけを選んで「通常設定のばらつきは解消した」とは結論づけない。
50反復への延長も、別の5起動検証で速度帯が分かれたため解決策として採用しない。

## 外部負荷とCPU状態

17の1起動目では、通常約250msの反復が393.67msまで延びた。
同じ時間帯にVisual Studio、そこから起動したMSBuildなどのCPU使用量が増え、測定CPUのProcessor Performanceカウンターが約125から約83へ下がった。
MSBuildの親プロセスがdevenvであることを確認した。ベンチマーク自身の準備buildと区別している。
初期の測定中にはエージェント側でもファイル編集・別端末での確認を行っていた。これらの影響も排除できないため、17を対策成功の検証として扱わない。
18以降は準備と集計を実測の前後に分け、測定中の編集・追加端末起動を停止した。

ただし、外部負荷だけでは説明できない起動差も残った。
19の速い2起動のCPU性能カウンター中央値は約123、他の3起動は約126だった。
速い起動だけが高いCPU性能状態だったわけではない。これはプロセスCPU使用量が1コアの80%を超えたサンプルでの集計であり、各Actual反復の厳密なクロック補正ではない。
温度、GC停止時間、速い/遅い起動の生成コード差は未確認。PGOやJITを唯一の原因と断定しない。

CPUカウンターは2秒間隔、記録プロセスは測定CPUとは別の論理CPU 16へ固定した。
11～16はCPUカウンター、17以降はプロセス別CPU使用量も記録した。01～10には連続カウンター記録がない。
記録自体の負荷はゼロではなく、カウンター値を温度や厳密な実効クロックの測定と同一視しない。

## 測定条件と再現

- 対象実装は `a4a37bb` 時点と同じ遅延配列版WordParser。実装コミットは `8562efc0757e9c936693b3cb2ced3aef1febe511`。
- [前後比較](2026-10-05-word-parser-json-effect.md)の固定コピー `artifacts/word-parser-json-effect-2026-10-05/snapshots/deferred/` を再利用。各run開始前にsource-manifestの35ファイルのSHA-256を照合した。
- 既存 `JsonParserBenchmarks.Kotowari()`、入力、ライブラリを変更していない。ファイル読み込みは測定外、解析・結果生成・入力char[]化は測定内。
- 入力4,172,062 bytes、SHA-256 `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。
- Windows 11 10.0.26200.9457、Intel Core i7-13700H、SDK 10.0.401、.NET 10.0.12、X64 RyuJIT、Concurrent Workstation GC。
- BenchmarkDotNet 0.15.8。ホスト依存はPidgin 3.5.1、Sprache 2.3.1、Superpower 3.2.1、SpracheJSON 1.0.1。今回の実測対象はKotowariだけ。
- Release / Short、InvocationCount=4、UnrollFactor=1、Affinity=4、OutlierMode=DontRemove、MemoryDiagnoser、JSON exporter。
- 反復内呼び出し数を4に固定した。以前の自動Pilotありの測定と完全に同じ起動過程とは扱わない。
- 01～16および21～23はLaunchCount=1。17～20はLaunchCount=5。Warmup/Actual数は上記表のとおり。
- PGO無効のrunだけ子プロセスへ `DOTNET_TieredPGO=0` を指定。通常runには上書きなし。段階的JIT無効の試験は今回行っていない。
- BDNが一時使用した高パフォーマンス電源プランは、終了後バランスに復元された。依存追加・更新・外部パッケージ取得なし。

同じ固定コピーを用意した上で、次のコマンドの回数と保存先を各runに合わせる。既存保存先へ上書きしない。

```powershell
dotnet artifacts/word-parser-json-effect-2026-10-05/snapshots/deferred/host/bin/Release/net10.0/WordParserJson.deferred.Measurement.dll --filter '*JsonParserBenchmarks.Kotowari' --job Short --launchCount 1 --warmupCount 200 --iterationCount 16 --invocationCount 4 --unrollFactor 1 --affinity 4 --outliers DontRemove --exporters json --artifacts artifacts/reproduction-normal-warm200 --keepFiles --join
```

PGO無効の切り分けは `--envVars DOTNET_TieredPGO:0` を追加したもの。主比較には使用しない。
設定の意味は[Microsoftのランタイム設定](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/compilation)と[BenchmarkDotNetのJobs](https://benchmarkdotnet.org/articles/configs/jobs.html)、使用中の0.15.8のCLIヘルプで確認した。

## 検証と保存

通常設定とPGO無効それぞれで、測定外の検証プロセスが全文消費4,172,062文字、100,001ノード、正規化した木のSHA-256 `5ABDB9015EF96B7E79FADB54B636AF0C5AB9D0AA1864C5AF21FF8669C2A4233C` を確認した。
これは各計時反復の生成結果をハッシュ検証したという意味ではない。
全23run・39測定プロセスが完了し、各runのNと予定反復数を検証した。実測544反復、Actual/Result各544行。

既存CSVへ `Experiment=2026-10-05-benchmark-variance` としてsummary 23行、iterations 1,088行、launches 39行を追記した。
CSV追加前のbytesをSHA-256で照合し、既存行を維持した。複数起動のメモリ診断集計を各起動へ複製せず、該当launchesのメモリ欄は空欄にした。
raw JSON・ログ・カウンター・実行/集計補助はGit対象外の `artifacts/benchmark-variance-2026-10-05/` に保持する。
各runのJSONは `RUN/results/Marimo.Kotowari.Benchmarks.JsonParserBenchmarks-report-full-compressed.json`。

| Run | JSON SHA-256 |
|---|---|
| 05-normal（約170ms） | `F233FC08BADBF212D128608F1BD770DF32EC65D95FC80728CE2462A0F3551CB5` |
| 17-normal-warm50-validation | `4B898F02E9D6F699D44F3A4AAF3A3A50CD79C764F1DAA7996C918E0B074CCC7E` |
| 18-normal-quiet-warm10 | `B0AFA7639881B073EA448E9804A6276299128C32DBE9B8712F4A33F19212B0DA` |
| 19-normal-quiet-warm50 | `B17C7DAC4C463506EFEC7C1E92BA13B21A216F31DA789F7F1787FFAD8281B121` |
| 20-pgo-off-quiet-warm50 | `60EA6CDF079DE0EA8BAAB1036A2C60AB8BB659382149385A104152056AF82F01` |
| 21-normal-warm200 | `D1ECB5315CD0EF8CF132B7237644353D1F2DFBEEB6390E8B28FA4C963231A7B4` |
| 22-normal-warm200 | `EFF35773BA0C33BDDCBBC8749BEE761076E4C2A535EA31A7EC0C1A1153740626` |
| 23-normal-warm200 | `3E33BC67E43347F347307666F03AD805FCC85073B5676A07F8141F7FC7AA4007` |

`Check-Stability.ps1` はResult反復だけから起動別統計を出し、既定では5起動以上・起動間CV 3%以下・起動内CV平均5%以下を確認する。これは今回の運用上の判定基準で、統計的有意差の保証や速い実行状態への到達判定ではない。
初期通常5起動、17、19、20は不合格、18は合格となった。18の合格だけを根拠に通常設定全体の問題が解決したとは扱わない。
生データを除外・書き換えず、同じ実装・入力・ランタイム・ジョブの報告を渡して使う。

```powershell
pwsh -File Kotowari.Benchmarks/Check-Stability.ps1 -ReportPath artifacts/benchmark-variance-2026-10-05/19-normal-quiet-warm50/results/Marimo.Kotowari.Benchmarks.JsonParserBenchmarks-report-full-compressed.json
```

本体と既存テストの変更がないため、全テスト・C# Analyzerの形式的な再実行はしていない。
EventPipe解析用の補助は準備・buildしたが、今回新しいトレースは採取していない。
次の論点は、PGO有効で速い起動と遅い起動の生成コード・最適化内容がどう違うかであり、1時間の作業区切りを超えて追試していない。
