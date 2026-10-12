# UTF-8カーソルの保持情報を縮小する評価 2026-10-11

不採用。保持情報は8 bytes縮小したが、40起動の通常計時では速度改善を確認できず、候補は元に戻した。

## 仮説と変更範囲

基点は`5e80990ed5f335c88b0c651770a47c58692c72f0`。直前の[OneOrMoreのPrepend候補](2026-10-10-one-or-more-prepend.md)は戻し、混ぜていない。

現行版のCPU採取ではSequence系列のself標本がUTF-8で68/401あった。新たなPGO有効のTier 1採取でも、Sequenceの本体、カーソルの移動、解析結果からのカーソル取得に値の転送が残っていた。標本はmanaged stack単位であり、転送命令そのものがCPU時間を支配すると確定したわけではない。

`Unsafe.SizeOf<T>()`による現行の実サイズは次のとおり。入力データやヒープオブジェクトのサイズではなく、各値型そのもののサイズである。

| 型 | 文字列用 bytes | UTF-8用 bytes |
|---|---:|---:|
| Document | 8 | 24 |
| Cursol | 16 | 32 |
| ParseResult&lt;char&gt; | 24 | 40 |
| ParseResult&lt;string&gt; | 32 | 48 |

候補の再計測では、UTF-8文書16、カーソル24、ParseResult&lt;char&gt;32、ParseResult&lt;string&gt;40 bytesとなり、それぞれ8 bytes縮小した。文字列用のサイズは変えていない。

UTF-8文書はmanaged byref、int長さ、文書同一性のためのobject参照を保持している。候補ではint長さを、既存の文書同一性オブジェクトの役割を引き継ぐ`Utf8DocumentIdentity`へ移す。カーソルと解析結果に複製する状態を小さくする狙いである。

```csharp
readonly ref readonly byte utf8Reference;
readonly Utf8DocumentIdentity identity;

int Utf8Length => identity is null ? 0 : identity.Length;
```

既存の`new object()`を`new Utf8DocumentIdentity(utf8.Length)`へ置き換えるため、文書ごとのオブジェクト数は増えない。入力のコピー、pinning、raw pointerへの変更、公開APIの変更、パーサーの手動インライン化は行わない。内部状態型は専用ファイルへ分ける。

長さの読み取りは、値型内のintから共有オブジェクトへの間接参照へ変わる。null判定も必要になる。小さくなれば速いと決めつけず、このコストと生成コードの変化を含めて全体計時で判断する。既定カーソルは引き続き空入力として扱う。

## 診断で確認したこと

基点ホストは`CurrentUtf8Cpu.Measurement.dll`、SHA-256は`8972D0891DFD03FB0CF5E6582132A84FC3EAB00F90F322796A65A58D4B50A35D`。凍結した本体・JSONサンプル49ファイルのハッシュは基点と一致した。

候補の凍結ソースは50ファイル。基点との差分は`Utf8Document.cs`の変更と`Utf8DocumentIdentity.cs`の追加だけで、測定用の`Program.cs`と`JsonStreamBenchmarks.cs`は同一。候補の全文消費、100,001ノード生成、System.Text.Jsonとの結果一致も計時前に検証した。専用JSONパーサーとの速度比較ではない。

| 対象 | SHA-256 |
|---|---|
| 候補ホスト `Utf8IdentityLength.Candidate.Measurement.dll` | `37293C47F17F9C112121E9DC370F84081240949302ED472310E668C05E1FCD12` |
| 現行 `Utf8Document.cs` | `CCD61F20C5886B34196BB1A93C510DC33F354F38B26CA3AF1B862FB4ADD4DE9A` |
| 計測候補 `Utf8Document.cs` | `2FA3D1510EBAC53599236E344CFDD4ADC0E9C5355E94F1141B49F88E3CB076D0` |
| 候補 `Utf8DocumentIdentity.cs` | `FBBEA5ED8ED8116A7CC78E3918FB04E7748AF75D7C878FF0A95680E73B8091EC` |

新規UTF-8 JIT採取は通常計時と分離し、PGO有効、affinity 4、warmup 16、Actual 8、invocation 4、unroll 1、launch 1、DontRemoveで1起動。終了コード0、Actual/Result各8点を確認した。

