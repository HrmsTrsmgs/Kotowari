# Cursol文字列Document化・巨大JSON全体測定（2026-10-06）

## 結論

`Cursol`の文書保持を`char[]`から元の`string`を保持するDocumentへ変更しても、この巨大JSONベンチマークでは処理時間の改善を確認できなかった。16回の独立起動のうち各版8回を比較した平均は、baseline 151.360 ms、candidate 149.854 ms（candidateが名目上0.995%短い）だった。ABBA順序のペア差の95%信頼区間は-3.666%から+1.715%で0を含むため、速度差は判定不能とする。

一方、割り当て量はbaseline 60,335,063 B/parseからcandidate 51,988,398 B/parseへ8,346,665 B（13.834%）減った。JSON入力文字列を`char[]`へ複製しなくなる変更と整合する。GC回数の記録値は両版で同じだった（1,000 parseあたりGen0 4,500、Gen1 1,750、Gen2 500）。

したがって、今回の効果として確認できたのは主にメモリ割り当ての削減であり、解析速度の向上ではない。これは同一入力・同一環境での当該ベンチマーク結果であり、一般的な速度優劣を示すものではない。

## 比較対象と再現情報

| 項目 | 値 |
|---|---|
| Benchmark | `JsonParserBenchmarks.Kotowari`（Kotowariアダプターのみ） |
| 入力 | `Kotowari.Benchmarks/Data/test.json`、4,172,062 bytes / UTF-16 code units |
| 入力SHA-256 | `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D` |
| baselineソース | commit `03e3edc4e5ddafb2a4d1a5f5b2c982954301c8b5`、`Cursol.cs` SHA-256 `1A51B08778EE8C09EE33E3D8D4C834CE8525F4C5CE758AD4F48E24625E346BD9` |
| candidateソース | 同じcommitの他のソースに作業ツリーの`Cursol.cs`変更を適用、SHA-256 `26FF3C8245A74F7C721CA4482418BD36778B042185920A62D2824509C4A51948` |
| SDK / Runtime | .NET SDK 10.0.401 / .NET 10.0.12 |
| BenchmarkDotNet | 0.15.8 |
| OS / CPU | Windows 11 25H2 / Intel Core i7-13700H |
| 実行方式 | 各測定を新規プロセスで起動、baseline/candidateをABBA順で4組（各8起動） |
| PGO | Dynamic Tiered PGO有効（親プロセス環境変数`DOTNET_TieredPGO=1`, `COMPlus_TieredPGO=1`） |
| Job | Release、Affinity mask `00000000000000000100`、InvocationCount 4、UnrollFactor 1、Warmup 16、Iteration 8、IterationTime 500 ms、OutlierMode `DontRemove` |

candidateのソースハッシュは測定時点の保存コピーを示す。測定後に`Document`のXMLコメントを改訂したため、コミットするファイルのハッシュは異なるが、コメント以外のコードが保存コピーと一致することを確認した。

測定中は同一マシンの重い作業を止める前提で実行した。短い事前プロセスCPU確認は0.00%表示だったが、OS全体の負荷を連続記録したわけではない。測定入力は既存のファイルをそのまま使い、Kotowariの両アダプター実行で各100,001ノードが報告された。木全体のハッシュ比較は行っていないため、結果構造が完全一致したとは主張しない。

## 起動別結果

平均は各独立プロセス内の8反復の平均を、版ごとに等重みで平均した値。範囲と中央値はプロセス平均の範囲・中央値。割り当て値はBenchmarkDotNet出力のMiB精度をbytesへ換算した値であり、その表示精度に基づく。

| Variant | 起動数 | 平均 (ms) | 起動平均の中央値 (ms) | 起動平均の範囲 (ms) | 起動間CV | 割り当て (B/parse) |
|---|---:|---:|---:|---:|---:|---:|
| baseline (`char[]`) | 8 | 151.360 | 150.804 | 147.947–156.497 | 1.82% | 60,335,063 |
| candidate (string Document) | 8 | 149.854 | 149.495 | 143.354–155.511 | 2.69% | 51,988,398 |

ペアは隣接するABBAのbaseline/candidate（順序を反転した組を含む）として対応づけ、対数比を用いた対応ありt区間で速度差を評価した（8ペア、自由度7）。差の区間が0をまたぐこと、各プロセス内にもばらつきがあることから、約1%の速度差は効果として確定できない。

## 保存データ

起動ごとのBenchmarkDotNetログ、CSV、測定用hostとソースコピーはGit対象外の`artifacts/cursol-string-document-json-2026-10-06/valid-runs/`以下にある。16起動すべて終了コード0で、各8反復の結果を出力した。要約統計は既存の`word-parser/summary.csv`、起動統計は`word-parser/launches.csv`、各起動のActualおよびオーバーヘッド補正済みResult反復は`word-parser/iterations.csv`へ記録した。

BenchmarkDotNet出力に`SpracheJSON`由来のNU1701互換性警告があったが、本測定対象はKotowariだけであり、測定行には含めていない。
