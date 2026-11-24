# JSONベンチマーク基準値 — 2026-09-14

record／primary constructor導入後、最適化前の基準値。測定したソースはマージコミット `c458046b4dfacfbb5546d055007e26e86c658dc8`。本体・JSONパーサー・測定コードはこのコミットから変更していない。

## 入力・条件

- 入力: `Kotowari.Benchmarks/Data/test.json`
- サイズ: 4,172,062 bytes / 4,172,062 UTF-16コード単位
- SHA-256: `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`
- BenchmarkDotNet 0.15.8、ShortRun、LaunchCount=1、WarmupCount=3、IterationCount=3
- OS: Windows 11 25H2、10.0.26200.9445
- CPU: Intel Core i7-13700H 2.90GHz、14物理 / 20論理コア
- SDK: 10.0.302
- Runtime: .NET 10.0.10 (10.0.1026.32716)、X64 RyuJIT x86-64-v3
- GC: Concurrent Workstation
- 測定中はBenchmarkDotNetが高パフォーマンス電源プランを使用し、終了時に元のプランへ復元
- ファイル読み込みはGlobalSetupで行い、測定対象は解析と結果生成。System.Text.JsonはJsonDocumentの解析に加えてRootElement.Cloneを含む。

依存バージョンは `Directory.Packages.props` と復元済み `project.assets.json` の両方で確認した。

| 対象 | バージョン |
|---|---|
| Kotowari / 自作JSONアダプター | 上記コミット |
| Pidgin | 3.5.1 |
| Sprache | 2.3.1 |
| SpracheJSON | 1.0.1 |
| Superpower | 3.2.1 |
| System.Text.Json | .NET 10.0.10ランタイム付属 |

## 結果

Allocatedは1回の解析あたりのマネージド割り当て。表のMiBはBenchmarkDotNet出力のMB（1,048,576 bytes）に対応する。

| 対象 | Mean (ms) | StdDev (ms) | 割り当て (MiB) |
|---|---:|---:|---:|
| Kotowari | 321.94 | 13.918 | 80.31 |
| Pidgin | 46.07 | 0.807 | 13.80 |
| Sprache | 694.48 | 41.079 | 2071.07 |
| SpracheJSON（旧実装） | 789.96 | 21.282 | 2543.35 |
| Superpower | 198.14 | 3.081 | 175.22 |
| System.Text.Json（専用パーサー参考値） | 14.47 | 0.960 | 6.75 |

全6対象を同じ実行のShortRunで測定。詳細な誤差、GC、ジョブ設定は [CSV](2026-09-14-json-baseline.csv) に保存する。測定回数が3回と少なく信頼区間が広いため、小さな速度差の判断には追加測定が必要。

## 測定前の検証と比較の制約

`--validate` で全6対象の解析成功と結果の生成を確認した。

- Kotowari、Pidgin、Sprache、Superpower、System.Text.Json: 100,001ノード
- SpracheJSON: 104,120ノード

既存の `--validate` は消費位置を検査しない。補足として測定外の一時検証プログラムで、リフレクションにより `Marimo.Parser.JSON.JObject` のParseResultと `SpracheJSON.JSONParser.JObject` のSprache結果を取得した。どちらも成功し、消費位置は4,172,062 / 4,172,062だった。PidginのDocumentはEnd、SpracheのDocumentはEnd()、SuperpowerはAtEnd()を要求している。System.Text.Jsonも文書全体を解析する。

これはこのサンプルの全入力消費確認であり、任意入力に対するJSON仕様準拠や、生成した木の全キー・値の一致までは検証していない。生成オブジェクトの表現も異なる。

2026-08-04のKotowari記録は104,120ノードだったが、今回は100,001ノード。旧記録との速度差には処理内容の違いも入り、構文更新・record化だけの効果とは断定できない。SpracheJSONはノード数が異なるため、同一仕事量の厳密な比較として扱わない。

SpracheJSON 1.0.1は.NET Framework向けの `lib/net47` を明示参照しており、既存のNU1701警告が発生する。測定設定は既存どおりOptimization Validatorを無効化している。

## 次に測る候補（コード調査による仮説）

1. `WordParser` の `word.Select(...)` は遅延列挙で、解析のたびに文字パーサーを生成する。リスト→配列→文字列という結果構築もある。単語解析に絞った測定で寄与を確認し、既存の比較・空白処理を保った最小変更から試す。
2. JSON文字列・数値の組み立てには `List<char>`、一時配列、最終文字列の割り当てがある。入力の連続区間を使う走査がSpan適用候補。ただしエスケープなど変換を伴う経路は区別する必要がある。
3. `Cursol` は入力時に一度 `ToCharArray()` する。このサンプルでは文字データだけで約7.96MiB。移動とコピーでは文書配列を共有しており、毎回全文コピーしているわけではない。現在は公開 `char[] Text` と書き換え可能な `ref char Current` を持つため、文字列など別の入力保持方式への変更にはAPI・文書同一性への影響確認が必要。
4. JSON再帰取得のラムダは `ParserConverter` を都度生成している。キャッシュ候補だが、`RecursiveParser` 全般の取得頻度を変えると利用者のデリゲートの意味が変わるため、まずJSON構築箇所に限定して評価する。
5. `Cursol.Current` の範囲外アクセスは例外で終端を表現している。境界判定への置換候補だが、今回の全体測定だけでは寄与は未確定。

Spanの導入や本体最適化はまだ実施していない。各候補の効果は個別変更前後の同条件測定で判断する。

## 再現コマンド

```powershell
dotnet run --project Kotowari.Benchmarks/Marimo.Kotowari.Benchmarks.csproj -c Release --no-restore -- --validate
dotnet run --project Kotowari.Benchmarks/Marimo.Kotowari.Benchmarks.csproj -c Release --no-restore -- --filter "*" --job short --artifacts artifacts/performance-baseline-2026-09-14
```
