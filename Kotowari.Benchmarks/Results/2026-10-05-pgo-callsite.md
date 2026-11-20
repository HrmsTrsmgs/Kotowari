# 内部呼び出し位置へ型profileを分けた比較（2026-10-05）

## 結論

PGO有効のまま、内部combinatorの子パーサー呼び出しを `Parse` 経由から `ParseCore` 直接呼び出しへ変えた。
既存の約4MB JSONを使い、各版のビルド済みDLLを固定して新規プロセスを交互に起動した。
**診断なしの4起動ずつでは、起動平均の平均が282.98msから158.25msへ44.1%短縮した。**
生成コード付き12起動ずつでも、変更後の起動平均は155.92〜162.79msに収まり、遅い側へ揃えた結果ではない。

狙った変更は生成コードでも確認できた。`ZeroOrMore<char>` 自身の呼び出し位置に型profile計測が入り、
変更前の使われない `CharParser` ガードから、実際にこの位置へ渡される `OrParser<char>` または `ConditionalCharParser` のガードへ変わった。
**どちらの型が選ばれた場合も速い帯に収まった。**

ただし生成コードは毎回同一ではなく、PGO依存がすべて消えたという結果ではない。
入力は1種類、マシンも1台で、まれな遅い起動を将来も起こさない保証はない。
また内部24呼び出し位置を一緒に変更したので、全改善量をZeroOrMore一箇所へ帰属させない。

## 実装の範囲

基点は `a4a37bb92ef852976b0d9a50df07cbc68942c1ea`。WordParserの遅延配列実装を含む変更前から、次だけを変更した。

- `Parser<T>.ParseCore` を `protected internal abstract` にし、同一assembly内のoverrideもアクセス修飾子をそろえた（基底1＋override 22）。
- 内部の子パーサーへの24呼び出しを `.Parse(...)` から `.ParseCore(...)` へ変更。
- 公開 `Parse`、引数・戻り値、アルゴリズム、入力消費、成功・失敗、値の蓄積方式は維持。
- PGO OFF、`NoInlining`、`AggressiveInlining`、JSON専用分岐、手動の処理複製は追加していない。
- 共通入口由来の型profile混在を避ける意図を基底のコメントへ記載。

