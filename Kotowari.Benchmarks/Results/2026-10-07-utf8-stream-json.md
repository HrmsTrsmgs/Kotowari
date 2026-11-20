# 大きなJSONでの文字列・UTF-8入力経路比較（2026-10-07）

## 結果

同じ4.17 MBのJSONをMemoryStreamから読み、文字列入力とUTF-8入力で解析・結果木を生成した。UTF-8経路は文字列経路より平均52.0%遅かった。8組の隣接起動から求めた時間比の幾何平均は+52.0%、95% t区間は+47.5%〜+56.6%。今回の条件では、UTF-8入力が文字列入力と同等以上の速度という基準を満たさない。

| 経路 | 起動 | 平均ms/JSON | 起動平均の範囲ms | 起動間CV | 割り当てMiB/JSON |
|---|---:|---:|---:|---:|---:|
| 文字列：読み込み→UTF-8復号→解析 | 8 | 145.791 | 138.094–155.126 | 3.809% | 61.515 |
| UTF-8：読み込み→UTF-8解析 | 8 | 221.542 | 213.663–231.325 | 2.522% | 57.536 |

UTF-8経路では1回あたり平均4,171,931 bytes（約3.98 MiB）のマネージド割り当てが減った。速度と割り当ての結果は今回の入力・実装に対する観測値であり、他のJSONやストレージからの読み込み時間まで一般化しない。

## 比較対象と検証

- 実装: commit `f4044ec` の `JSON.Parse(string)` と `JSON.Parse(ReadOnlySpan<byte>)`。両経路ともKotowari JSONパーサー。
- 入力: `Kotowari.Benchmarks/Data/test.json`、4,172,062 bytes、SHA-256 `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。BOMなし、ASCIIのみ（4,172,062文字）。
- 測定前の検証: System.Text.Jsonで入力全体を文書として解析し、両Kotowari経路の結果木が全値一致すること、100,001ノードであることを確認。
- 入力バイト列はGlobalSetupで読み込み、測定ごとに同じMemoryStream経路で別のMemoryStreamへコピーした。コピー先は入力長の容量をあらかじめ確保。
- 文字列経路はコピーした範囲をUTF-8からstringへ復号して解析する。UTF-8経路は同範囲のReadOnlySpan<byte>を解析APIへ渡す。API内でspanからbyte配列へのコピーも含む。両経路とも読み込み、復号／入力準備、全JSON解析、結果木生成を測定。
- MemoryStreamにより読み込み量と処理を固定した測定であり、物理ディスクやファイルシステムの遅延は測っていない。

## 条件と統計

- Windows 11 25H2、Intel Core i7-13700H、.NET SDK 10.0.401、.NET Runtime 10.0.12、BenchmarkDotNet 0.15.8、Release、X64 RyuJIT。
- `DOTNET_TieredPGO=1`。PGOを無効にしていない。高パフォーマンス電源プラン、Affinity mask `00000000000000000100`。測定後に電源プランがバランスへ戻ることを確認。
- 各経路8つの新規プロセス。各起動 Warmup 16、Actual 8、InvocationCount 4、UnrollFactor 1、OutlierMode `DontRemove`。隣接する2起動を1組とし、文字列→UTF-8／UTF-8→文字列の順を交互にして8組実施。
- 表の平均・CVは起動ごとの8反復平均を等重みで集計。反復を独立プロセスとして数えていない。比較区間は8組のUTF-8／文字列時間比を対数化し、平均の95% t区間（自由度7）を指数変換した。
- 最初の隔離環境からの一連の試行はBenchmarkDotNetが測定プロセスを生成できず、各回0件だったため破棄した。記録した16起動では、それぞれ8 Actual反復を確認した。

検証済みのホストコード、BenchmarkDotNet出力、全反復、起動ログはGit対象外の `artifacts/utf8-json-end-to-end-2026-10-07/` にある。各起動・各反復は `word-parser/summary.csv`、`launches.csv`、`iterations.csv` に `Experiment=2026-10-07-utf8-stream-json` として記録。
