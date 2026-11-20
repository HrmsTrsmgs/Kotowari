# UTF-8文書の入力保持方法の比較 2026-10-10

独立した8組の確認測定では、UTF-8のMemoryStream読み取り・全文解析・結果構築が196.89から186.61 msへ短縮した。対応時間比は0.94820、95% t区間は0.92202–0.97514で、約5.2%の改善を支持した。変更は`Utf8Document`の入力保持方法だけで、入力のコピー、公開API、文法、パーサーの組み合わせ方を変更していない。

この小さな改善は残す。ただし、UTF-8対応前の文字列版に対する時間比は1.29117、95% t区間は1.26347–1.31947で、最終目標の「1.10倍以内」は未達。今回の「効果が出るか週間残量40%まで」という試行は、改善を確認した時点で区切った。

## 仮説と変更

[既存のCPU・JIT調査](2026-10-10-sequence-cpu-jit.md)では、UTF-8のSequence実行器にカーソルの転送・一時領域の初期化が残り、その周辺にCPU標本が集中していた。[以前の共通カーソルの調査](2026-10-07-utf8-span-regression.md)では、Spanの入れ子を外す表現変更で中間コピーと大きな性能後退を縮小できていた。

現在の型別カーソルは`Cursol<TDocument>`の中に`Utf8Document`、その中に`ReadOnlySpan<byte>`を保持している。今回は一段内側のSpanを、同じ入力から取得したmanaged byrefとint長さに分ける候補を試した。現在はDocument自体の入れ子が残るため、以前と同じコピー削減が起こるとは仮定しない。

本体の変更箇所は`Kotowari/Core/Utf8Document.cs`のみ。

```csharp
readonly ref readonly byte utf8Reference;
readonly int utf8Length;
readonly object identity;

ReadOnlySpan<byte> Utf8
    => MemoryMarshal.CreateReadOnlySpan(in utf8Reference, utf8Length);

internal Utf8Document(ReadOnlySpan<byte> utf8)
{
    utf8Reference = ref MemoryMarshal.GetReference(utf8);
    utf8Length = utf8.Length;
    identity = new object();
}
```

`System.Runtime.InteropServices`を追加し、`Read`と`GoFoward`の長さ参照を`utf8Length`、スライス元を`Utf8`へ変更した。両フィールドは同じSpanから取得した後に変更しない。長さと参照の整合性をこの型で維持し、managed byrefの参照先はGCが追跡する。pinning、raw pointer、入力バッファの追加確保は行わない。Runeによる復号、補助平面文字の拒否、文書の同一性、入力変更の反映、文字列文書は変更していない。

## 対象の版と条件

開始時のHEADは`1422292a983a1213b763acb155c1907ddf9f4387`、ブランチは`codex/optimize-word-parser`。開始時には41件の未コミットパスがあり、HEADだけでは今回の現状実装を再構築できない。開始時のソース31件は`artifacts/execution-commonization-2026-10-09/after/source/`と全件一致し、今回の凍結候補はそのうち`Utf8Document.cs`だけが異なる。

| 対象 | SHA-256 |
|---|---|
| 現状ホスト `ExecutionCommonization.After.Measurement.dll` | `F6C61B5574049E521CC156CF1F62875EA66C82486DEF44BA3FE579BAD4D3F7FD` |
| 候補ホスト `Utf8ReadPath.Candidate01.Measurement.dll` | `ECE314E09FA0717BF04511752956C53AB73B7D457A934337BF1C3ABC62E3CB1C` |
| 計測した候補の `Utf8Document.cs` | `016758AF107F44874D2D87D67F4FF134AE531A2B454A86BC7040E55F8825A086` |
| 採用理由のコメント追記後の `Utf8Document.cs` | `CCD61F20C5886B34196BB1A93C510DC33F354F38B26CA3AF1B862FB4ADD4DE9A` |
| 対応前文字列ホスト `TypedDocument.original.Measurement.dll` | `CD76D06DED9F00EC6E7BDFB4C59819E25A1A9CDF7899A152A24A137CF0C3393B` |
| 既存入力 `test.json` | `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D` |

