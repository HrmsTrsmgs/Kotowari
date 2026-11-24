# OneOrMoreの連結処理の性能評価 2026-10-10

## 結論

`Prepend`への置換は不採用とし、本体と今回追加した確認テストを変更前へ戻した。約1.08 MB/opの割り当て削減は確認できたが、速度改善は確認できなかった。独立確認8組の候補UTF-8 / 現状UTF-8は1.019807、95%区間は0.997613–1.042494。候補文字列 / 現状文字列の幾何平均は1.052897だった。

電源イベントの留保もあるため、これらを性能同等性や正確な遅化率の証明とはしない。一方、高速化目的でこの変更を採用する根拠もない。配列を省けることと、全体処理が速くなることは別だった。

## 調査から選んだ変更

現在の実装を固定したEventPipe採取で、文字列版では`Enumerable.Concat`と`Concat2Iterator<char>.ToArray`に合計41/405のself標本があった。UTF-8版では2/401だった。ライブラリ内の`Concat`呼び出しは`OneOrMoreParser<T>`の一か所で、最初の結果を一要素配列へ入れて残りの列へ連結していた。

この中間配列を作らず、最初の結果を`Prepend`で付ける候補を評価した。本体の変更は次の一行だけ。`SequenceParser`、`ZeroOrMoreParser`、`ParserConverter`による構成は保ち、パーサーの処理を手動でインライン化しない。

```diff
- tuple => new[] { tuple.Item1 }.Concat(tuple.Item2));
+ tuple => tuple.Item2.Prepend(tuple.Item1));
```

順序、遅延列挙、繰り返し列挙、入力消費、成功・失敗の契約は同じ。配列を省けることはコードから確認できるが、実行時間の改善やUTF-8固有の遅さの解消は、それだけでは判断できない。

## 現行実装のCPU採取

基点は`5e80990ed5f335c88b0c651770a47c58692c72f0`。本体とJSONサンプルの49ファイルを凍結し、文字列版とUTF-8版を別プロセスで直列に採取した。管理者承認を必要とするWPRは使用せず、EventPipeを使用した。

両採取は.NET 10.0.12、SDK 10.0.401、BenchmarkDotNet 0.15.8、PGO有効、affinity mask 4、warmup 16、Actual 32、invocation 4、unroll 1、launch 1、DontRemove。JIT disassemblyは併用していない。両経路とも32組のActual開始・終了イベントと、32件のResultが揃い、EventsLostとstack欠落は0だった。

| selfフレームまたは系列 | 文字列 405標本 | UTF-8 401標本 |
|---|---:|---:|
| Enumerable.Concat | 10 | 2 |
| Concat2Iterator&lt;char&gt;.ToArray | 31 | 0 |
| List&lt;char&gt;.AddWithResize | 44 | 26 |
| SequenceParser系列 | 40 | 68 |
| OrParser系列 | 3 | 13 |
| ZeroOrMoreParser系列 | 1 | 3 |
| StringDocument.ReadまたはUtf8Document.Read | 14 | 0 |

標本はActual区間内の測定スレッドに限定した。EventPipeのmanaged stack標本であり、native命令IP別のCPU時間ではない。inline化、サンプリング、tier移行によってフレームの見え方が変わる。UTF-8版の`ToArray`が0標本でも、使われていないとは言えない。Concat関連の標本割合から10%の短縮を予測したわけでもない。

Actual区間内にも文字列50件、UTF-8 66件のtier compilationイベントがあった。今回のイベント出力にはメソッド別のJIT完了情報がなく、特定のTier 1コード版との対応は確定できない。inclusive標本は親子で重複するので合算しない。採取付きの時間を通常計時へ混ぜない。

## 入力と版の固定

