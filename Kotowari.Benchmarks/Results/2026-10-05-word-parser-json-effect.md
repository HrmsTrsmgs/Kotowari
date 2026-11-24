# WordParser変更前後の巨大JSON比較（2026-10-05）

## 結果と判断

元のList版と採用済みの遅延配列版を、用意済みの4.17 MB JSON全体の解析・結果生成で比較した。
**割り当てメモリの約8.8%削減を確認した。一方、JSON全体の安定した速度改善は確認できなかった。**
[単語の個別測定](2026-10-05-word-parser-production-deferred-array.md)での成功時約30%短縮を、JSON全体の改善率とは扱わない。

各版3独立起動、各起動8反復。次の中央値は「起動ごとの平均」を3個集めた中央値であり、中央値の信頼区間は算出していない。
割り当ても3起動の中央値を示す。MiBは1,048,576 bytes。各起動の元精度はCSVに保存した。

| 指標 | 元List | 遅延配列 | 差の解釈 |
|---|---:|---:|---|
| 起動平均の中央値 (ms/op) | 255.77 | 251.28 | 約1.75%短いが、ばらつきに対して小さく改善と断定しない |
| 3起動すべての平均 (ms/op) | 238.66 | 252.19 | 遅延配列が約5.7%長い。中央値と方向が逆転する |
| 割り当ての中央値 (MiB/op) | 63.09 | 57.54 | 約5.55 MiB / 約8.8%減少 |
| Gen0 / Gen1 / Gen2（1000操作あたり） | 5250 / 2250 / 750 | 4500 / 1750 / 500 | 各版の全3起動で一致 |

元Listの `05-list` が大幅に速い。これを捨てず、平均・中央値の両方を示した。
起動内の99.9%信頼区間が重なるケースもあり、元Listの最速起動は遅延配列の全起動より速かった。
原因未確認の起動差があるため、約1.75%改善とも約5.7%悪化とも安定した効果として結論づけない。
一方、全3起動の割り当て範囲は元List約63.087～63.089 MiB、遅延配列約57.535～57.537 MiBで分離した。

## 起動別結果

順序はList→遅延配列→遅延配列→List→List→遅延配列（ABBAAB）。
Errorは各起動のBenchmarkDotNetによる99.9%信頼区間半幅であり、3起動間の誤差ではない。

| Run | 実装 | Mean (ms) | Error (ms) | 割り当て (B/op) |
|---|---|---:|---:|---:|
| 01-list | 元List | 255.77 | 9.80 | 66,151,628 |
| 02-deferred | 遅延配列 | 250.19 | 14.62 | 60,330,196 |
| 03-deferred | 遅延配列 | 251.28 | 9.46 | 60,330,116 |
| 04-list | 元List | 264.08 | 16.05 | 66,153,620 |
| 05-list | 元List | 196.15 | 28.66 | 66,151,332 |
| 06-deferred | 遅延配列 | 255.09 | 15.03 | 60,331,604 |

多峰性・最短反復時間の警告なし。01/03/04/06に外れ値検出のヒント各1件があり、DontRemoveのまますべて残した。
05-listのActual反復は181.60～222.65 ms/op、最後のWarmupは178.57 ms/op。他の起動と異なる速度帯だが、原因は調査していない。
電力・温度・実効クロック・JIT/PGO・GC停止時間のトレースは取得しておらず、どれかを原因と断定しない。
前の[他ライブラリ比較](2026-10-05-json-combinators.md)の176.87 msを今回の修正後の値として混ぜない。絶対値の系列間差から変更効果を計算しない。

## 対象ソースと正しさの確認

変更後の基点は `9b90acbef716cd0dd8208014397eafc742a79d84`（実装は `8562efc0757e9c936693b3cb2ced3aef1febe511` と同じ）。
元WordParserだけを `15dbfedbb6df18d816e8264d66f7798d8338ade7:Kotowari/Core/WordParser.cs` のGit blobへ替えた。
元コミットの古いフレームワーク設定を混ぜず、両版とも現在のnet10.0設定を使った。
ライブラリとJSONサンプルを固定コピーへ取り、実際の型名・公開範囲・AssemblyName `Marimo.Kotowari` / `Marimo.Parser` を維持した。
比較専用の継承やディスパッチを本体へ加えていない。