対応前文字列版は実際のコミット`defe7830d2e11b9761f0902e2ad9f0b08acc96b6`の凍結ホスト。比較用に作り直した合成実装ではない。入力は既存の4,172,062 bytes、全ASCIIのJSONを変更せず使用した。

- .NET SDK 10.0.401、.NET 10.0.12、BenchmarkDotNet 0.15.8、Release、X64 RyuJIT、Concurrent Workstation GC。
- Windows 11 25H2 build 26200.9457、Intel Core i7-13700H、affinity mask 4。
- Dynamic PGO=1、warmup 16、Actual 8、invocation 4、unroll 1、launch 1、DontRemove。各条件を新規プロセスで直列実行する。
- 両経路ともMemoryStreamからの読み取り用バッファ作成・コピーを含む。文字列版はUTF-8からstringへの変換も含み、UTF-8版はSpanを借用する。物理ディスクからの読み取りはGlobalSetupで済ませ、計時外。
- shared hostの`Program.cs`はSHA-256 `F7204E12008AAF20B32992246CFC2739295FD9BA35DB594A2CE0CC5EF09C0212`、`JsonStreamBenchmarks.cs`は`0309A360697D91A143267CE061619CDCA920F45F86EAC540216E5947F5F5AE33`を維持。
- 計測前に文字列・UTF-8両経路の全入力消費と100,001ノードの結果を確認し、System.Text.Jsonの結果と照合した。専用JSONパーサーとの速度比較ではない。
- 通常計時にはJIT診断やCPUプロファイラーを付けない。PGO OFFの計測は行っていない。

## 探索測定

2026-10-10 06:25:26–06:34:30 UTCに4条件×4組の16起動を実行した。奇数組は現状文字列、現状UTF-8、候補文字列、候補UTF-8の順、偶数組は逆順。

独立単位は各プロセスの平均。下表はoverhead補正後のBDN Workload/Result平均で、単位はms/JSON。同一プロセス内の8反復を8独立試行とは扱わない。

| 条件 | 組1 | 組2 | 組3 | 組4 | 算術平均 | 起動間SD | 起動間CV |
|---|---:|---:|---:|---:|---:|---:|---:|
| 現状文字列 | 161.363600 | 157.238125 | 162.064625 | 156.675769 | 159.335530 | 2.770948 | 1.739% |
| 候補文字列 | 157.636225 | 157.817841 | 154.709519 | 151.452653 | 155.404059 | 2.994712 | 1.927% |
| 現状UTF-8 | 197.712575 | 201.510013 | 196.979203 | 198.996622 | 198.799603 | 1.990027 | 1.001% |
| 候補UTF-8 | 187.258281 | 187.889106 | 184.696753 | 191.386825 | 187.807741 | 2.756605 | 1.468% |

| 比較 | 対応比の幾何平均 | 95% t区間 df=3 |
|---|---:|---:|
| 候補UTF-8 / 現状UTF-8 | 0.944668 | 0.924449–0.965329 |
| 候補文字列 / 現状文字列 | 0.975300 | 0.942758–1.008965 |

対応した4組のlog時間比から、標本標準偏差とt=3.182446305284263を使って区間を算出した。UTF-8の区間は1を下回る。文字列の区間は1を含み、同等性や文字列版の改善を断定しない。今回の主評価はUTF-8で、両区間を多重比較調整済みの同時区間とは扱わない。

割り当ては現状／候補で、UTF-8が56,159,208／56,158,897 bytes/op、文字列が64,502,596／64,503,846 bytes/opだった。入力コピーを復活させた比較ではなく、マネージド割り当て量も実質同程度。

## 独立した確認測定

探索後に組数を8に固定し、2026-10-10 06:38:44–06:53:00 UTCに3条件×8組の24起動を完了した。途中結果による打ち切りや延長はしない。探索の16起動を確認の主比較へ合算していない。

以下のSはUTF-8対応前の文字列版、Uは変更前の現状UTF-8版、Cは候補UTF-8版を指す。いずれも新規プロセスで、順序とプロセス平均を残す。時間はms/JSON。