例えばZeroOrMoreのループは、子呼び出しが `Parser.Parse(current)` から `Parser.ParseCore(current)` になるだけである。
外部assemblyの派生クラスは、[C#のアクセス規則](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/protected-internal#overriding-protected-internal-members)どおり `protected override` のままで使う。

本体変更は `Parser.cs`、`WithWhiteSpaceParser.cs` と `Core/` の `CharParser.cs`、`ConditionalCharParser.cs`、
`DebugParser.cs`、`DelimitedSequenceParser.cs`、`ExpectCharParser.cs`、`OneOrMoreParser.cs`、`OptionalParser.cs`、
`OrParser.cs`、`ParserConverter.cs`、`RecursiveParser.cs`、`SequenceParser.cs`、`WordParser.cs`、`ZeroOrMoreParser.cs` の15ファイル。
テストは `Kotowari.Tests/Core/ZeroOrMoreParserのテスト.cs` に外部派生型の特性テスト2件を追加した。
本体とテストの差分は85行追加・47行削除。既存BOM/CRLFを維持した。

## 性能とばらつき

表の平均・範囲・CVは、各プロセスのactual結果16反復を平均した「起動平均」に対する統計。
CVは起動平均の標本標準偏差÷平均。反復を独立した起動として水増ししていない。

| 系列 | 各版の起動数 | 変更前の平均 ms | 変更後の平均 ms | 前の起動平均範囲 ms | 後の起動平均範囲 ms | 前→後の起動間CV |
|---|---:|---:|---:|---|---|---|
| disassemblyのみ | 12 | 279.84 | 158.54 | 235.75〜286.62 | 155.92〜162.79 | 5.03% → 1.32% |
| 診断なし | 4 | 282.98 | 158.25 | 281.40〜286.24 | 156.14〜159.95 | 0.79% → 1.02% |
| disassembly＋EventPipe | 1 | 298.08 | 164.08 | — | — | 起動間評価なし |

診断なしは約1.79倍の速度、時間は44.1%短縮。disassembly系列では43.3%短縮した。
診断なしの変更前4起動はもともと一つの遅い帯に収まっているため、その系列でCVが改善したとは言わない。
生成コード系列の変更前には235.75msの起動もあり、同一DLLでも起動による差が残った。
以前の約173msの変更前ケースは、今回の条件では捕捉していない。

予定数は実行前に固定し、速いケースが出ても打ち切っていない。外れ値削除なし。
01〜24は `baseline → callsite → callsite → baseline` を6回、25〜32は診断なしで同じ順序を2回、33〜34は各版のEventPipe採取。
診断条件が異なる3系列を一つの母集団として合算しない。

## プロファイルと生成コード

### 型profileの位置

Instrumented Tier0のZeroOrMoreには、変更前は共通 `Parser<char>.Parse` 呼び出しが1箇所あり、型計測helperはなかった。
変更後はその呼び出しがなくなり、virtualな `ParseCore` 呼び出しの前に `CORINFO_HELP_CLASSPROFILE32` が1箇所入った。
採取した各版13起動（disassembly 12＋trace 1）で同じ違いを確認した。

Tier 1のZeroOrMoreが直接の子パーサーに対して行う型ガードは次のとおり。
型アドレスは同じプロセスのオブジェクト生成コード・constructor呼び出しと照合した。

| 対象 | 選択された型 | 起動数 | その起動平均の範囲 ms |
|---|---|---:|---|
| 変更前・disassembly | CharParser | 12 | 235.75〜286.62 |
| 変更後・disassembly | OrParser<char> | 6 | 156.76〜162.79 |
| 変更後・disassembly | ConditionalCharParser | 6 | 155.92〜160.64 |

今回のJSONではZeroOrMoreの相手は文字列のOrParserと数字列のConditionalCharParserであり、CharParserではない。
変更後は型選択自体が毎回同じになるのではなく、どちらの選択にも利用される経路がある。
ただしOrParser内部など、さらに内側の型ガードがすべてその用途に最適になったとは主張しない。

### inline・サイズ・残る違い

ZeroOrMoreのTier 1サイズは変更前546〜555 bytes、変更後383〜1215 bytes。
変更後はConditionalCharParser展開、OrParser展開、その内側のCharParserや文字変換処理の展開量などに差が残る。
小さい生成コードだけが速いわけでも、展開量が毎回同じになったわけでもない。

EventPipe付き34では、ZeroOrMoreへのOrParser.ParseCoreのinline成功を確認した。
変更前33では共通Parser.ParseとCharParser.ParseCoreのinline成功を確認した。
変更後の文字処理について、共通 `Parser<char>.Parse` 自体のJIT出力・メソッドイベントは見つからなかった。

ZeroOrMore等のヘッダーには `optimized using Synthesized PGO` と記録されている。
この表記をPGO OFFと読み替えず、実行環境の `DOTNET_TieredPGO=1`、型計測helper、ガード対象、inlineイベントを個別に記録した。
生の型ヒストグラムは今回も未採取。

`Cursol.Current` は両版・採取26起動すべてで78 bytes、アドレス等を除く命令列hashが一致し、bounds checkが残る。
ZeroOrMoreのList容量チェックも残る。割り当て方式変更による改善ではない。
対象assemblyのdisassembly・2トレースにOSR tierはなく、今回の差をOSRへ帰属させる証拠はない。

### Tier移行

各プロセスのトレース開始からの経過時間。

| メソッド | 変更前33：Instrumented Tier0 → Tier 1 load | 変更後34：Instrumented Tier0 → Tier 1 load |
|---|---|---|
| OrParser<char>.ParseCore | 3.078秒 → 4.316秒 | 3.154秒 → 4.583秒 |
| ZeroOrMore<char>.ParseCore | 3.116秒 → 4.493秒 | 3.245秒 → 4.716秒 |

両方ともこれらのTier 1はwarmup 1中に生成され、その後の再生成なし。
actual開始は変更前15.741秒、変更後11.707秒。変更後はwarmup 2〜4が約204〜207ms、warmup 7以降が約163〜166msだった。
他のメソッドは引き続きTier移行しており、すべてがwarmup 1で完成したわけではない。
外側のJSON.Parseは変更前22.381秒、変更後15.186秒にTier 1へ移行し、どちらもactual中だった。
正確なprofile読み取り命令の時刻は未採取。計測終了までのメソッドイベントと反復窓は保存した。

## 測定条件・再現性

- Windows 11 10.0.26200.9457、i7-13700H、SDK 10.0.401、.NET 10.0.12（10.0.1226.42308）、X64 RyuJIT、Concurrent Workstation GC。
- 既存 `test.json`：4,172,062 bytes、SHA-256 `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。
- 両版を別の検証プロセスで全文消費4,172,062文字、100,001ノード、木のSHA-256 `5ABDB9015EF96B7E79FADB54B636AF0C5AB9D0AA1864C5AF21FF8669C2A4233C` と確認。
- BDN 0.15.8の既存自動生成測定コードを無変更で再利用。コードSHA-256 `4B0A99ADBD36344E11DEE7BED2BF425CE5E3A5CCDD67A3121F79CCCECC6DEB04`。
- BDNの[NoAcknowledgementConsoleHost](https://github.com/dotnet/BenchmarkDotNet/blob/v0.15.8/src/BenchmarkDotNet/Engines/NoAcknowledgementConsoleHost.cs)経路を使い、ビルド済み実行ファイルを直接新規起動。独自の解析ループへ置き換えてはいない。
- Release、各起動Warmup 10・Actual 16反復、InvocationCount 4、UnrollFactor 1、DontRemove、affinity 4（論理CPU 2）、High priority。
- 現在のバランス電源プランを両版で維持し、変更しなかった。以前のBDNホストによる測定とは起動方法・電源条件が異なるため、過去の絶対値と直接合算しない。
- `DOTNET_TieredPGO=1`。Tier・OSR・インライン化の閾値変更なし。PGO OFFの追加測定なし。
- disassemblyは `DOTNET_JitDisasm=Marimo.Kotowari!* Marimo.Parser!*` と起動ごとの `DOTNET_JitStdOutFile`。
- traceはさらに `DOTNET_EnableEventPipe=1`、起動ごとの出力、buffer `40`（16進、64MiB）、config `Microsoft-Windows-DotNETRuntime:1000001019:5,BenchmarkDotNet.EngineEventSource:ffffffffffffffff:5`。
- 各版35ソースを固定。差は本体15ファイルだけ。各版37ファイル（DLL・実行設定・入力）のSHA-256を起動前ごとと全測定後に照合し、不変を確認。
- ソースの基点SHA・変更後ファイルhash・本体patch・バイナリhashを証拠JSONに保存。未コミットの測定実装を版不明として残さない。

再起動は、各固定harnessの出力ディレクトリで `dotnet PgoCallsiteHarness.dll --benchmarkName Marimo.Kotowari.Benchmarks.JsonParserBenchmarks.Kotowari --benchmarkId 0`。
環境変数・affinity・priority・記録・終了確認はignored補助 `Run-FixedCase.ps1`、起動順は `plan.json` に保存した。
新規パッケージの導入なし。復元は既存キャッシュを使用した。

## 外部負荷・割り当て

実装・全検証・両版ビルドを終えてから測定し、測定中に編集・追加ビルド・解析は行わなかった。
ランナーと2秒間隔のCPU記録は論理CPU 16へ固定。対象プロセスは全34回でaffinity 4・Highを確認した。
起動からaffinity設定完了までは13〜83msで、プロセス生成前からの固定ではない。

対象プロセスが1コアの80%以上を使用したサンプルのCPU性能カウンター中央値は、非trace系列で各起動約109.8〜110.3。
両版で大差はないが、これは各反復の厳密な実効クロックや温度測定ではない。
ChatGPT・Chrome・WebView等のバックグラウンド活動はあった。外れ値は取り除いていない。

全34起動のMemoryDiagnoser相当の追加統計はGen0/1/2が18/7/2回・4操作、すなわち4500/1750/500回/1000解析。
割り当ては両版とも約60.33MB/解析で、メモリ確保量の大幅削減は観測していない。

## 検証・保存

- 既存全242テスト成功。外部派生型の追加特性2件は本体変更前も成功し、Refactor後は全244件成功・Skip 0。
- 旧DLLでコンパイルしたconsumerを再コンパイルせず、新DLLだけ差し替えて成功。consumer DLLのSHA-256は差し替え前後で一致。
- Releaseビルド成功。指定Analyzerビルド成功、実測16.088秒。NU1701は変更前にも再現。
- 全5プロジェクトのSARIFは診断コード・レベル・場所・メッセージが変更前後一致（本体3、テスト1、JSON 3、残り0）。
- `dotnet format ... --verify-no-changes --severity info` は既存指摘によりexit 2。変更前後の出力74行は完全一致。検査成功とは扱わず、無関係な箇所は変更していない。
- 固定harness・両検証プログラムのビルド成功。測定34プロセスはすべてexit 0、Warmup 10・Actual 16・Result 16を検査。EventPipe 2本ともEventsLost 0。
- 解釈用のガード照合はレジスタ経由比較の形にも対応して再検査済み。補助記録スクリプトの構文エラーは修正後に保存・整合性検査を行った。測定のやり直しや結果の選別はしていない。
- [pgo-callsite-evidence.json](word-parser/pgo-callsite-evidence.json)に全34起動、26起動のガード照合、代表6起動のdisassembly、2本のTier/inline記録、ソース・バイナリの版、CPU記録の要約を集約。
- 既存 `summary.csv` に34行、`iterations.csv` に1,088行、`launches.csv` に34行を `Experiment=2026-10-05-pgo-callsite` として追記。実測は544 actual反復で、Actual/Resultを二重に独立試行として数えない。warmupは証拠JSONに保存。CIを計算していないためsummaryのError欄は空欄。
- raw・固定コピー・実行補助は `artifacts/pgo-callsite-2026-10-05/`。互換性・全テスト・静的検査のログは `artifacts/pgo-callsite-compat-2026-10-05/`。CPU記録は `artifacts/benchmark-variance-2026-10-05/pgo-callsite-activity.jsonl`。

現時点では、この入力で速度と大きな起動間変動を改善できた実装として残す。別入力・別環境やまれな遅い起動までの保証はしない。
測定時点では本体・テストは未コミットだった。その後、ユーザー確認を経て
`09dee2bed3c080a81202c27d97425f038ea3d5db` で実装・特性テスト・説明コメントをコミットした。
測定時の実装との差は `Parser.cs` のXML説明コメントだけで、実行コードは一致する。
証拠JSONの `CandidateCommitted=false` とソースhashは測定時点の記録として保持している。
PGO依存の追加調査や別の最適化へ自動で広げていない。

## 測定方法の追加検証

ユーザーの追加依頼により、同じ実装・入力で測定条件だけを比較した。
`Experiment=2026-10-05-measurement-method`。本体・テストの追加変更なし。Refactor担当への委任なし。
通常測定48起動とEventPipe確認4起動を事前に固定し、すべて完了した。外れ値・速い起動の削除、都合のよい時点での打ち切り、追加追試はない。

### 今後の使い分け

この4MB JSONと現在の速度帯では、次を測定の出発点にする。

| 用途 | Warmup | Actual | 独立起動 | 変更後の1起動平均所要時間 |
|---|---:|---:|---:|---:|
| 候補の探索・比較 | 10 | 8 | 各版8 | 16.51秒 |
| 最終評価 | 16 | 8 | 各版8 | 20.24秒 |
| 今回の比較基準 | 10 | 16 | 各版8 | 21.45秒 |

探索条件は比較基準よりプロセス所要時間を23.0%短縮。最終評価条件も5.6%短縮した。
最終評価の16回は「速いPGO結果を出すため」ではなく、今回実際に観測した遅いTier移行を測定前へ移すための値である。
回数を増やしても、変更前実装の起動間変動は消えなかった。

所要時間はプロセスの起動から終了までであり、版ごとの初回ビルド・hash確認・後処理は含まない。
今回の変更前後を8起動ずつ比較した合計では、探索条件約5.5分、最終評価条件約6.8分、比較基準約7.4分。
変更前の速度帯の出現割合も条件間で異なるため、この合計時間の差を純粋な反復数短縮効果とはみなさない。

これは普遍的なベンチマーク既定値ではない。入力・実装・ランタイムが変わったら、反復時間とTier移行を再確認する。
特にns単位の単体ベンチマークにInvocationCount=4を転用しない。
[BDNの通常の方針](https://benchmarkdotnet.org/articles/configs/jobs.html)では反復数等の自動決定が基本だが、
ここでは既知の起動間変動を独立に扱うため固定条件を検証した。

### 通常測定の結果

各行は別の新規プロセス8起動。値は起動平均の平均、CVは起動平均の標本標準偏差÷平均。

| 実装 | Warmup / Actual | 平均 ms | 起動平均の範囲 ms | 起動間CV | 起動内CV平均 |
|---|---|---:|---|---:|---:|
| 変更前 | 10 / 16 | 277.20 | 237.79〜285.81 | 5.79% | 1.04% |
| 変更前 | 10 / 8 | 272.71 | 188.33〜285.84 | 12.51% | 1.36% |
| 変更前 | 16 / 8 | 271.41 | 234.38〜286.38 | 8.16% | 0.85% |
| 変更後 | 10 / 16 | 156.21 | 153.85〜158.34 | 0.93% | 1.78% |
| 変更後 | 10 / 8 | 157.08 | 154.03〜159.47 | 1.21% | 1.99% |
| 変更後 | 16 / 8 | 156.71 | 154.44〜159.45 | 1.18% | 1.48% |

開始前の運用基準は、起動間CV 3%以下・起動内CV平均5%以下、
かつ短縮条件／比較基準の比の95%区間全体が0.97〜1.03内に入ることとした。
後者は各ブロックのlog時間比8個に対するt区間（df=7）を指数へ戻したもの。
全比較に同時に95%の保証があるという意味ではなく、分布仮定を伴う個別の区間である。

- 変更後10/8の比は1.0056、95%区間0.9918〜1.0196。16/8は1.0032、0.9906〜1.0159。いずれも事前基準を満たした。
- 変更前は全条件で起動間CVが基準を超え、時間比の区間も広い。測定方法だけで実装のPGO依存を解消できたとは言わない。
- 全48通常起動で、Actual前半と後半の平均差は事前の警戒値3%以内だった。
- 比較基準の同じプロセスの先頭8反復だけを使う補助確認では、16反復平均との差は−0.10〜+0.85%。これは独立した追加実験ではない。

変更後の同じDLLについて、事前に決めた奇数ブロック4起動と偶数ブロック4起動をA/A比較した。
10/8では変更がないのに1.12%の見かけの差があり、保守的な95%区間は−1.44〜+3.75%だった。
他の2条件も区間に差ゼロを含む。各群4起動の独立log平均差と、df=3の保守的なt臨界値を使用した。
小標本で偽陽性率を検証したわけではない。今後も1〜2%程度の差を一度の小標本だけで改善と断定しない。

### Tier移行の確認

EventPipeは通常測定と分離し、10/8と16/8で各版1起動、計4起動。4本ともEventsLost=0。
対象assemblyの全記録済みメソッドを確認した。OSR tierは観測しなかった。

| Run | 条件 | Actual開始 | JSON.ParseのTier 1 load | Actual中の対象assemblyのTier 1 load数 |
|---|---|---:|---:|---:|
| 49・変更前 | 10 / 8 | 16.440秒 | 23.074秒（Actual 6） | 4 |
| 50・変更後 | 10 / 8 | 11.010秒 | 14.522秒（Actual 6） | 5 |
| 51・変更前 | 16 / 8 | 22.762秒 | 22.187秒（Warmup 16） | 0 |
| 52・変更後 | 16 / 8 | 15.591秒 | 15.085秒（Warmup 16） | 0 |

10/8でも平均値は比較基準に近いが、外側のJSON.Parse、Cursol constructor等のTier移行をActual中に含む。
従って完全な定常状態の測定と呼ばず、探索用とする。
16/8では採取した2起動の移行がActual前に終わったため、最終評価の出発点にする。
すべての将来起動で移行が終わる保証や、framework全体でJITがなくなったという主張ではない。
診断ありの時間はPGO profileにも影響し得るため、通常測定の精度判定へ混ぜない。

### 運用手順

1. 変更前後のソース・入力・生成結果の同等性を確認し、各版を一度ずつReleaseビルドする。依存、runtimeconfig、入力を含む実行ファイル群を固定し、hashを記録する。
2. 実測前に編集・ビルド・解析を終え、他の重い作業を止める。電源・CPU割り当て・優先度を両版でそろえる。[BDNの注意事項](https://benchmarkdotnet.org/articles/guides/good-practices.html)に従い、デバッガを付けず、AC給電も確認する。
3. PGO有効、InvocationCount=4、UnrollFactor=1、DontRemoveで、固定DLLから毎回新規プロセスを起動する。各版8起動をAB/BA交互に配置する。途中の結果で予定数を変えない。
4. 通常の速度比較にはトレース・disassemblyを付けない。診断は代表起動として別枠で実行し、入力やruntimeの変更時にはTier移行を再確認する。
5. 反復ではなく独立起動を統計単位にし、起動別平均、起動間CV、起動内CV、前後半の変化、差の区間を確認する。A/A対照も利用する。
6. 速度帯が分かれる、区間が広い、外部負荷が強い場合は「判定不能」とする。遅い起動や速い起動を選別せず保存し、追加測定は別の事前計画として扱う。
7. 版と起動条件を記録し、種類別CSVへ追記する。実装の採否は性能差と保守性の両方で判断する。

### 固定条件・再現資料・検証

- .NET 10.0.12、SDK 10.0.401、BDN 0.15.8、入力、GC、CPU、Windowsは本報告前半と同じ。
- 前半の測定に使った本体・JSON・host・依存DLLと入力をbyte単位で再利用。新しいharnessでは生成済みBDNコードのWarmupCountとIterationCountの初期化2箇所だけを環境変数読取りへ置換し、測定ループは維持した。
- `KOTOWARI_WARMUP` と `KOTOWARI_ACTUAL` はこの測定補助が定義する値であり、.NETやBDN標準の設定ではない。親プロセス全体ではなく子プロセスへ設定。
- 各版35ファイルのhashを毎起動前と全測定後に照合。起動間のビルドなし。PGO=1以外のJIT/Tier設定変更なし。CPU affinity=4、High priority、バランス電源プラン固定。ランナー・2秒間隔の負荷記録は論理CPU 16。
- 8ブロックの各々に6条件を1回ずつ置き、条件順を循環、各条件の変更前後順をAB/BAで反転した。48通常起動の後に4診断起動。全52プロセスexit 0、予定したWarmup・Actual・Result数を検査。
- CPU性能カウンターの各起動中央値は約109.7〜110.2。ChatGPT（最大約116%・1コア換算）やZoom（約44%）の活動も記録したが、結果は除外していない。カウンターは厳密な実効クロック・温度ではない。AC状態の照会はアクセス拒否で、今回AC給電を確認できたとは言わない。
- 補助2プロジェクトのReleaseビルド成功、警告0。初回復元はNuGet設定の読み取り権限で失敗し、取得先を空にして昇格復元後に成功。新規パッケージ追加・取得なし。SDKがコピーした参照assemblyは、凍結前に元のruntime DLLと同じbytesへそろえた。
- 本体・テストを変更せず、前半で検証したソースhashも一致するため、全244テストやC# Analyzerの再実行はしていない。実測終了後に負荷記録プロセスを明示停止した終了コード1は、ベンチマーク失敗ではない。
- [measurement-method-evidence.json](word-parser/measurement-method-evidence.json)へ全52起動、事前計画・基準、起動単位の統計、4本のTierイベント、負荷要約、バイナリhash、生成harnessと補助スクリプトのソースを保存。実装の版は前半の証拠JSONをhash付きで参照する。
- 既存CSVにsummary 52行、iterations 1,088行、launches 52行を追記。544 Actual反復のActual/Result各544行であり、独立試行1,088件ではない。CSVのError欄は従来の99.9%区間を意味するため、今回の95%区間は証拠JSONに分けて保存し、Error欄は空欄。
- raw、凍結DLL、実行補助は `artifacts/measurement-method-2026-10-05/`、負荷記録は `artifacts/benchmark-variance-2026-10-05/measurement-method-activity.jsonl`。既存出力を上書きする再実行は拒否する。

実行補助の `Run-Case.ps1` は、Variant、Profile、Warmup、Actual、Diagnostics、未使用のRun名を持つCaseを受け、
凍結harnessを `dotnet MeasurementMethodHarness.dll --benchmarkName Marimo.Kotowari.Benchmarks.JsonParserBenchmarks.Kotowari --benchmarkId 0` で起動する。
同じ入力・版の再現にはこの保存済み補助を使い、新版の比較では先に別の凍結コピーとmanifestを作る。
通常の `dotnet run` にWarmup/Actual数だけを渡しても、電源プランや起動経路まで今回と同じになるとは保証しない。
