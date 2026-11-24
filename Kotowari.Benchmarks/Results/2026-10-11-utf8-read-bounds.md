# UTF-8読み取り範囲判定の評価 2026-10-11

`Utf8Document.Read`の範囲判定を一つの符号なし比較にまとめる候補は不採用とした。生成コードのチェック削減は確認したが、通常計時の予備4組では候補／現行が1.021745倍となり、速度改善を確認できなかった。本体の一行変更は元に戻した。

基点は`5e80990ed5f335c88b0c651770a47c58692c72f0`。解析処理や入力保持の構造は変えていない。

## 候補を選んだ根拠

基点のPGO有効・巨大JSONのCPU採取では、UTF-8の401標本のうち`SequenceParser<Canon,char>`のselfが31、`SequenceParser<char,(Canon,char)>`が29だった。これにはインライン化された子の処理も含まれ、Sequence自身だけのコストではない。

同じ基点の別プロセスのTier 1出力を調べると、`SequenceParser<Canon,char>`のIG13には、展開されたUTF-8読み取りの負数判定`jl`、末尾以上の判定`jge`、後続のSpanスライスの判定`ja`があった。この事実から、範囲チェックをまとめる余地を調べる。チェック数やコードサイズだけで速くなるとは判断しない。

対象のJIT出力は`artifacts/utf8-identity-length-2026-10-11/diagnostics/utf8-jit-baseline-01/jit.asm`の63385行から始まるTier 1本体。CPU標本は`artifacts/current-utf8-cpu-2026-10-10/eventpipe/utf8-01/cpu-analysis.json`にある。両者は別プロセスであり、標本の命令位置をこの出力へ直接対応付けたものではない。

## 変更と契約

変更前:

```csharp
if (index < 0 || index >= utf8Length) return Cursol.NullCharacter;
```

候補:

```csharp
if ((uint)index >= (uint)utf8Length) return Cursol.NullCharacter;
```

保持する長さはSpanの非負の長さであり、負のindexはuint変換でその長さを上回る。範囲内・範囲外の判定結果は維持する。文字の復号、進行位置、借用・コピー、文書の同一性、公開API、文字列版は変更しない。前のList初期容量候補は撤去し、この一行と混ぜない。

## 検証と測定の区別

Release全352テストが成功（本体237、JSON115、失敗・Skipなし）。全体Releaseビルド＋アナライザーは成功し、MSBuild表示の所要時間は18.10秒だった。既存NU1701が1件、5プロジェクトのSARIFは既存6診断だけで、変更した行には診断がない。全体format検査は既存WHITESPACE・IDE等の指摘で終了コード2。無関係な指摘の一括修正は行わない。

まず同じ巨大JSONを使った候補のJIT出力を別プロセスで取得し、ホットな呼び出し元のチェックが実際に減るかを確認する。PGO有効、affinity 4、warmup 16、Actual 8、invocation 4、unroll 1、launch 1、DontRemoveを維持する。診断付きの所要時間は通常計時へ混ぜない。

候補のソース、ホスト、入力のハッシュ、全文消費と100,001ノードの結果一致、生成コード、起動記録は`artifacts/utf8-read-bounds-2026-10-11/`へ分離して保存する。

## 生成コードの確認

候補を隔離してReleaseビルドし、文字列・UTF-8両経路の全文消費とSystem.Text.Jsonとの結果一致、100,001ノードを確認した。49ファイルのうちUtf8Documentだけが基点と異なり、共有ホストのProgram.cs・JsonStreamBenchmarks.csと入力は基点と同じである。

2026-10-10 19:22:58–19:23:43 UTCの新規1起動でJIT出力を取得した。終了コード0、Actual/Result各8点、AC接続・battery saver無効・電源プラン不変、同じOSスレッドでSystemRequiredの取得と解除が成功した。EventPipeは併用していない。

| 対象のTier 1生成コード | 基点でのチェック | 候補でのチェック |
|---|---|---|
| Utf8Document.Read単体 | jl、jge、ja | unsigned cmp/jaeの1分岐 |
| SequenceParser&lt;Canon,char&gt;内の読み取り | jl、jge、ja | 確認した箇所ではjge、jaが残る |
| DelimitedSequenceParser&lt;Canon,char&gt;内の読み取り | jl、jge、ja | 確認した箇所ではunsigned cmp/jaeの1分岐 |

