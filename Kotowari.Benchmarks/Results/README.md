# ベンチマーク測定記録

測定記録には、対象実装、入力、環境・依存バージョン、測定条件、結果と判断を残す。
結果を再現する際は、各記録に示したソースの版と条件を使う。

UTF-8対応を終了して文字列専用実装へ戻す際の[履歴索引と保存タグ](../../docs/utf8-experiment-archive.md)を用意した。旧比較基準OはUTF-8対応済みの旧カーソルの文字列経路であり、実際の導入前実装とは異なる点も記録している。

## JSONパーサー比較

- [2026-11-05のJSON同期前後・最新SpracheJsonとの簡易速度比較](2026-11-05-json-sync-speed.md)（同期前後ではノード数差があり、最新SpracheJsonとの比較では同一ノード数・全消費・値構造一致を確認。後者はこの条件でSpracheJsonが約8.38倍遅く、結果は特定入力・環境での観測）

- [2026-10-20のSequence3中間結果削減と追試](2026-10-20-sequence3-projection.md)（初回は時間比1.0072、delegateキャッシュ取得を文法生成へ移した追試は0.9843、95%区間[0.9494, 1.0206]。いずれも改善の採用条件未達で本体は撤回。テスト補強・再現差分・全観測を保存）
- [2026-10-19のJSON値の試行順変更](2026-10-19-json-value-order.md)（リテラルを先に試し約4.28 MB/解析削減。一方、時間比1.0577、95%区間[1.0381, 1.0776]で悪化を支持。差分・テスト・記録を保存して撤回）
- [2026-10-19のJSONArray一括構築の評価](2026-10-19-json-array-bulk.md)（逐次Addの容量拡張を省き約0.58 MB/解析削減。時間比1.0132、95%区間[0.9887, 1.0382]で速度改善の基準未達。差分・テスト・記録を保存して撤回）
- [2026-10-19のJSON通常文字判定の重複削減](2026-10-19-normal-character-predicate.md)（引用符・バックスラッシュの除外を既存ConditionalCharParserへ集約。独立確認で約5.3%短縮、95%区間[0.9311, 0.9624]。ユーザーが採用・コミットを了承）
- [2026-10-19のJSON空白判定の重複削減](2026-10-19-whitespace-predicate.md)（既存ConditionalCharParserへ固定4文字の判定を集約。独立確認で約6.7%短縮、95%区間[0.9214, 0.9449]。本体・公開APIは変更なし）
- [2026-10-19のJSON文字列化の二重配列コピー削減](2026-10-19-char-materialization.md)（割り当て約9.4%減に対し、時間比1.0576、95%区間[1.0318, 1.0840]で悪化を支持。差分と全結果を保存して撤回）
- [2026-10-19のJSON再帰定義の生成削減](2026-10-19-recursive-definition.md)（割り当て約12.3%減。時間比0.9732、95%区間上限1.0066で速度改善の採用基準未達。差分と全結果を保存して撤回）
- [2026-10-19の次候補の根拠調査](2026-10-19-next-candidates-investigation.md)（既存の割り当て・Tier 1コードと入力構造を照合。再帰定義時の生成、二重配列化、Sequence中間結果の削減候補を特定。本体変更・新しい速度測定なし）
- [2026-10-19のWordParser入力範囲からの結果生成](2026-10-19-word-result-slice.md)（約1.13 MB/解析の割り当て減少に対し、8組比較では約2.6%悪化し撤回。次に試す根拠ある候補を特定できず、今回の探索を終了）
- [2026-10-19のWordParser定義語再利用](2026-10-19-word-result-reuse.md)（約2.20 MB/解析の割り当て減少に対し、8組比較では約3.2%悪化したため不採用。差分を保存して撤回）
- [2026-10-19の成功結果の重複null判定削減](2026-10-19-parsed-value-guard.md)（生成コードの判定は減ったが、8組比較で約2.4%悪化したため不採用。候補差分と測定記録を保存し、本体は撤回）
- [2026-10-19のOneOrMore収集一元化](2026-10-19-one-or-more-buffer.md)（CPU・割り当て診断を根拠に一要素配列とConcat iteratorを削除。独立確認で約4.8%短縮、約2.85 MB/解析の割り当て減少）
- [2026-10-19の文字列専用実装の継続チューニング](2026-10-19-string-tuning.md)（CPU・割り当て標本を根拠に区切り付き反復へ既存プール方式を適用。独立確認で約9.5%短縮、約2.65 MB/解析の割り当て減少。スリープ抑止中断の留保を記録）
- [2026-10-18の文字列専用実装へ復帰後の性能調査](2026-10-18-string-tuning.md)（カーソル範囲判定は独立確認で約8.1%、同条件再測定で約13.1%短縮。残る4案の再評価も完了し、反復結果のプール化は独立確認で約3.1%短縮して採用。Or専用反復・Prepend・容量16・Or文脈分離は採用条件を満たさず不採用）
- [2026-08-04](2026-08-04-json.md)
- [2026-09-14の基準値](2026-09-14-json-baseline.md)と[CSV](2026-09-14-json-baseline.csv)
- [2026-10-05の巨大JSON・パーサーコンビネーター比較](2026-10-05-json-combinators.md)（専用JSONパーサーは測定対象外）
- [2026-10-07の文字列・UTF-8入力経路比較](2026-10-07-utf8-stream-json.md)
- [2026-10-07のUTF-8カーソル二重復号削除](2026-10-07-utf8-cursor-single-decode.md)
- [2026-10-07のUTF-8カーソル・次の候補の調査](2026-10-07-utf8-cursor-next-investigation.md)（実装変更前のCPUサンプル・生成コード確認）
- [2026-10-07のASCII文字幅直接取得の評価](2026-10-07-utf8-cursor-ascii-width.md)（通常計時では改善を確認できず、実験変更を取り消し）
- [2026-10-07のUTF-8 Span借用・巨大JSON比較](2026-10-07-utf8-span-borrowing.md)（全文コピーは撤去できたが、両経路で大幅な速度低下）
- [2026-10-07のSpan借用後の速度低下・原因切り分け](2026-10-07-utf8-span-regression.md)（Tier 1生成コードと、型宣言・同サイズのデータ表現を分けた対照実験）
- [2026-10-07のSpan借用カーソル・値渡しの本体修正](2026-10-07-utf8-cursor-flat-fix.md)（中間コピー列と大幅な後退を縮小、対応前文字列版の速度目標と安定化は未達）
- [2026-10-09のExecution共通化・JSON性能評価](2026-10-09-execution-commonization.md)（転送・キャッシュ・空白処理を共通化、性能同等性は未確認）
- [2026-10-10のカーソル引数のscoped in化の評価](2026-10-10-cursor-in-parameter.md)（CPU／JIT調査に基づくコピー削減候補を、PGO有効・MemoryStream込みで評価）
- [2026-10-10のUTF-8文書の入力保持方法の比較](2026-10-10-utf8-document-layout.md)（独立した8組の確認測定で約5.2%短縮、対応前文字列版の1.10倍以内という目標は未達）
- [2026-10-10のOneOrMore連結処理の評価](2026-10-10-one-or-more-prepend.md)（中間配列の削減では速度改善を確認できず不採用、電源イベントの留保付き）
- [2026-10-11のUTF-8カーソル保持情報の縮小](2026-10-11-utf8-cursor-size.md)（値型は8 bytes縮小したが、40起動の通常計時で速度改善を確認できず不採用）
- [2026-10-11の反復解析の収集バッファ評価](2026-10-11-repetition-buffer.md)（CPU標本と要素数分布から収集処理を調査、プール案・char初期容量16案は速度改善を確認できず不採用）
- [2026-10-11のUTF-8読み取り範囲判定の評価](2026-10-11-utf8-read-bounds.md)（生成コードのチェックは減ったが、通常計時の予備比較で速度改善を確認できず不採用）
- [2026-10-11のOr呼び出し文脈による型profile分離](2026-10-11-or-profile-context.md)（実際の子に合う型ガードへ変化したが、40起動の通常計時で速度改善を確認できず不採用）
- [2026-10-11のCharParser・WordParserのUTF-8直接照合](2026-10-11-utf8-byte-match.md)（MemoryStream込みの固定40起動。UTF-8の時間比2.004628、95%区間[1.933993, 2.077843]。割り当ては約3.9%減るが速度悪化のため不採用。試作を履歴に保存）
- [2026-10-12のASCII文字消費・Word単独案の評価](2026-10-12-utf8-character-advance.md)（PGO有効・MemoryStream込みの固定56起動。複合案は約4.0%短縮を支持するが、個別案は事前採用条件を満たさず撤回。旧文字列版の1.10倍以内という目標も未達）

