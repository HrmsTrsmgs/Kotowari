# WordParserの先行生成・配列保持の比較 — 2026-10-04

## 結論

- 同じWordParserを再利用する単体測定では、案Bの配列保持が有効だった。成功解析は変更前146.02ns／再測定168.24nsに対し71.36ns、割り当ては384Bから176B。
- 既存JSONサンプル全体では高速化を確認できなかった。変更前212.4ms／再測定182.6msに対し、案Bは220.3ms。実行間の変動もあり、単体測定の改善を文書全体へ外挿しない。
- JSON解析1回の割り当ては80.31MiBから63.08MiBへ約21.5%減った。案A・B・Cの全体測定では同じ水準で、単体測定の追加削減量がそのまま全体へ現れたわけではない。
- 生成して一度だけ解析する経路は遅くなった。案Bを未コミットの比較候補として残したが、JSON全体の速度改善としての採用判断は保留する。

## ソースと試した変更

基点は `9d9ecd83c9942d521be15beacbc8e9bc7582a6e6`。測定時点では各案とも未コミットで、共通の `WordParserBenchmarks.cs` と回帰テストを追加した作業ツリーを使用した。本体の変更は `Kotowari/Core/WordParser.cs` の以下の2箇所だけ。公開API、CharParser、空白処理、結果のリスト／文字列構築、JSON構文は変更していない。

| 案 | 非公開Parsersプロパティの型 | コンストラクターでの代入 |
|---|---|---|
| 変更前 | `IEnumerable<Parser<char>>` | `word.Select(c => new CharParser(c, ignoreCase))` |
| A | `IEnumerable<Parser<char>>` | 上記に `.ToArray()` を追加 |
| B（残した候補） | `Parser<char>[]` | Aと同じ |
| C | `CharParser[]` | Aと同じ |

CharParserは解析時に自身の状態を変更しないため、同じWordParser内で再利用する。大文字小文字の比較は引き続き解析時のCharParserに任せ、入力の大文字小文字を保った結果を返す。文書を書き換えた場合にも入力を読み直す。

先行生成では、最初の文字で失敗する場合も指定単語の全文字分を生成する。生成コストと保持する文字パーサーの数は単語長に比例する。再利用に有利でも、一回利用や長い単語の早期失敗に無条件で有利とはしない。

## 入力・環境・バージョン

- JSON入力: `Kotowari.Benchmarks/Data/test.json`（既存ファイルを変更していない）
- サイズ: 4,172,062 bytes / 4,172,062 UTF-16コード単位
- SHA-256: `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`
- OS: Windows 11 25H2、10.0.26200.9457
- CPU: Intel Core i7-13700H 2.90GHz、14物理 / 20論理コア
- SDK: 10.0.401
- Runtime: .NET 10.0.12 (10.0.1226.42308)、X64 RyuJIT x86-64-v3
- GC: Concurrent Workstation
- BenchmarkDotNet: 0.15.8
- 比較対象の復元済みバージョン: Pidgin 3.5.1、Sprache 2.3.1、SpracheJSON 1.0.1、Superpower 3.2.1。System.Text.Jsonはランタイム付属。
- 依存バージョンは `Directory.Packages.props` と `project.assets.json` で確認。依存の追加・更新はしていない。
- BenchmarkDotNetが高パフォーマンス電源プランを使用し、終了時に元のプランへ復元。

2026-09-14の記録はSDK 10.0.302／Runtime 10.0.10で、今回とは条件が異なる。過去の321.94msとの差を今回のコード変更の効果とは扱わない。

## 測定方法と記録

Release構成、Shortジョブを基にLaunchCount=1、IterationCount=10、OutlierMode=DontRemoveで測定した。外れ値も除去していない。表のErrorは99.9%信頼区間の半幅、Allocatedは1回のマネージド割り当て。BenchmarkDotNetのMBをここではMiB（1,048,576 bytes）と表記する。

最初はWarmupCount=3・CPU固定なしで実行したが、実行時間の大きな変動があった。論理CPU 2へ固定（`--affinity 4`）してもウォームアップ中に時間が変化したため、主比較はWarmupCount=10に統一した。CPU固定やウォームアップ増加だけでは実行間変動を除去できていない。

全8実行・56ケースの統計と条件を [CSV](2026-10-04-word-parser.csv)、JSON全体の各反復値80件を [反復CSV](2026-10-04-word-parser-json-iterations.csv) に保存した。未固定・ウォームアップ3回の探索結果も残すが、主比較へ混ぜない。完全なログ・HTML・元CSVはGit対象外の `artifacts/performance-word-parser-2026-10-04/` にある。

実行順は `baseline` → `cached-enumerable` → `baseline-fixed` → `baseline-warmed` → `cached-enumerable-warmed` → `cached-array-warmed` → `baseline-warmed-repeat` → `cached-char-array-warmed`。`baseline*` は変更前、`cached-enumerable*` はA、`cached-array*` はB、`cached-char-array*` はCに対応する。

単体測定は `WordParser("false", true)` を使い、成功 `false,`、先頭不一致 `0,`、末尾不一致 `falsX,` を測る。末尾のコンマにより、単語直後の空白判定が入力終端例外のコストに支配されないようにした。Cursolの生成はGlobalSetupで行い、ParseReusedではパーサーの生成も測定外、ConstructAndParseでは生成から解析までを測る。戻り値を返し、Setupで成功・失敗・値・カーソル位置を検査する。

