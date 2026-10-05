# Cursol読み取り専用化の巨大JSON全体測定（2026-10-05）

## 結果

HEAD `f9f35e0` の `Cursol` と、作業中の読み取り専用版を、既存4.17 MB JSONの全解析で比較した。PGOは.NET 10の既定設定で有効。**速度差は明確に確認できなかった。**

| 指標 | 変更前 | 読み取り専用版 | 観測差 |
|---|---:|---:|---:|
| 8起動の平均 (ms/解析) | 150.075 | 150.923 | +0.566% |
| 起動平均の範囲 (ms) | 143.780–159.623 | 143.620–160.532 | — |
| 起動間CV | 3.91% | 3.91% | — |
| 割り当て中央値 (B/解析) | 60,330,555 | 60,329,902 | −653 B |
| Gen0 / Gen1 / Gen2 (回/1,000解析) | 4,500 / 1,750 / 500 | 4,500 / 1,750 / 500 | 同じ |

8つの隣接した変更前後ペアについて、各起動の平均時間比を対数変換して求めた95% t区間は **−3.99%〜+5.34%**（自由度7）だった。観測平均は読み取り専用版が約0.85 ms遅いが、区間はゼロを含み、起動ごとの差も約3.9%あるため、性能低下とは判定しない。1〜2%程度の効果もこの測定では見分けられない。

割り当て中央値は実質同じ。読み取り専用版の1起動だけ60,823,514 Bとなり、ほか7起動の約60.33 MBから外れたが、削除せず全件を記録した。GC回数/1,000解析は全起動で一致した。

## 比較したソースと解析結果

- 変更前の基点: `f9f35e0651061e9373cca02b1814969d0dba7f70`。
- 変更後: 同じ基点へ作業中の `Kotowari/Core/Cursol.cs` だけを適用。テストファイルの差分はベンチマークビルドへ含めていない。
- 変更前後の `Cursol.cs` SHA-256: `B214B0429171708037DF59C264FDFE636E6630627EF9A7574D034421D86E63B9` / `1A51B08778EE8C09EE33E3D8D4C834CE8525F4C5CE758AD4F48E24625E346BD9`。
- 既存入力 `Kotowari.Benchmarks/Data/test.json`: 4,172,062 bytes、SHA-256 `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。
- 両版で4,172,062 / 4,172,062文字を消費し、100,001ノードを生成。正規化した結果木のSHA-256は両版とも `5ABDB9015EF96B7E79FADB54B636AF0C5AB9D0AA1864C5AF21FF8669C2A4233C`。

## 条件

- Windows 11 25H2、Intel Core i7-13700H、.NET SDK 10.0.401、.NET Runtime 10.0.12、BenchmarkDotNet 0.15.8、Release、X64 RyuJIT。
- 各版8つの独立プロセス、交互順 `baseline, readonly, readonly, baseline` を4回。各プロセスでWarmup 16回、Actual 8反復、1反復4解析、Affinity 4、UnrollFactor 1、OutlierMode `DontRemove`。外れ値ヒントが出た反復も保持した。
- `DOTNET_TieredPGO` / `COMPlus_TieredPGO` に上書きはなく、.NET 8以降の既定でDynamic PGOが有効な条件。[Microsoftのランタイム設定資料](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/compilation#profile-guided-optimization)および[.NET 8の変更点](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/runtime#dynamic-pgo)を参照。
- BenchmarkDotNetは各起動中に高パフォーマンス電源プランへ切り替え、終了時にバランスへ復元した。測定前の3秒CPU使用量サンプルでは、観測したVisual Studioプロセスの負荷は全論理CPU合算の約0.21%。測定中の継続的な負荷記録は取得していない。
- 1回目の準備実行は、リポジトリ内に同名ベンチマークプロジェクトが複数あり、BenchmarkDotNetがホストを選べず測定0件で終了した。ログはraw artifactに残し、数値へ含めていない。両版で名前の異なる専用ホストを作り直し、以降の16起動すべてで8 Actual反復を確認した。
- 隔離した2ホストのReleaseビルド成功。従来どおりSpracheJSON参照のNU1701警告あり。事前検証では両版とも全文消費・ノード数・結果木ハッシュが一致した。

## 保存場所

16起動のBenchmarkDotNetログと完全JSONは、Git対象外の `artifacts/cursol-readonly-json-2026-10-05/runs/` に保存した。最初の無効な0件起動とそのログは `artifacts/cursol-readonly-json-2026-10-05/01-baseline/` に分けて残している。集計値とActual/Result各反復は既存 `word-parser/summary.csv`、`iterations.csv`、`launches.csv` に `Experiment=2026-10-05-cursol-readonly-json` として追記した。