`Sequence<__Canon,char>.ParseCore`のTier 1版は、入口でR8のカーソル引数参照をRBXへ保持する。RCXは実行器自身、RDXは戻り値バッファであり、カーソルではない。本体のIG48/IG50にカーソル一時値の32-byte転送、IG56に引数転送がある。`GoFoward`と`ParseResult.get_Cursol`にも32-byte YMM転送がある。個々の経路の実行割合や命令別の寄与は未分離。

## 通常計時の条件と評価方法

既存JSONは4,172,062 bytes、全ASCII、SHA-256は`DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。縮小、再生成、置換はしない。MemoryStreamからの読み取り・バッファ作成・コピー、全文解析、結果構築を含む。文字列版ではUTF-8からstringへの変換も含む。物理ディスクからの読み取りはGlobalSetupで済ませて計時外とする。

.NET 10.0.12、SDK 10.0.401、BenchmarkDotNet 0.15.8、Release x64 RyuJIT、Concurrent Workstation GC。Windows 11 25H2 build 26200.9457、Intel Core i7-13700H。PGO有効、affinity mask 4、warmup 16、Actual 8、invocation 4、unroll 1、launch 1、DontRemove。通常計時にCPU profiler/JIT disassemblyを付けない。

5条件を8組、40回の新規プロセスで直列実行した。組数と順序は開始前に固定し、途中結果で延長・打ち切りしなかった。この候補では先行pilotを実施していない。

| 記号 | 対象 |
|---|---|
| O | UTF-8対応前文字列：実コミット`defe7830d2e11b9761f0902e2ad9f0b08acc96b6` |
| BS | 現行文字列 |
| CS | 候補文字列 |
| BU | 現行UTF-8 |
| CU | 候補UTF-8 |

対応前文字列ホストは`TypedDocument.original.Measurement.dll`、SHA-256は`CD76D06DED9F00EC6E7BDFB4C59819E25A1A9CDF7899A152A24A137CF0C3393B`。比較用に再構成した合成実装や過去の平均値ではない。

順序は`O BS CU CS BU`とその逆順、`BS CS O BU CU`とその逆順、`CS BU BS CU O`とその逆順、`BU CU CS O BS`とその逆順の8組。

主比較はCU/BU。8個の対応するlog時間比の平均±`2.3646242510102993 × 標本SD / sqrt(8)`を指数変換し、幾何平均と95% t区間を求める。新規プロセスのResult平均を比較単位とし、起動内反復を独立試行として数えない。CS/BS、CU/O、BS/Oは副比較であり、同時に多重比較調整した区間とはしない。CU/Oで最終目標1.10倍との距離も確認する。

## 結果と採否

通常計時は2026-10-10 17:22:02–17:42:33 UTC。40起動すべて終了コード0、Actual/Result各8点、各点4操作だった。

| 組 | O ms | BS ms | CS ms | BU ms | CU ms | CU/BU |
|---|---:|---:|---:|---:|---:|---:|
| 1 | 145.317 | 157.991 | 154.119 | 184.565 | 183.606 | 0.99481 |
| 2 | 143.804 | 156.280 | 148.438 | 185.748 | 178.772 | 0.96245 |
| 3 | 141.986 | 156.982 | 153.925 | 181.589 | 179.140 | 0.98651 |
| 4 | 136.536 | 170.184 | 155.324 | 189.941 | 183.841 | 0.96788 |
| 5 | 141.721 | 150.932 | 151.176 | 190.017 | 180.698 | 0.95096 |
| 6 | 146.186 | 149.369 | 156.517 | 177.610 | 180.422 | 1.01583 |
| 7 | 144.597 | 152.735 | 155.261 | 184.392 | 187.641 | 1.01762 |
| 8 | 139.681 | 164.531 | 158.242 | 185.897 | 187.583 | 1.00906 |

| 比較 | 幾何平均比 | 95% t区間 |
|---|---:|---:|
| 主比較：候補UTF-8 / 現行UTF-8 | 0.98785 | 0.96666–1.00950 |
| 参考：候補文字列 / 現行文字列 | 0.98001 | 0.94570–1.01555 |
| 参考：候補UTF-8 / 対応前文字列 | 1.28247 | 1.24763–1.31828 |
| 参考：現行文字列 / 対応前文字列 | 1.10386 | 1.04638–1.16451 |

主比較の点推定は約1.2%短縮だが、区間は約3.3%短縮から約1.0%増加までを含む。改善を示せたとは判断せず、候補は不採用とした。「差がないことを証明した」「カーソル転送は無関係」とも結論しない。型の縮小は確認できたが、それだけで速度改善の代用にはしない。対応前文字列版の1.10倍以内という最終目標も未達である。

通常計時中のAC接続、battery saver無効、電源プランは不変。Systemイベント42/107/506/507は計時区間および集計までの直後区間に該当なし。一時的なSystemRequired保持を確認し、helper heartbeatの最大間隔は10.041秒だった。BDN起動用hostのCPU時間はworkerのCPU時間ではないため、性能評価へ使っていない。

測定後、`Utf8Document.cs`を基点へ戻し、候補専用の`Utf8DocumentIdentity.cs`と既定カーソルの確認テストを取り除いた。他の最適化とは重ねない。試した本体実装・生成物は隔離コピーと以下の再構成情報に保存した。未確認の速度改善を採用理由にしたコメントも本体へ残さない。

## 候補の再構成

基点の`Utf8Document.cs`に対して、`readonly int utf8Length`を削除し、`readonly object identity`を`readonly Utf8DocumentIdentity identity`へ変更する。`int Utf8Length => identity is null ? 0 : identity.Length;`を追加し、Span生成、Readの範囲判定、GoFowardの継続判定で`utf8Length`を`Utf8Length`へ置き換える。コンストラクター内の長さ代入と`new object()`を`identity = new Utf8DocumentIdentity(utf8.Length);`へ置き換える。入力のbyref取得、Rune読み取り、文字幅処理は変更しない。

新しい内部型は同じnamespaceの別ファイルに置く。

```csharp
namespace Marimo.Kotowari.Core;