既存の`test.json`は4,172,062 bytes、全ASCII、SHA-256は`DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。入力の縮小・再生成・置換はしていない。

各経路はMemoryStreamからバッファへの読み取り・コピー、文字列版ではUTF-8からstringへの変換、その後の全文解析と結果構築を含む。物理ディスクの読み取りだけはGlobalSetupで済ませて計時外とする。現状・候補とも全入力消費、100,001ノードの生成、System.Text.Jsonとの結果一致を検証した。専用JSONパーサーとの速度比較ではない。

| 対象 | SHA-256 |
|---|---|
| 現状ホスト CurrentUtf8Cpu.Measurement.dll | `8972D0891DFD03FB0CF5E6582132A84FC3EAB00F90F322796A65A58D4B50A35D` |
| 候補ホスト OneOrMorePrepend.Candidate.Measurement.dll | `34EB428C6D5EF801B6245BA9812112D9A6929B559C71489A020F38E278F7B824` |
| 現状 OneOrMoreParser.cs | `993B91E2ADB7CCD5E77017962C36D61DDAA52227793DA9BECB4FAC1041C655A8` |
| 候補 OneOrMoreParser.cs | `148FA8654A5A1D747843CE16A15CE8897AE659BE216E6DEAFF55AB9FC3B9547A` |

現状と候補の本体・JSONソース49ファイルのうち、上記の一ファイルだけが異なる。測定ホストの`Program.cs`と`JsonStreamBenchmarks.cs`は同一ソースを使う。

## 予備測定と電源状態の問題

予備測定は変更前後の文字列・UTF-8を2組、計8回の新規プロセスで実行した。各プロセスはwarmup 16、Actual 8、invocation 4、unroll 1、launch 1、DontRemove、PGO有効、affinity mask 4。診断profilerは付けていない。8回とも終了コード0、ActualとResultが各8件だった。

| 条件 | 組1 ms | 組2 ms |
|---|---:|---:|
| 現状文字列 | 161.160225 | 121.463794 |
| 候補文字列 | 161.047528 | 128.546266 |
| 現状UTF-8 | 168.869397 | 145.717622 |
| 候補UTF-8 | 155.732294 | 152.842288 |

| 時間比 | 組1 | 組2 |
|---|---:|---:|
| 候補文字列 / 現状文字列 | 0.999301 | 1.058309 |
| 候補UTF-8 / 現状UTF-8 | 0.922206 | 1.048894 |

両入力とも改善方向が逆転した。さらに、組1の候補文字列終了13:34:34 UTCから現状UTF-8開始14:56:30 UTCまで約81.93分の空きがあった。SystemログにはModern Standby開始のKernel-Power 506が13:33:30.921 UTC、終了の507が14:56:27.069 UTCに記録されていた。開始時刻は最初の現状文字列プロセスの実行区間に重なる。これだけで各時間差を電源状態のせいと断定できないが、安定した電源状態での比較とは扱わない。

プロセス内も一様ではなかった。組1の現状UTF-8のActualは153.0–202.3 ms、組2の現状UTF-8は143.9–148.4 msだった。ウォームアップ不足やPGOだけを原因と決めつけず、この8起動は採用判断から分離する。2組の記述的な時間比の幾何平均は文字列1.028382、UTF-8 0.983512だが、改善の証拠には使わない。

割り当ては候補で両経路とも約1.08 MB/op少なかった。ただし、割り当て削減だけで所要時間の改善と判定しない。

最初の起動準備ではPowerShell 5.1非対応のProcessStartInfoプロパティと、同名測定csprojの探索衝突で失敗した。Actualが0件の試行は性能値へ含めず、ログを保存した。runnerを対応する起動方法へ直し、候補ホスト名を一意にした後の8起動が上表に該当する。

確認測定ではAC接続とbattery saver無効を確認し、実行中だけ`SetThreadExecutionState`で自動スリープを抑止する。終了時には解除し、恒久的な電源設定変更やaway modeは使わない。このAPIは明示的なスリープ操作を阻止するものではない。[Windows公式仕様](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate)

## 独立確認の条件と評価方法

候補を変えず、5条件を8組、計40回の新規プロセスで比較する。組数と順序は開始前に固定し、途中の結果による延長・打ち切りはしない。予備測定は合算しない。PGO、affinity、反復条件、入力、MemoryStreamを含む処理範囲は上記の通常計時と同じ。

対応前文字列Oには、実際のコミット`defe7830d2e11b9761f0902e2ad9f0b08acc96b6`を保存した`TypedDocument.original.Measurement.dll`を使う。SHA-256は`CD76D06DED9F00EC6E7BDFB4C59819E25A1A9CDF7899A152A24A137CF0C3393B`。比較用に作り直した合成実装や過去の平均値ではない。

| 記号 | 条件 |
|---|---|
| O | UTF-8対応前の文字列版 |
| BS | 変更前の現行文字列版 |
| CS | Prepend候補の文字列版 |
| BU | 変更前の現行UTF-8版 |
| CU | Prepend候補のUTF-8版 |

順序は、`O BS CU CS BU`、その逆順、`BS CS O BU CU`、その逆順、`CS BU BS CU O`、その逆順、`BU CU CS O BS`、その逆順の8組。

主比較は各組の`CU/BU`。8個のlog時間比の平均±`2.3646242510102993 × 標本SD / sqrt(8)`を指数変換し、幾何平均と95% t区間を求める。独立単位は新規プロセスのBDN Result平均とし、プロセス内の8反復を8独立試行とは数えない。これは起動間変動に基づく近似区間で、別入力や別マシンへの保証ではない。

`CS/BS`は文字列版への影響、`CU/O`は最終目標の1.10倍との距離、`BS/O`は現行文字列版の位置を確認する副比較。これらを多重比較調整済みの同時区間とは扱わない。

## 検証

### 独立確認の結果

2026-10-10 15:13:58.468–15:34:35.828 UTCに40起動を完了した。すべて終了コード0、各起動でActual/Resultが8点、各点4操作だった。以下は1操作あたりのResult平均。丸め前の値と反復値は保存先の完全JSONにある。

| 組 | O ms | BS ms | CS ms | BU ms | CU ms |
|---|---:|---:|---:|---:|---:|
| 1 | 165.678466 | 156.404772 | 159.006569 | 188.593806 | 192.420944 |
| 2 | 143.069197 | 150.785625 | 159.037191 | 192.078928 | 186.508778 |
| 3 | 140.432625 | 150.787169 | 156.685225 | 183.015069 | 185.173913 |
| 4 | 138.021638 | 153.349416 | 166.187653 | 181.251263 | 185.950756 |
| 5 | 149.845491 | 148.836144 | 156.767913 | 188.568213 | 189.489497 |
| 6 | 140.085091 | 147.963166 | 156.402150 | 185.615797 | 192.743294 |
| 7 | 144.176678 | 151.870381 | 157.544947 | 180.641456 | 192.151097 |
| 8 | 142.816594 | 148.303250 | 160.614669 | 191.015931 | 195.772528 |

主比較CU/BUのlog比の標本SDは0.02631892、幾何平均1.01980671、95% t区間0.99761280–1.04249438。副比較の幾何平均はCS/BS 1.05289724、CU/O 1.30765737、BS/O 1.03938320。候補は対応前文字列の1.10倍以内という目標にも達していない。

各組の前後でAC接続、battery saver無効、BalancedプランのGUID一致を記録した。実行スレッドの`ES_SYSTEM_REQUIRED`は非ゼロの取得・解除結果を確認し、同一OSスレッドのfinallyで解除した。しかし組1/Oの区間内、15:14:14.396 UTCにKernel-Power 506が記録され、507は測定後16:56:54.577 UTCに記録された。

Modern Standbyには画面消灯後の準備段階も含まれ、公式仕様ではAC電源時のpower requestはNo-CS段階を保持できる。このイベントだけで、40起動のCPUが停止していた、あるいは電源要求が無効だったとは断定できない。実際に40起動は約20分で完了している。ただし画面消灯前後の条件を完全に分離できていないため、電源状態の留保付きの集計として保存し、採用の根拠にはしない。[WindowsのModern Standby準備段階](https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/prepare-software-for-modern-standby)

集計時にはPowerShellのJSON日時自動変換によるUTC区間のずれを修正し、保存済みISO日時から正しいSystemログの区間を再照合した。測定値と元のtimestampは変更していない。

### コード検証

文字列とUTF-8で結果の順序、繰り返し列挙、配列への具体化、残りの入力位置を確認するテストを一件追加した。変更前にOneOrMoreの10件が成功し、変更後に全343件が成功した。新しい振る舞いを追加する変更ではなく、既存契約の確認なので、意図的に実装を壊してRedを作ってはいない。

- Release全体ビルドとアナライザー検査は成功。既知のSpracheJSONのNU1701が一件、エラー0件。
- 全5プロジェクトのSARIFは既存6診断のみ。今回追加した処理・テスト行への新規診断はない。
- `dotnet format`の書き換えなし検査は既存の空白、IDE0290などの指摘で失敗。今回の新規行は対象でなく、無関係な整形・primary constructor化はしていない。
- `git diff --check`は成功。変更C#ファイルのCRLFとBOMを維持した。

実行した主なコマンドは次のとおり。

```powershell
dotnet test Kotowari.Tests/Marimo.Kotowari.Tests.csproj -c Release --no-restore -p:UseSharedCompilation=false --filter FullyQualifiedName~OneOrMoreParser
dotnet test Kotowari.sln -c Release --no-restore -p:UseSharedCompilation=false
dotnet build Kotowari.sln -c Release --no-restore --no-incremental -p:UseSharedCompilation=false -p:EnforceCodeStyleInBuild=true -p:RunAnalyzersDuringBuild=true -p:ErrorLog=obj/ide-diagnostics.sarif
dotnet format Kotowari.sln --no-restore --verify-no-changes --severity info
git diff --check
```

## 証拠の保存先

CPU採取は`artifacts/current-utf8-cpu-2026-10-10/`。凍結ソース一覧はrootの`source-freeze.json`、診断要約は`eventpipe/final-summary.md`、各採取の完全なself・inclusive表は`eventpipe/string-01/cpu-analysis.json`と`eventpipe/utf8-01/cpu-analysis.json`にある。trace、イベント出力、BDN完全JSONも同じ採取ディレクトリに保存した。

変更前後の凍結ソースと通常計時は`artifacts/one-or-more-prepend-2026-10-10/`へ分離した。ソース差分の一覧は`source-differences.json`。Git管理するCSVは追加せず、自動生成されたCSVを含む生ログはGit対象外のartifactsへ保存する。

予備測定は`pilot-summary.json`、独立確認は`runs/confirmation-02/final-summary.json`と`all-runs.json`、各起動の完全BDN JSONに保存した。runnerと集計コードも同じrootにある。起動前失敗のディレクトリを正常40起動へ合算しない。候補の凍結ソースを残しているため、本体を戻しても今回の実装を再現できる。