| 組 | 起動順 | 対応前文字列 S | 現状UTF-8 U | 候補UTF-8 C | C/U |
|---|---|---:|---:|---:|---:|
| 1 | S → U → C | 147.034484 | 195.608041 | 188.482147 | 0.963571 |
| 2 | C → U → S | 146.040809 | 192.570313 | 182.664228 | 0.948559 |
| 3 | U → C → S | 143.004678 | 212.103441 | 190.103675 | 0.896278 |
| 4 | S → C → U | 148.263997 | 202.673697 | 183.806144 | 0.906907 |
| 5 | C → S → U | 139.980441 | 192.685088 | 185.130531 | 0.960793 |
| 6 | U → S → C | 144.347316 | 192.989778 | 190.794491 | 0.988625 |
| 7 | S → U → C | 144.230531 | 194.638063 | 186.030881 | 0.955779 |
| 8 | C → U → S | 143.354278 | 191.837691 | 185.856103 | 0.968820 |

| 条件 | 算術平均 ms | 起動間SD ms | 起動間CV | 平均割り当て bytes/op |
|---|---:|---:|---:|---:|
| 対応前文字列 | 144.532067 | 2.591970 | 1.793% | 64,502,908 |
| 現状UTF-8 | 196.888264 | 7.057854 | 3.585% | 56,158,414 |
| 候補UTF-8 | 186.608525 | 2.921446 | 1.566% | 56,158,387 |

| 比較 | 対応比の幾何平均 | 95% t区間 df=7 | 判断 |
|---|---:|---:|---|
| 候補UTF-8 / 現状UTF-8（主比較） | 0.948204 | 0.922016–0.975136 | 約5.2%短縮、区間は約2.5–7.8%短縮 |
| 候補UTF-8 / 対応前文字列 | 1.291166 | 1.263472–1.319467 | 1.10倍以内は未達 |
| 現状UTF-8 / 対応前文字列 | 1.361696 | 1.319904–1.404812 | 今回の変更前の隔たり |

各組の`log(C/U)`を取り、8個の平均±`2.3646242510102993 × 標本SD / sqrt(8)`を指数変換した。独立単位を新規プロセスとする近似区間であり、プロセス内反復を水増ししていない。後二つは目標との距離を確認する副比較で、三つを多重比較調整済みの同時区間とは扱わない。

候補の起動間CVは今回小さかったが、8起動だけでPGOのばらつき解消を断定しない。割り当て量は実質同じで、割り当て削減による改善とも説明しない。

通常のSpanフィールド保持より低水準で、JITのコード生成に依存する調整である。変更をこの型の内部に閉じ、参照・長さの不変条件と採用理由を非公開プロパティのremarksへ残す。ランタイムが変わった場合や元の表現へ戻す際には再評価する。パーサーの手動インライン化や構成の分散はしていない。

## 生成コードで分かったこと

候補のUTF-8 JIT診断を通常計時と別に1起動実行し、既存の同一現状ソースのUTF-8診断と比較した。いずれもPGO有効のTier 1。現状診断のActualは32、候補は8であり、同一のprofile分布やインライン化判断を固定した対照実験ではない。

- 最初に狙った`Sequence<Canon,char>`の32-byte引数転送は残った。現状asmの64629–64630行、候補の64530–64531行にはいずれも`vmovdqu ymm`による転送がある。これを除去できたとは報告しない。
- 対象の`Sequence<Canon,char>`の`sub rsp`は744から456 bytesへ、ZeroOrMoreのOrChildループは296から216 bytesへ変わった。このSequenceのコードは2,935から2,850 bytesへ減ったが、サイズの大小だけで速度を説明しない。
- CharParserのコードは781から618 bytesへ減ったが、`GoFoward`側の`Rune.DecodeFromUtf8`がinlineからcallへ変わったことを含む。CharParserの`sub rsp`は56から72 bytesへ増えた。`Read`側のRune復号は両方inlineされている。
- フィールド保持方法だけを変更して通常計時の差が出たことと、生成コードの変化は整合する。ただし、何個のコピー・初期化・呼び出しの差が何msを占めるかは分離していない。Span一般の性能についての結論でもない。

