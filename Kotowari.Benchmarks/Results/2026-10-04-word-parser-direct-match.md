# WordParserの直接照合の比較 — 2026-10-04

## 結論

- 今回の直接照合案の採用は勧めない。JSON全体の高速化を確認できず、単体にも遅くなった経路がある。CharParserの比較ロジックの重複と終端・負Indexの分岐が増えた保守コストを、今回の結果だけで正当化しない。
- JSON全体の6起動ずつの平均は、直前の配列保持版205.91ms、直接照合版242.04ms。今回の集計では直接照合版が遅いが、起動間のばらつきが大きい。約17.5%の差を実装変更による確定した遅延率とは扱わない。
- JSON解析1回のマネージド割り当ては約63.09MiBから56.45MiBへ約10.5%減少した。全12起動のGC診断で同じ削減方向を確認した。保持量・ピーク使用量ではない。
- 単体の再利用・末尾不一致は46.30nsから203.77ns。起動平均の範囲も43.66〜48.28nsに対し180.27〜235.04nsだった。ただし単体には短い反復時間と多峰性の警告があり、正確な倍率を保証しない。
- 再利用・短い入力で先頭不一致の割り当ては32Bから40Bへ増えた。単語より短い入力では比較前に捕捉配列を確保する現行実装のため、割り当て削減も全経路に一律ではない。
- 直接照合の本体・互換性テストは未コミットの試行のまま残す。測定を理由にさらにインライン化を展開したり、実装を自動的に戻したりはしていない。

## ソースと測定用コピー

基点は `721906f70471c90f2c41b4c5da8e1ac6712b2cad`、ブランチは `codex/optimize-word-parser`。配列保持のコミット `aecdb93` とC#指示のコミット `721906f` が含まれる。

- baseline: 基点コミットのWordParser。非公開 `Parser<char>[]` を保持し、`[.. word.Select(c => new CharParser(c, ignoreCase))]` で先行生成する。
- direct: 未コミットのWordParser。文字パーサー配列を削除し、`Cursol.Current` と `char.ToLower` を使って直接比較する。通常成功経路は `new string(char[], start, length)`、終端・負Indexの非連続経路だけ捕捉配列を使う。
- Span、Cursol、CharParser、JSON構文、入力、ベンチマーク本体、公開API、依存バージョンは変更していない。
- 今回のbaselineはコレクション式版であり、[先行記録](2026-10-04-word-parser.md)の `.ToArray()` 版と同一ソースではない。過去の生成時の割り当て・時間を今回へ流用しない。

Git対象外の `artifacts/word-parser-direct-match-2026-10-04/sources/` に各38ファイルを固定し、WordParserだけをbaselineのコミット版に置き換えた。ハッシュ比較でWordParser以外の37ファイルが同一であることを確認した。その後、双方へ同一の `NuGet.Config` を追加して、自動測定ビルドの復元先もローカルの空feedに限定した。依存は既存NuGetキャッシュを使い、新規導入・外部通信は行っていない。

WordParser.csのSHA-256:

- baseline: `4B7F0FCFB18274419D6DD47AABC3B714E2B4926759276115E077B7EAD4A36B50`
- direct: `757FA89B347142E7C010525634B57A3C2BDC04F00E18560A62296E613C5436A9`

測定後もdirectと元の作業ツリーのWordParserのハッシュ一致を確認した。Git対象の本体・テスト・設定は測定中に編集していない。

## 入力・環境・バージョン