PrependからOrの文脈分離までの6候補は、いずれも速度改善の採用根拠が得られず、本体へは残していない。週間残量40%への到達で新しい試行を終了した。実測、仮説、未確認事項を分け、割り当て削減や生成コードの変化を速度改善と同一視しない。

この6候補の[通常計時の集約JSON](experiments/2026-10-11-utf8-tuning-evidence.json)には、予備と本測定を区別して200起動の平均・反復値・条件・元JSONのハッシュを保存した。ActualとResultは同じ操作の生値と補正値であり、別々の独立試行ではない。CPU/JIT診断付きの時間は含めず、環境上の留保や採否は各記録を参照する。CSVは追加していない。

## WordParserの測定

同じテーマのデータは、測定のたびにCSVを増やさず、`word-parser/` の種類別CSVへ追記する。
`Experiment` で測定記録を、`Run` でその中の実行を識別する。
WordParser変更後のJSON全体の性能確認も、この既存データ群に含める。

現行の文字蓄積比較は `List`、`ListCapacity`、`StringBuilder`、`StringBuilderCapacity`、`Array`、`DeferredArray`、`StackBuffer` の7方式を対象とする。
`IteratorConcat`、`IteratorArray`、`ResultLambda` は、UTF-8試作時の `ref struct` カーソル・解析結果をイテレーターへ保持できないため比較対象から除外した。文字列専用実装への復帰後も、合意済みの7方式を維持し、旧3方式は再追加していない。
旧10方式はコミット `8562efc0757e9c936693b3cb2ced3aef1febe511` と [2026-10-05の測定記録](2026-10-05-word-parser-accumulation.md) の条件から再現する。過去のCSVは当時の記録のまま変更しない。
現行の単体・文字蓄積ベンチマークには呼び出しごとのカーソル生成と入力状態の割り当てを含むため、保存済みカーソルを使った旧測定の絶対値とは直接比較しない。

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
| `2026-10-07-utf8-cursor-next-investigation` | [次の候補のCPUサンプル・生成コード調査](2026-10-07-utf8-cursor-next-investigation.md) | — | — | — | 185 |
| `2026-10-07-utf8-cursor-ascii-width` | [ASCII文字幅直接取得の評価](2026-10-07-utf8-cursor-ascii-width.md) | 24 | 384 | 24 | — |
| `2026-10-07-utf8-span-borrowing` | [UTF-8 Span借用の実装前後比較](2026-10-07-utf8-span-borrowing.md) | 32 | 512 | 32 | — |
| `2026-10-07-utf8-span-regression` | [Span借用後の速度低下・原因切り分け](2026-10-07-utf8-span-regression.md) | 24 | 384 | 24 | 206 |
| `2026-10-07-utf8-cursor-flat-fix` | [Span借用カーソル・値渡しの本体修正](2026-10-07-utf8-cursor-flat-fix.md) | 20 | 320 | 20 | — |
| 合計 | | 612 | 14,196 | 778 | 735 |

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