`git ls-files -- Kotowari JsonParser` の全ファイルと、global.json・.editorconfig・Directory.Packages.props・README.md・既存Program.cs・JsonParserBenchmarks.cs・ComparisonJsonParsers.cs・Data/test.jsonの35ファイルを対にして照合した。
測定前後とも、WordParser以外のファイルのSHA-256が一致。本体作業ツリーも変更していない。

| 識別情報 | SHA-256 |
|---|---|
| 元WordParser.cs | `A3AE30CEB5CCB7EF44FFC05D313706BA9900C202E95FF6A37380DD0D7FBFAA3A` |
| 遅延配列WordParser.cs（採用理由コメント込み） | `F5787D24D135E47D33BFD697661444B8D86A98E6DC958B6ABCF07614FDED415A` |
| source-manifest.json | `DD1D9C303B24EA6336EBAF69AD59748946803AB150ED925DCC0175A258244EAA` |
| 共通Measurement.csprojテンプレート | `7FF2D788C5465D232911AA53E297A96FB3AF1F017EEF35CC73B720FEDED5A14E` |

測定前後に両版を別プロセスで検証し、4,172,062文字の全文消費・成功・100,001ノードを確認した。
全キー・値・配列順序の正規化した木のSHA-256は両版とも `5ABDB9015EF96B7E79FADB54B636AF0C5AB9D0AA1864C5AF21FF8669C2A4233C`。
正規化は前のコンビネーター比較と同じ方式（キーOrdinal順、配列順保持、種類・要素数・長さ付きの値）。検証やハッシュ計算は測定区間外。

## 入力・環境・測定条件