- 入力: 既存の `Kotowari.Benchmarks/Data/test.json`。縮小・置換なし。
- サイズ: 4,172,062 bytes / 4,172,062 UTF-16コード単位。
- SHA-256: `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。
- OS: Windows 11 25H2、10.0.26200.9457。
- CPU: Intel Core i7-13700H 2.90GHz、14物理 / 20論理コア。
- SDK: 10.0.401。Runtime: .NET 10.0.12 (10.0.1226.42308)、X64 RyuJIT x86-64-v3。
- GC: Concurrent Workstation。
- BenchmarkDotNet: 0.15.8。復元済みバージョンをproject.assets.jsonで確認。
- Pidgin 3.5.1、Sprache 2.3.1、SpracheJSON 1.0.1、Superpower 3.2.1。System.Text.Jsonはランタイム付属。
- 今回はKotowariの変更前後だけを時間測定した。他ライブラリの順位は更新していない。
- BenchmarkDotNetが高パフォーマンス電源プランを使い、各実行後に元のプランへ戻した。
- ユーザーから他作業を停止して測定してよいとの回答を得て実行した。測定・ビルド・テスト・別エージェントの作業は並行させていない。ただしOS等の背景負荷、温度、実効クロックは計測しておらず、完全な無負荷環境の証明とはしない。

## 方法と全入力消費の確認

両コピーをReleaseでビルドしてから測定した。測定外の検証プログラムで非公開JObjectパーサーの結果を確認し、双方とも成功、JSONObject生成、4,172,062 / 4,172,062文字の全消費、100,001ノードを確認した。全キー・値の同一性や任意JSONへの適合性を保証する検査ではない。

JSONは既存 `JsonParserBenchmarks.Kotowari` を使用。ファイル読み込みはGlobalSetupで測定外、入力文字配列化・解析・結果構築は測定内。単体は既存 `WordParserBenchmarks` を使い、Setupで成功・失敗・値・カーソル位置を検証した。

共通条件はRelease / Shortベース、LaunchCount=3、WarmupCount=10、IterationCount=10、OutlierMode=DontRemove、Affinity=4（論理CPU 2）。段階的JITなどのランタイム環境変数は変更していない。

- JSON: InvocationCount=4、UnrollFactor=1。配列保持→直接照合→直接照合→配列保持の順、各3起動・各10反復、合計12プロセス・120反復。
- 単体: IterationTime=150ms、実行数とunrollはBenchmarkDotNetの既定・自動調整。配列保持→直接照合の順。3入力×2利用方法×各3起動、合計36プロセス・360反復。
- すべて順次実行。外れ値は除去せず、起動やケースを選別していない。
- ErrorはBenchmarkDotNetの99.9%信頼区間の半幅。起動内の10反復を10回の独立した起動とみなさない。
- MiBは1,048,576 bytes。GC回数は1,000操作あたり。割り当ては累積マネージド割り当て。

## JSON全体

| 実行順 | 実装 | 起動別平均 (ms) | 全体平均 (ms) | Error (ms) | StdDev (ms) | 割り当て (MiB) |
|---|---|---:|---:|---:|---:|---:|
| 01-baseline-json | cached-array | 203.69 / 132.64 / 201.72 | 179.35 | 22.53 | 33.72 | 63.09 |
| 02-direct-json | direct-match | 201.35 / 293.10 / 268.77 | 254.40 | 46.39 | 69.44 | 56.45 |
| 03-direct-json | direct-match | 254.21 / 258.47 / 176.32 | 229.67 | 26.31 | 39.38 | 56.45 |
| 04-baseline-json | cached-array | 256.92 / 177.77 / 262.73 | 232.47 | 27.59 | 41.30 | 63.09 |

6起動ずつの平均は配列保持205.91ms／直接照合242.04ms。起動平均の標本標準偏差は49.02ms／44.08ms、範囲は132.64〜262.73ms／176.32〜293.10ms。範囲は重なり、各JSON実行にMultimodalDistribution警告が出た。高速化を確認できなかったという判断と、遅くなった原因を特定したという判断を区別する。

各起動の割り当ては配列保持約66,150,000bytes、直接照合約59,195,000bytes。GC診断の小さな起動差もCSVに保持した。Gen0は減った一方、今回のJSON集計ではGen1が増えている。割り当て減少をGC時間減少と同一視しない。

## 単体

平均 ± Error (ns)、割り当て (B)。値は3起動・30反復の集計であり、固定された一般的な実行速度ではない。

### 同じパーサーを再利用

| 入力 | 配列保持 (ns) | 配列保持 (B) | 直接照合 (ns) | 直接照合 (B) |
|---|---:|---:|---:|---:|
| `0,` | 73.67 ± 11.70 | 32 | 58.34 ± 21.13 | 40 |
| `false,` | 166.73 ± 105.14 | 176 | 150.85 ± 68.52 | 32 |
| `falsX,` | 46.30 ± 1.51 | 64 | 203.77 ± 49.33 | 0 |

### 生成して一度だけ解析

| 入力 | 配列保持 (ns) | 配列保持 (B) | 直接照合 (ns) | 直接照合 (B) |
|---|---:|---:|---:|---:|
| `0,` | 385.99 ± 95.49 | 456 | 77.74 ± 20.11 | 104 |
| `false,` | 340.90 ± 56.71 | 600 | 239.48 ± 110.20 | 96 |
| `falsX,` | 535.89 ± 78.30 | 488 | 168.36 ± 68.40 | 64 |

生成経路の割り当ては大きく減ったが、JSONのnull/true/falseパーサーは静的に生成・再利用されるため、その効果をJSON全体の時間改善に外挿しない。

両単体実行にMinIterationTimeとMultimodalDistribution警告が出た。目標150msを指定しても実測の最短反復は配列保持21.686〜66.316ms、直接照合23.863〜32.318msで、100msの推奨を下回るケースがあった。単体の小さな差や正確な改善倍率を断定しない。各起動の平均・最小・最大・標準偏差は起動別CSV、全反復は反復CSVに残した。

## 検証・失敗・未確認

- 最初のコピー復元はSDKフォルダーのアクセス制限で失敗。必要なアクセスを許可された実行でやり直し、両コピーの復元・Releaseビルドが成功した。
- 全入力消費の一時検証は最初、依存DLLの読み込みに失敗。ローカルDLL解決を追加して再ビルドし、両版の最終検証に成功した。このプログラムは測定区間では実行していない。
- BenchmarkDotNetの自動ビルド・全6実行・48起動が成功。失敗した解析の時間を成功扱いしていない。
- 既存のSpracheJSON 1.0.1の手動DLL参照・NU1701警告・Optimization Validator無効化を維持した。
- 直接照合実装の直前検証は全242テスト成功・両ターゲットビルド成功。本測定では本体・テストを変更しておらず、その検証を再実行していない。今回の新しいテスト実行結果とは扱わない。
- 前工程のCA2208（既存のnull word例外のParamName=`source`を維持）、IDE0290、既存整形指摘は未変更。測定のために診断を抑制していない。
- 背景負荷、実効クロック、温度、本測定区間のJITイベントは未収集。ばらつきの原因は未特定。
- 測定対象は.NET 10のみ。netstandard2.0利用側の別ランタイム性能は未測定。
- 直接照合の外部からの並行文書書き換えの振る舞いは未検証。

## 記録

- [統計CSV](2026-10-04-word-parser-direct-match.csv): 全16集計（JSON4、単体12）。
- [反復CSV](2026-10-04-word-parser-direct-match-iterations.csv): 480実測反復それぞれのWorkloadActualとWorkloadResult、計960行。同じ反復の生値とオーバーヘッド補正値を2回の独立試行として数えない。
- [起動別CSV](2026-10-04-word-parser-direct-match-launches.csv): 全48プロセス。時間はWorkloadResult10反復、割り当て・GCは対応する各起動のGC診断から取得。
- 元ログ、HTML、元CSV、完全JSON、固定ソース、一時検証プログラムはGit対象外の `artifacts/word-parser-direct-match-2026-10-04/` に保存。
- この記録とCSVを追加しただけで、本体・テスト・ベンチマーク・設定の追加変更、ステージ、コミット、pushはしていない。

## 再現コマンド

コピーを上記のソースで作成し、既存キャッシュから復元・Releaseビルドする。各コピーのルートで実行し、実装・実行ごとに異なるartifactsパスを指定する。測定中はほかの重い作業を実行しない。

```powershell
dotnet Kotowari.Benchmarks/bin/Release/net10.0/Marimo.Kotowari.Benchmarks.dll --filter "*JsonParserBenchmarks.Kotowari*" --job short --launchCount 3 --warmupCount 10 --iterationCount 10 --invocationCount 4 --unrollFactor 1 --outliers DontRemove --affinity 4 --exporters json --artifacts artifacts/reproduction/json

dotnet Kotowari.Benchmarks/bin/Release/net10.0/Marimo.Kotowari.Benchmarks.dll --filter "*WordParserBenchmarks*" --job short --launchCount 3 --warmupCount 10 --iterationCount 10 --iterationTime 150 --outliers DontRemove --affinity 4 --exporters json --artifacts artifacts/reproduction/word
```