JSONは既存の `JsonParserBenchmarks.Kotowari` を変更せず使用。ファイル読み込みはGlobalSetupで測定外、文字配列の生成・全文解析・結果の生成は測定内。今回の時間測定はKotowariの変更前後に限定し、他ライブラリの順位は更新していない。

## 単体測定（CPU固定・ウォームアップ10回）

各セルはMean (ns) / Allocated (B)。変更前の右側は最後の再測定値。

### 同じパーサーを再利用

| 入力 | 変更前（初回／再測定） | A | B | C |
|---|---:|---:|---:|---:|
| `0,` | 37.84／45.43 / 144 | 30.99 / 64 | 18.90 / 32 | 19.62 / 32 |
| `false,` | 146.02／168.24 / 384 | 117.24 / 208 | 71.36 / 176 | 73.60 / 176 |
| `falsX,` | 107.41／106.61 / 272 | 76.82 / 96 | 43.17 / 64 | 45.82 / 64 |

### 生成して一度だけ解析

| 入力 | 変更前（初回／再測定） | A | B | C |
|---|---:|---:|---:|---:|
| `0,` | 62.36／59.67 / 296 | 247.18 / 456 | 194.30 / 424 | 220.93 / 424 |
| `false,` | 165.85／194.05 / 536 | 260.35 / 600 | 252.10 / 568 | 251.50 / 568 |
| `falsX,` | 136.72／133.64 / 424 | 227.59 / 488 | 224.63 / 456 | 234.72 / 456 |

BとCの割り当ては同じで、Cに明確な追加効果は観測できなかった。Bを比較候補として残す。B・Cの末尾不一致／再利用にはMinIterationTime警告（最短92.6ms／96.9ms、推奨100ms以上）があり、小さなB対Cの差を確定的に評価しない。

## JSON全体（CPU固定・ウォームアップ10回）

| 実装 | Mean (ms) | Error (ms) | StdDev (ms) | 割り当て (MiB) |
|---|---:|---:|---:|---:|
| 変更前・初回 | 212.4 | 12.95 | 8.57 | 80.31 |
| A | 233.5 | 42.71 | 28.25 | 63.08 |
| B | 220.3 | 6.68 | 4.42 | 63.08 |
| 変更前・再測定 | 182.6 | 11.62 | 7.69 | 80.31 |
| C | 228.9 | 7.18 | 4.75 | 63.08 |

変更前を測り直すだけでも約14%変動した。各実装の単一プロセス内の反復だけから、実行間の環境変動も含めた信頼性を主張しない。今回の全体測定では各最適化案の平均が変更前より遅く、JSON全体を高速化したとは報告できない。遅くなった原因の特定も未実施。

## 動作・互換性・静的検査

- 追加した3回帰テストは、失敗後の再利用と大文字小文字を保った値、文書配列を書き換えた後の再解析、空の単語と指定空白処理を確認。最適化前にも成功し、振る舞いを変更するためのRedではない。
- 全テストは変更前227件、追加後およびA・B・C・最終Bで230件成功（本体176件、JSON54件）。既存テストの期待値は変更していない。
- `netstandard2.0` / `net10.0` の本体・JSONビルドと、Release測定プロジェクトのビルドが成功。
- 増分省略なし・スタイル解析付きソリューションビルドが成功。所要15.39秒、既存のNU1701警告1件。5プロジェクトのSARIFに既存の7診断があり、新規診断はなかった。
- `dotnet format --verify-no-changes --severity info` は既存の整形・診断により失敗。変更した3ファイルに限定しても、変更していないWordParserの `if(!result.IsSuccess)` の空白と既存コンストラクターのIDE0290が残る。無関係な整形やprimary constructor化は混ぜていない。
- 最適化前後の `--validate` で全6対象の成功と結果生成を確認。Kotowari・Pidgin・Sprache・Superpower・System.Text.Jsonは100,001ノード、旧SpracheJSONは104,120ノード。
- 測定外で非公開JObjectパーサーの結果を取得し、最終Bの成功、JSONObject生成、4,172,062 / 4,172,062文字の消費を確認。変更前にも全消費を確認済み。
- SpracheJSON 1.0.1は既存の.NET Framework用DLL明示参照とNU1701警告を維持。既存設定どおりOptimization Validatorを無効化。専用パーサーのSystem.Text.Jsonは参考対象のまま。

全入力消費とノード数の確認は、このサンプルでの検証であり、任意入力のJSON規格適合や全キー・値の比較を保証するものではない。今回の速度測定は.NET 10のみで、netstandard2.0向けの別ランタイムでの性能は未測定。

## 再現コマンド

実装表の差分を適用してビルドし、実装ごとに異なるartifactsパスを指定する。同時に別の測定・ビルド・テストは実行しない。

```powershell
dotnet test Kotowari.sln -c Release --no-restore -p:UseSharedCompilation=false
dotnet build Kotowari.Benchmarks -c Release --no-restore -p:UseSharedCompilation=false
dotnet run --project Kotowari.Benchmarks -c Release --no-build -- --validate
dotnet run --project Kotowari.Benchmarks -c Release --no-build -- --filter "*WordParserBenchmarks*" "*JsonParserBenchmarks.Kotowari*" --job short --launchCount 1 --warmupCount 10 --iterationCount 10 --outliers DontRemove --affinity 4 --artifacts artifacts/performance-word-parser-2026-10-04/reproduction
```

Spanの導入、結果構築やJSONパーサーの最適化は今回行っていない。