- 入力は既存 `Kotowari.Benchmarks/Data/test.json`。4,172,062 bytes / UTF-16コード単位。変更・縮小・再生成なし。
- 入力SHA-256: `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。
- CPU: Intel Core i7-13700H、1 CPU / 14物理 / 20論理コア。
- Windows 11 10.0.26200.9457 / 25H2。SDK 10.0.401、Runtime .NET 10.0.12（10.0.1226.42308）、X64 RyuJIT、Concurrent Workstation GC。
- BenchmarkDotNet 0.15.8、Release / Optimize=true。通常のランタイム設定でTieredCompilation・TieredPGOの環境変数上書きなし。
- 共通ホストの依存版はPidgin 3.5.1、Sprache 2.3.1、Superpower 3.2.1、SpracheJSON 1.0.1。今回はfilterでKotowariだけを実測し、他ライブラリ・専用JSONパーサーは再測定していない。
- 各run: Short、LaunchCount=1、WarmupCount=10、IterationCount=8、IterationTime=500ms目標、InvocationCount自動、UnrollFactor=1、Affinity=4、OutlierMode=DontRemove、JSON exporter / keepFiles / join。
- 全runで1反復4操作。6独立起動、各8反復の計48測定反復。Actual/Result各48行を96回の独立反復とは扱わない。
- 同時開発・検証buildなしで直列実行。BDN自身のホスト生成buildは含む。BDNが高パフォーマンス電源プランへ切り替え、各run終了時にバランスへ復元した。
- 6runのBDN総時間の合計208.17秒（約3分28秒）。全6測定プロセスexit 0、各run N=8。欠測・実測0件・実行失敗なし。

既存の `JsonParserBenchmarks.Kotowari()` は全文を変更せずリンクして使用した。
ファイル読み込みはGlobalSetup、測定区間は `JSON.Parse` による解析と結果生成（入力char[]化を含む）。
既存入口と同じDisableOptimizationsValidator設定であり、実際のJIT最適化を無効にしたものではない。

## 再現とraw記録

固定コピー・準備/実行スクリプト・検証補助・ログ・完全JSONは、Git対象外の `artifacts/word-parser-json-effect-2026-10-05/` に保持する。
再現時は上記35ファイルを同じ相対位置で `snapshots/list/` と `snapshots/deferred/` に置き、List側のWordParserだけを上記Git blobのbytesで置き換える。
共通ホストテンプレートは[コンビネーター比較のホスト](2026-10-05-json-combinators.md#条件と再現方法)と同じ依存・設定で、参照をsnapshot内へ変更した。
具体的にはProjectReferenceを `../JsonParser/JsonParser.csproj`、Compileを `../Kotowari.Benchmarks/{Program,JsonParserBenchmarks,ComparisonJsonParsers}.cs`、Noneを `../Kotowari.Benchmarks/Data/test.json` とする。
ホストcsproj名は `WordParserJson.list.Measurement.csproj` / `WordParserJson.deferred.Measurement.csproj` と分け、同名csprojの再帰探索衝突を避ける。
両ホストをReleaseでbuildし、全文検証後、リポジトリrootから次をABBAAB順で実行する。保存先は既存runへ上書きせず新しい空のディレクトリを使う。

```powershell
dotnet artifacts/word-parser-json-effect-2026-10-05/snapshots/<variant>/host/bin/Release/net10.0/WordParserJson.<variant>.Measurement.dll --filter '*JsonParserBenchmarks.Kotowari' --job Short --launchCount 1 --warmupCount 10 --iterationCount 8 --iterationTime 500 --unrollFactor 1 --affinity 4 --outliers DontRemove --exporters json --artifacts artifacts/word-parser-json-effect-2026-10-05/<run> --keepFiles --join
```

復元は既存ローカルNuGetキャッシュのみを使用し、パッケージ追加・更新・外部取得なし。
各runの完全JSONは `<run>/results/Marimo.Kotowari.Benchmarks.JsonParserBenchmarks-report-full-compressed.json`。ログは各run直下の日時付き `.log`。

| Run | 完全JSONのSHA-256 |
|---|---|
| 01-list | `83988504E469C31252970EE092F0B7E3F5578421C30E7FFD15C618A4F6A4B7A0` |
| 02-deferred | `8F3D99829AC5917861A355475DB1C1B1ABA9C48F3AB52A829C59964251AD3844` |
| 03-deferred | `234E3F32388A0C6F90AF69C52545E315EF95D109BFD2992CC9E88E700AA1EC9C` |
| 04-list | `BB456883668D54B3C783A34A5B03BC6116CA84EA6C091E68401076B4E5408578` |
| 05-list | `A1BACEE89FB52DEFB8C2DAFDFF31DF495AC1DBD2470A3A4E6E0476E6374C2F99` |
| 06-deferred | `9ABC541720D93FCB4F165E9CF591C10C24368310D037AC1AB64C506113609B0C` |

## 保存・検証

`Experiment=2026-10-05-word-parser-json-effect` として、既存の [summary.csv](word-parser/summary.csv) に6行、[iterations.csv](word-parser/iterations.csv) に96行、[launches.csv](word-parser/launches.csv) に6行を追記した。新しいCSVは作っていない。
時間は元精度のns、割り当てはbytesで保存。単一起動のrunなので、そのrunのMemoryDiagnoser結果を起動行にも保存した。
CSV読取監査は列数26/15/19と実験別件数・単位を確認。追記前のbytesのSHA-256一致、全6runの統計・起動平均・メモリとraw JSONの一致、全96行のActual/Resultの操作数・総時間・時間換算・Result平均を確認した。索引の合計は257/6,484/407行。記録・索引・CSVはUTF-8/BOMなし・LFのみで、`git diff --check` は成功（既存のGit改行変換警告あり）。

両ホスト・両検証補助のRelease buildは成功（準備33.72秒）。既存SpracheJSON参照のNU1701警告あり。
全文消費・結果ハッシュ・固定コピーと作業ツリーのソースハッシュは測定前後とも一致した。
本体・既存テスト・設定に変更はなく、元/変更後WordParserの既存検証済み実装を測定したため、全テスト・Analyzerの形式的な再実行はしていない。
今回の実測は通常設定のみ。起動間の速度差の原因調査と、条件を変えた追試は未実施。
前の比較記録は `9b90acb` にコミット済み。この新しい前後比較記録は未コミットで、pushは行っていない。
