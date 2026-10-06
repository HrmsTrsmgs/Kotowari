# ベンチマーク測定記録

測定記録には、対象実装、入力、環境・依存バージョン、測定条件、結果と判断を残す。
結果を再現する際は、各記録に示したソースの版と条件を使う。

## JSONパーサー比較

- [2026-08-04](2026-08-04-json.md)
- [2026-09-14の基準値](2026-09-14-json-baseline.md)と[CSV](2026-09-14-json-baseline.csv)
- [2026-10-05の巨大JSON・パーサーコンビネーター比較](2026-10-05-json-combinators.md)（専用JSONパーサーは測定対象外）
- [2026-10-07の文字列・UTF-8入力経路比較](2026-10-07-utf8-stream-json.md)
- [2026-10-07のUTF-8カーソル二重復号削除](2026-10-07-utf8-cursor-single-decode.md)

## WordParserの測定

同じテーマのデータは、測定のたびにCSVを増やさず、`word-parser/` の種類別CSVへ追記する。
`Experiment` で測定記録を、`Run` でその中の実行を識別する。
WordParser変更後のJSON全体の性能確認も、この既存データ群に含める。

| Experiment | 測定記録 | summary | iterations | launches | stacks |
|---|---|---:|---:|---:|---:|
| `2026-10-04-cache` | [先行生成・配列保持](2026-10-04-word-parser.md) | 56 | 80 | — | — |
| `2026-10-04-investigation` | [性能差の追加調査](2026-10-04-word-parser-investigation.md) | — | 200 | 20 | 173 |
| `2026-10-04-direct-match` | [直接照合](2026-10-04-word-parser-direct-match.md) | 16 | 960 | 48 | — |
| `2026-10-05-accumulation` | [文字蓄積方式の比較](2026-10-05-word-parser-accumulation.md) | 138 | 4,332 | 282 | — |
| `2026-10-05-production-deferred-array` | [本体のList版と遅延配列版](2026-10-05-word-parser-production-deferred-array.md) | 36 | 576 | 36 | — |
| `2026-10-05-json-combinators` | [巨大JSON・コンビネーター比較](2026-10-05-json-combinators.md) | 5 | 240 | 15 | — |
| `2026-10-05-word-parser-json-effect` | [WordParser変更前後の巨大JSON比較](2026-10-05-word-parser-json-effect.md) | 6 | 96 | 6 | — |
| `2026-10-05-cursol-readonly-json` | [Cursol読み取り専用化の巨大JSON全体測定](2026-10-05-cursol-readonly-json.md) | 16 | 256 | 16 | — |
| `2026-10-05-benchmark-variance` | [ばらつき・長時間ウォームアップの調査](2026-10-05-benchmark-variance.md) | 23 | 1,088 | 39 | — |
| `2026-10-05-pgo-codegen` | [PGOの速度帯・Tier 1生成コード比較](2026-10-05-pgo-codegen.md) | 32 | 1,024 | 32 | — |
| `2026-10-05-pgo-callsite` | [内部呼び出し位置への型profile分離](2026-10-05-pgo-callsite.md) | 34 | 1,088 | 34 | — |
| `2026-10-05-measurement-method` | [PGO有効での測定条件・所要時間の検証](2026-10-05-pgo-callsite.md#測定方法の追加検証) | 52 | 1,088 | 52 | — |
| `2026-10-06-cursol-string-document-json` | [Cursolの文字列Document化・巨大JSON全体測定](2026-10-06-cursol-string-document-json.md) | 16 | 256 | 16 | — |
| `2026-10-06-cursol-document-inheritance-json` | [Cursolの継承分離・巨大JSON全体測定](2026-10-06-cursol-document-inheritance-json.md) | 18 | 288 | 18 | — |
| `2026-10-06-cursol-utf8-three-way-json` | [UTF-8対応前・分岐版・継承版の巨大JSON全体測定](2026-10-06-cursol-utf8-three-way-json.md) | 24 | 384 | 24 | — |
| `2026-10-07-utf8-stream-json` | [文字列・UTF-8入力の比較](2026-10-07-utf8-stream-json.md) | 16 | 256 | 16 | — |
| `2026-10-07-utf8-cursor-single-decode` | [UTF-8カーソルの二重復号削除](2026-10-07-utf8-cursor-single-decode.md) | 24 | 384 | 24 | 171 |
| 合計 | | 512 | 12,596 | 678 | 344 |

各データの粒度:

- [summary.csv](word-parser/summary.csv): 実行・ベンチマーク・入力ごとの統計、割り当て、GC、記録済みのジョブ条件。
- [iterations.csv](word-parser/iterations.csv): 反復ごとの操作数・総時間・1操作あたりの時間。
- [launches.csv](word-parser/launches.csv): 起動ごとの反復統計、割り当て、GC。
- [stacks.csv](word-parser/stacks.csv): プロファイルのメソッド別サンプル数と割合。

列の扱い:

- `Benchmark` は型名とメソッド名を `.` で結ぶ。`Parameters` は単体入力を `Text=...` または測定クラスの `Case=...` で示し、JSON全体は空欄。
- 時間の数値と `TimeUnit` (`ns` / `ms`) を分ける。旧データは元の精度を保ち、換算・再計算・丸め直しをしていない。新しい完全JSON由来の反復・起動統計は各測定記録の計算方法に従う。反復の `TotalNanoseconds` は単位が常にns。
- `Allocated` と `AllocationUnit` (`B` / `MiB`) を分ける。元のBenchmarkDotNet表示の `MB` は1,048,576 bytesのため、数値を変えず `MiB` と明記した。割り当ては1操作あたりの累積マネージド割り当て。
- `Gen0Per1000` / `Gen1Per1000` / `Gen2Per1000` は1,000操作あたりのGC回数。
- `N` は元の統計に記録された反復数。空欄は元CSVに記録のない情報であり、0で補っていない。その他の空欄も同じ扱い。
- `MeasurementKind` と `Environment` は、通常測定・段階的JIT無効・プロファイル付き測定を区別する。条件が違う行を主比較へ混ぜない。
- `Stage` の `Actual` と `Result` は、同じ反復の生値とオーバーヘッド補正値。直接照合の960行は480反復に対応し、960回の独立試行ではない。元CSVにStageのない行は空欄のまま。
- `Launch` は起動番号。起動内の反復を独立した起動として数えない。先行生成の反復CSVには起動番号がないため補っていない。
- 起動別の割り当て・GCが記録されていない場合は空欄とする。全起動合計の診断値を、各起動の独立した観測として複製しない。
- `Error` は記録された99.9%信頼区間の半幅。`Percent` はプロファイルの実測スレッドサンプルに対する割合で、壁時計時間の正確な比率ではない。`Scope` の子呼び出し込みの割合は重なるため合計しない。

## ソースの保存と既存データの移行

直接照合版と互換性テスト・測定記録は、コミット `6db4bcbf6a028230f7e150bb9ac4992c316ed22a` に保存した。
その測定の配列保持版は `721906f70471c90f2c41b4c5da8e1ac6712b2cad`。
これらのSHAを測定記録から参照し、CSV整理後も測定した実装を取得できるようにする。
先行生成・追加調査の各案は、それぞれの測定記録にある基点SHAと実装差分・ソースハッシュを参照する。
`.ToArray()` で測った案Bと、後の `[..]` 版を同一ソースとして扱わない。

WordParserの旧8CSVを上記4CSVへ統合した。全行と、列名・単位表記を戻したときの全セル値が元CSVと一致することを確認している。
旧CSVの正確なファイル内容は `6db4bcbf6a028230f7e150bb9ac4992c316ed22a` から取得できる。
元ログ・完全JSON・トレース・測定用コピーは、各測定記録に示すGit対象外の `artifacts/` にある。