ホットな呼び出し元でもチェックが減る箇所がある。一方、すべての呼び出し元・すべてのチェックが同じように除去されたわけではない。生成コードの比較を通常計時へ進む根拠とし、まだ速度改善の証明にはしない。行番号付きの詳細は`diagnostics/utf8-jit-candidate-01/analysis.md`に保存した。

| 対象 | SHA-256 |
|---|---|
| 基点のUtf8Document.cs | CCD61F20C5886B34196BB1A93C510DC33F354F38B26CA3AF1B862FB4ADD4DE9A |
| 候補のUtf8Document.cs | 5DB6EE417C7AF4861AF5FEF36AC4D120F8601FD866873057F31D7B44D801FD3D |
| 候補ソースmanifest | EB0A2A294A87C1CD832D4EDB81D9BEF3133EF0E39CD3CCE2A7C5B13B5C72D9D6 |
| 候補ホスト | CB92E342DC49DFDB4B113F221DDA7C7FD1DE51B6E0EB0C9A7B2730353B8446EE |
| 入力 | DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D |

## 通常計時の事前計画

まず現行UTF-8 BUと候補UTF-8 CUを4組8新規起動で測る。順序は`BU CU / CU BU / BU CU / CU BU`。起動平均の対応比CU/BUの幾何平均が1未満なら、予備を含めない独立した本測定へ進む。予備で改善と確定しない。

本測定は16組×3条件の48新規起動に固定する。対象はBU、CU、UTF-8対応前の文字列O。順序は`O BU CU / CU BU O / BU CU O / O CU BU / CU O BU / BU O CU`を2回繰り返し、その後に最初の4組を実施する。主比較は16組のCU/BUの対応log比から求める幾何平均と95% t区間（df=15、t=2.131449545559323）。区間全体が1未満なら改善を支持すると判断する。途中結果によって組数を延長・短縮しない。CU/Oと最終目標1.10は別に評価する。

今回の変更対象はUTF-8のReadのみなので、候補の文字列経路の時間は測定対象外とする。文字列ソースの不変と結果一致は確認するが、速度が同等と測定で確認したとは報告しない。

通常計時はCPU採取・JIT出力なし、PGO有効、affinity 4、warmup 16、Actual 8、invocation 4、unroll 1、launch 1、DontRemove。起動内の反復を独立標本に数えず、各起動の平均を比較する。既存のBDN割り当て診断値は併記する。

入力は既存の4,172,062 bytesの全ASCII JSON。MemoryStream作成・読み取り用バッファ作成・コピー・全文解析・結果木生成を含み、文字列ではUTF-8からstringへの変換も含む。ディスク読み取りはGlobalSetupで済ませ、計時外とする。

環境は.NET 10.0.12、SDK 10.0.401、BenchmarkDotNet 0.15.8、Release x64 RyuJIT、Concurrent Workstation GC、Windows 11 25H2 build 26200.9457、Intel Core i7-13700H。基点UTF-8ホストは`8972D0891DFD03FB0CF5E6582132A84FC3EAB00F90F322796A65A58D4B50A35D`。対応前文字列は実コミット`defe7830d2e11b9761f0902e2ad9f0b08acc96b6`、ホストは`CD76D06DED9F00EC6E7BDFB4C59819E25A1A9CDF7899A152A24A137CF0C3393B`。

## 予備結果と判断

2026-10-10 19:29:39–19:34:19 UTCに8起動を実施した。全起動が終了コード0、Actual/Result各8点。AC接続、battery saver無効、開始前後の電源プラン不変と、SystemRequiredの取得・解除成功を確認した。

| 組 | 現行UTF-8 ms | 候補UTF-8 ms | 候補／現行 |
|---|---:|---:|---:|
| 1 | 208.204 | 185.414 | 0.890538 |
| 2 | 223.360 | 225.370 | 1.008998 |
| 3 | 189.512 | 205.341 | 1.083525 |
| 4 | 184.494 | 206.524 | 1.119408 |

対応比の幾何平均は1.021745。1未満という本測定への事前進行条件を満たさないため、48起動の本測定は実施しなかった。最初の有利な一組だけを採用せず、4組すべてを使った。割り当ては両方とも約56.16 MB/opで同程度だった。

チェック削減が時間短縮になるとは今回確認できず、候補を撤去した。これは速度が同じと証明したことや、あらゆる入力で悪化すると断定することではない。全起動・反復値は`runs/preliminary/selection-01/summary.json`と起動別BDN出力、簡約表は同じ場所の`summary.md`へ保存した。