既存CPU標本の再集計も`analysis/cpu-hotspots-reaggregation.json`に保存した。Actual中の実測スレッド標本は文字列15,366、UTF-8 19,645。既存ETL全体のEventsLostは103,422／853,840あり、標本比率を精密な所要時間と扱わない。新たなCPU traceは採取していない。

型サイズを補助的に調べたcollectible ALCでの`sizeof`実験は無効な結果だったため、サイズの主張や採用根拠から除外した。JITの原本パス・SHA・該当行とこの制約は`jit-evidence.json`に記録している。

## 検証と保存

- 候補のReleaseホストビルドは警告・エラー0、全文解析の検証も成功。
- 既存カーソルテスト24件、Kotowari全227件、JSON全115件が成功。カーソル24件は227件の内数で、全体は342件。テストの追加・削除・変更なし。
- 候補ソースは計測前に凍結し、計測中の本体編集・ビルド・テストを行っていない。
- 計測後の本体差分は採用理由のremarks追加だけ。凍結候補との直接diffでも確認した。公開APIや実行処理に追加差分はない。
- アナライザー有効の全体buildは20.58秒、終了コード1。`WordParserBenchmarks.cs:12`と`WordParserAccumulationBenchmarks.cs:48`がref structの`Cursol<StringDocument>`をクラスフィールドに持つ既存のCS8345で失敗した。SpracheJSON 1.0.1のNU1701も残る。本体、JSON、両テストプロジェクトはビルドできた。
- `dotnet format`の書き換えなし検査は13.01秒、終了コード2。既存のWHITESPACE、IDE0290などとワークスペース読み込み警告が残る。全5プロジェクトのSARIFも読み、今回の`Utf8Document.cs`への診断は0件。範囲外の指摘は無断修正していない。Visual Studio固有の診断まで同一とは主張しない。
- `git diff --check`は成功。
- ステージング、コミット、pushは行っていない。

検証コマンドは次のとおり。先に完了したReleaseテストを、測定後のコメント追記だけのために再実行してはいない。

```powershell
dotnet test Kotowari.Tests/Marimo.Kotowari.Tests.csproj -c Release --no-restore -p:UseSharedCompilation=false --filter FullyQualifiedName~Cursol
dotnet test Kotowari.Tests/Marimo.Kotowari.Tests.csproj -c Release --no-restore --no-build
dotnet test JsonParserのテスト/JsonParserのテスト.csproj -c Release --no-restore -p:UseSharedCompilation=false
dotnet build Kotowari.sln --no-restore --no-incremental -p:UseSharedCompilation=false -p:EnforceCodeStyleInBuild=true -p:RunAnalyzersDuringBuild=true -p:ErrorLog=obj/ide-diagnostics.sarif
dotnet format Kotowari.sln --no-restore --verify-no-changes --severity info
git diff --check
```

証拠のrootは`artifacts/utf8-read-path-2026-10-10/`。開始状態は`baseline/working-tree.json`と`baseline/source/`。候補ディレクトリ`candidate-01-flat-document/`の下に`source/`、`source-freeze.json`、`validation.json`、`diagnostics/utf8-jit-01/jit.asm`を保存した。通常計時の各プロセスのログ、BDN full JSON、生Actual/Result、起動時刻・PIDは`runs/normal/`、独立確認は`runs/confirmation/`へ分離し、それぞれに`aggregate-summary.json`を保存した。全体build・formatの出力はrootの`final-build.log`、`final-format.log`にある。

候補の最初の`host-hashes.json`には相対パスの先頭を誤って切り詰めた記録があった。元ファイルは変更せず、`host-hashes-corrected.json`へ正しい候補ディレクトリ相対パスを保存し、43ファイルのbytes・SHAが元記録と一致することを確認した。計測に使ったホストの絶対パスとMainAssemblyのSHAには誤りがない。`source-freeze.json`の31相対パスには同じ問題はなかった。

Git管理するCSVは追加していない。CSVを含むBenchmarkDotNetの自動出力はGit対象外のartifactsへ分離し、測定した実装の凍結コピーとともに保存している。別入力、特に多バイト文字が多い入力や別ランタイムでの効果は、この全ASCII入力の測定からは保証しない。