sealed class Utf8DocumentIdentity(int length)
{
    public int Length { get; } = length;
}
```

測定時のコメントを含む正確なファイルは`candidate/source/`の凍結ソースと`candidate/source-hashes.json`で特定する。コメント差を除く実行上の変更は上記がすべてであり、既定カーソルの確認テストは性能ホストに含めない。

## 動作・静的検査

- 変更前：既定UTF-8カーソルの読み取り・移動・コピーについて既存動作を確認するテストを追加し、カーソル25件が成功。
- 変更後：Release全343件が成功（本体228、JSON115）、失敗・Skipなし。既存の借用スライス、入力変更の反映、ASCII/BMP、文書同一性、補助平面文字拒否のテストを維持した。
- 全体Releaseビルド・アナライザー：18.49秒で成功。既存NU1701が1件、エラー0件。全5プロジェクトのSARIFは既存6診断のみ。
- 全体`dotnet format --verify-no-changes --severity info`：既存の空白やIDE0290等で終了コード1。変更C#3ファイルに限定した同検査は終了コード0。無関係な整形はしない。
- `git diff --check`：成功。既存C#のCRLF/BOMを維持し、新規内部状態型も同じ形式にした。

振る舞いを追加する変更ではなく既存契約の確認なので、形式的なRedを作るために実装を壊してはいない。

## 記録の保存先

`artifacts/utf8-identity-length-2026-10-11/`に現行・候補の凍結ソース、入力ハッシュ、実サイズ、完全BDN JSON、反復値、JIT採取、runner、集計コードを保存する。CSVを新たにGit管理へ増やさない。現行JITの抜粋は`jit-excerpts.md`、全体は`diagnostics/utf8-jit-baseline-01/jit.asm`。

無人実行中はAC接続・battery saver無効を確認し、一時的な`ES_SYSTEM_REQUIRED`を保持する。画面点灯要求や恒久設定変更はせず、10秒ごとのheartbeat、電源状態、解除結果を記録する。保持には上限90分を設け、作業終了時には早期解除する。
