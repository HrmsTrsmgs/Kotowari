# Dynamic PGOによる速度差と生成コードの調査（2026-10-05）

## 結論

PGO有効・同一入力・同じ測定設定で、**172.77msの速い起動と約250msの遅い起動を捕捉し、それぞれの計測プロセス自身のTier 1コードを比較できた。**
ライブラリ、JSONパーサー、既存ベンチマークのC#コードは変更していない。PGO無効化を解決策にせず、今回の32起動はすべて `DOTNET_TieredPGO=1`。
各起動前のBDNビルドによりDLLファイルのハッシュは異なるが、32起動すべてで対象2 DLLの全メソッド本体とMVIDを除くメタデータが一致した。ビルド済みDLLそのものを固定した再起動実験ではない点は、残る制約として区別する。

最有力の原因仮説は、**共通入口 `Parser<char>.Parse` に混在する型profileが、インライン先の異なる用途にも使われ、guarded devirtualizationとその後の展開内容が変わること**。
遅い側では `CharParser`、速い側では `OrParser<char>` が共通入口の直接呼び出し先に選ばれていた。
特に文字列の反復を行う `ZeroOrMoreParser<char>` では、遅い側の `CharParser` 用分岐は今回のJSONパーサー構成では使われない。速い側の `OrParser<char>` 用分岐は使える。

これは生成コードで確認した差と、ソース上の呼び出し構成に基づく仮説である。
差分を一つずつ操作する対照実験はまだしておらず、**約81msの差のすべてをこの一箇所に帰属させたわけではない**。
速い起動が1回なので、原因を確定したとも、実運用の正確な発生確率を求めたとも扱わない。

## 起動別の結果

1起動ごとに新規プロセス。warmup 10反復、actual 16反復、各反復4解析。
起動平均が190ms未満・190以上235ms未満・235ms以上という区分を、前回の観測帯を基に整理用に使用した。PGO内部状態の定義ではない。

| 系列 | Run | 起動数 | 190ms未満 | 190〜235ms | 235ms以上 |
|---|---|---:|---:|---:|---:|
| disassembly＋EventPipe | 01〜12 | 12 | 0 | 0 | 12 |
| 診断なし | 13〜20 | 8 | 0 | 1（12.5%） | 7（87.5%） |
| disassemblyのみ | 21〜32 | 12 | 1（8.3%） | 1（8.3%） | 10（83.3%） |

系列の途中で速い結果が出ても打ち切らず、予定数まで記録した。外れ値を削除していない。
診断設定と実行時間帯が異なる系列を一つの母集団として合算しない。採取負荷・少数試行・単一入力の制約がある。
この測定では遅い側が多数なので、改善検証でも遅い側を通常ケースとして含める。最高速の1起動だけを代表値にしない。

| Run | 診断 | 平均 ms | 標準偏差 ms | actualの最小〜最大 ms |
|---|---|---:|---:|---:|
| 23 | disassembly | 253.63 | 8.48 | 243.10〜278.82 |
| 24 | disassembly | **172.77** | 5.86 | 165.15〜186.18 |
| 25 | disassembly | 250.27 | 9.93 | 241.70〜283.44 |
| 30 | disassembly | 213.86 | 11.50 | 205.17〜255.69 |
| 17 | なし | 214.66 | 6.63 | 204.31〜225.99 |

01〜12の平均は259.93 / 257.76 / 269.75 / 264.08 / 264.75 / 265.27 / 264.54 / 268.29 / 268.05 / 285.02 / 238.29 / 274.33ms。
11は一部反復が約219msだが起動平均は238.29ms。速い帯として都合よく再分類していない。
全32起動の平均・標準偏差・warmup/actual各反復は [集約証拠](word-parser/pgo-codegen-evidence.json) と既存CSVに保存した。

## 生成コードで確認できた差

### 共通入口の型選択

`Parser<char>.Parse` のInstrumented Tier0には、`ParseCore` のvirtual callの前に `CORINFO_HELP_CLASSPROFILE32` がある。
同じ閉じたジェネリック型の入口を `CharParser`、`OrParser<char>`、`ExpectCharParser`、`ConditionalCharParser` などが通る。

Tier 1の入口は両側とも32 bytesだが、型ガードの対象と直接tail call先が違う。
以下は実際のdisassemblyから、アドレス・ラベルのみ読みやすく置き換えた抜粋。

```asm
; 遅い23: 型がCharParserなら直接呼ぶ
mov      rax, <CharParserの型アドレス>
cmp      qword ptr [rcx], rax
jne      fallback
tail.jmp [Marimo.Kotowari.Core.CharParser:ParseCore(...)]

; 速い24: 型がOrParser<char>なら直接呼ぶ
mov      rax, <OrParser<char>の型アドレス>
cmp      qword ptr [rcx], rax
jne      fallback
tail.jmp [Marimo.Kotowari.Core.OrParser`1[char]:ParseCore(...)]

; 両側とも、不一致なら既存のvirtual dispatchを使う
fallback:
mov      rax, qword ptr [rcx]
mov      rax, qword ptr [rax+0x40]
tail.jmp [rax+0x20]Marimo.Kotowari.Parser`1[char]:ParseCore(...)
```

disassemblyを取った24起動では、24だけが `OrParser<char>`、残り23起動は `CharParser` を選択していた。
約214msの30も `CharParser` 選択なので、型選択だけで全速度差を説明できるわけではない。

### 呼び出し側へ反映された違い

23と24のTier 1は各256メソッド。アドレス差等を正規化した命令列で37メソッドが異なる。
この正規化は比較対象を絞るためのもので、すべての参照先アドレスの同一性や意味的同値を証明するものではない。
元の命令列・サイズ・比較hashを集約証拠に残した。

| メソッド | 遅い23のbytes | 速い24のbytes | 主な観測 |
|---|---:|---:|---|
| `Parser<char>.Parse` | 32 | 32 | 型ガードと直接tail call先が違う |
| `OrParser<char>.ParseCore` | 363 | 310 | CharParser展開とOrParser展開の違い |
| `ZeroOrMoreParser<char>.ParseCore` | 550 | 365 | CharParser用の使われない枝と、文字列で使えるOrParser用の枝 |
| `ExpectCharParser.ParseCore` | 295 | 286 | インラインされたOrParser内部の呼び出し形状が違う |
| `CharParser.ParseCore` | 134 | 134 | アドレスを除く命令列は同じ |
| `Cursol.Current` | 78 | 78 | 同じbounds checkと例外処理が残る |

`ZeroOrMoreParser<char>` のJITヘッダー上のinline統計は、遅い側が「PGO data 6 / single block 18 / without PGO 3」、速い側が「3 / 12 / 2」。
**速い側で展開量が増えているのではない。** 展開先が呼び出し箇所に合うかどうかが異なる。

現在のJSONサンプルの構成を確認すると、`ZeroOrMoreParser<char>` が繰り返す相手は次の2種類。

- `JSON.JString` の `ZeroOrMoreParser<char>(JChar)`：`JChar` は `OrParser<char>`。
- `JSON.Digits` の `OneOrMoreParser<char>(Digit)` が内部で作るZeroOrMore：`Digit` は `ConditionalCharParser`。

したがって、この構成のZeroOrMoreで `CharParser` と比較するガードは一致しない。
遅い側ではガード不一致からvirtual callへ進む一方、速い側では文字列経路でOrParserのループが展開されている。
このように、少なくとも共通入口由来の型ガードと、この呼び出し位置で実際に渡される型が一致していない。型profileの生の分布そのものは未採取である。

### 指定された観点ごとの確認

- **inline化**：上記の差を確認。EventPipe付きの遅い01では、ZeroOrMoreへの `Parser<char>.Parse` と `CharParser.ParseCore` のinline成功もイベントで確認した。速い24はイベント採取なしで、disassemblyとJITヘッダーによる確認。
- **devirtualization / guard**：共通入口と呼び出し側のclass virtual callで差を確認。今回の主要な観測はinterface callではない。BCL内部のinterface最適化すべてを調べたという意味ではない。
- **分岐配置・値の保持**：遅い23と中間の30は入口の型選択が同じだが、OrParserの成功結果の合流に差がある。23は一旦スタックへ置いた構造体をコピーし、30はフィールドをレジスタで保持して戻り先へ書く形。命令列hashも異なる。他メソッドにも差があるため約40msをこの差だけには帰属させない。
- **bounds check**：`Cursol.Current` は採取24起動すべてでアドレスを除く命令列が同じ。`cmp` / `jae` / `CORINFO_HELP_RNGCHKFAIL` が残る。ZeroOrMoreのList書き込みにも容量チェックが両側で残る。「速い側だけチェックが全部除去された」という結果ではない。
- **OSR**：Kotowari・JSON両assemblyの採取24起動にTier1-OSRのdisassemblyはなく、EventPipe付き12起動の該当メソッドにもOSR tierはなかった。この範囲でOSRを差の原因とする証拠はない。ランタイム全体のOSR不使用を主張するものではない。

## PGOが使うprofileとTier移行時刻

総解析回数だけでウォームアップ不足と判断しない。
EventPipe付き01の時刻はトレース開始からの経過時間で、JIT開始〜コードloadを記録した。

| メソッド | Instrumented Tier0 load | Tier 1 JIT開始〜load |
|---|---:|---:|
| `Parser<char>.Parse` | 3.082秒 | 4.4235〜4.4237秒 |
| `OrParser<char>.ParseCore` | 3.090秒 | 4.4250〜4.4259秒 |
| `ZeroOrMoreParser<char>.ParseCore` | 3.171秒 | 4.5761〜4.5773秒 |

この起動のwarmup 1は1.358〜4.752秒、actual開始は14.497秒。
ホットメソッドのTier 1はwarmup 1中に既に生成され、その後の計測終了まで再生成はなかった。
12起動全体でも、入口のTier 1 loadは約4.32〜5.46秒、ZeroOrMoreは約4.58〜6.73秒で、各ホットメソッドのTier 1生成は1回だった。

.NET 10.0.12の[profile取得実装](https://github.com/dotnet/runtime/blob/v10.0.12/src/coreclr/vm/jitinterface.cpp)は、コンパイル対象やinlineeの既存profileをJITへ渡す。
[JITのprofile処理](https://github.com/dotnet/runtime/blob/v10.0.12/src/coreclr/jit/fgprofile.cpp)もinlineeのprofileを利用する構造になっている。
したがって、全体を200回解析するまで待ってから最適化方針を選ぶのではなく、初期のTier 1生成時点のprofileがコードへ反映される、と今回のイベント・生成コードは整合している。

ただし、生の型ヒストグラム、profile取得命令そのものの時刻、型選択が変わった理由のうちサンプリングとスケジューリングの寄与は採取できていない。
Instrumented Tier0のload時刻〜Tier 1生成時刻は収集可能区間の手掛かりであって、正確な全サンプル時刻の測定ではない。

また、外側の `JSON.Parse`、ベンチマーク入口、`Cursol` constructorは01で約20.29秒、actual 6中にTier 1へ移った。
「全メソッドが計測前に移行済み」とは扱わず、ホットな文字処理と区別した。

| Run | warmup 1〜4のms/解析 | actual平均 |
|---|---|---:|
| 23・遅い | 816.39 / 296.38 / 252.16 / 256.76 | 253.63 |
| 24・速い | 961.73 / 248.91 / 172.53 / 171.34 | 172.77 |
| 30・中間 | 859.11 / 231.30 / 230.88 / 211.79 | 213.86 |

24はwarmup 3から速い帯に入り、actual 16反復も165〜186msだった。
**24にはEventPipeがないため、速い起動のTier移行との厳密な時刻対応は未確認。**
warmupが遅い理由をすべてJITだけに帰属させない。初期化・GC・Tier0実行なども含む。

## 外部負荷と割り当て

測定中は編集・追加ビルド・解析を行わず、直列に実行した。01の採取手順確認後、02〜32で前回と同じ2秒間隔のCPU/プロセスカウンターを記録した。
記録プロセスは論理CPU 16、測定プロセスは論理CPU 2へ固定。バックグラウンドのOS・アプリ活動はゼロではない。

23 / 24 / 25で、測定プロセスのCPU使用量が1コアの80%を超えたサンプルのCPU性能カウンター中央値は124.46 / 118.86 / 125.07。
速い24だけCPU性能カウンターが高かったわけではない。ただしこれは各反復の厳密な実効クロックや温度測定ではなく、外部要因を完全に否定しない。
30には別プロセスの活動もあり、最大255.69msの反復を除外せず保存した。

23 / 24 / 25の割り当ては60,330,814 / 60,331,268 / 60,331,788 bytes/解析で、約60.33MBの同じ規模。
MemoryDiagnoserのGen0/1/2はそれぞれ4500 / 1750 / 500回/1000解析だった。
大きな割り当て削減による差は見えていないが、速い24のGC停止時間は未採取なのでGC要因まで否定しない。

## 最小変更案（未実装）

実装変更はユーザー確認後。PGOは有効のまま、遅い側を含む起動分布で評価する。

1. **因果関係を切り分ける最小試験**：共通 `Parser<T>.Parse` 一箇所だけに `NoInlining` を指定する案。
   共通profileを取り込んだコードが各combinatorへ展開される連鎖を遮断し、起動差が縮むかを確認する。
   PGO自体は無効にしない。呼び出しコストで全体が遅くなる可能性があり、そのまま採用する提案ではない。
   速いケースが少ない今回の結果だけで、属性を製品コードに入れる根拠が十分とはしない。
2. **構造上の改善候補**：公開 `Parse` を維持しつつ、内部combinatorからはvirtualな `ParseCore` を直接呼ぶ。
   例えばZeroOrMoreの呼び出し位置でOrParser/ConditionalCharParserのprofileを取り、共通入口でCharParserが選ばれるprofileへの依存を減らす。
   パース処理の手動インライン化・複製、JSON専用の型分岐を追加する案ではない。
   現状は `ParseCore` がprotectedなので、内部アクセスとoverrideの修飾子を整える必要がある。該当する宣言は15ファイルにあり、論理変更は小さくても機械的な差分は複数ファイルになる。
   公開API・外部派生型の互換性を先に確認し、代表箇所を提示してから展開する。ジェネリック共有や他call siteのprofile依存がすべて消えるとは保証しない。

次の検証では、まずビルド済みDLL一組を固定して複数回起動し、ビルド識別子等の違いを除いた基準を取る。その上で1により仮説を切り分け、結果が支持するなら2を検討する。無条件の `AggressiveInlining` 追加や広範囲の構造変更は提案しない。
採否は、同一入力・PGO有効・複数新規起動の中央値/遅い側/起動間ばらつきと、生成コードの対応で判断する。遅く固定されただけなら改善扱いにしない。

## 条件・再現・保存

- 対象ソース：`a4a37bb92ef852976b0d9a50df07cbc68942c1ea`。本体実装は `8562efc0757e9c936693b3cb2ced3aef1febe511` の遅延配列版。
- 前回と同じ `artifacts/word-parser-json-effect-2026-10-05/snapshots/deferred/` を使用。各起動前にsource-manifestの35ファイルのSHA-256を照合。
- BDNが各起動前にビルドするため、対象DLLは各32起動中25種類のファイルハッシュになった。全32起動について `Marimo.Kotowari.dll` の138メソッド本体、`Marimo.Parser.dll` の60メソッド本体を照合し、各assembly内で完全一致した。ILだけでなくメソッドヘッダー・local signature token・例外領域を含む。メタデータ全体もMVIDの16 bytesを除けば完全一致。したがって実装ILの違いではないが、MVID等が異なる条件を同一バイナリとは呼ばない。
- 入力：既存 `test.json`、4,172,062 bytes、SHA-256 `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。
- Windows 11 10.0.26200.9457、i7-13700H、SDK 10.0.401、.NET 10.0.12、X64 RyuJIT、Concurrent Workstation GC。
- BDN 0.15.8。既存ホストの依存はPidgin 3.5.1、Sprache 2.3.1、Superpower 3.2.1、SpracheJSON 1.0.1。実測対象はKotowariのみ。
- Release / Short / LaunchCount=1 / WarmupCount=10 / IterationCount=16 / InvocationCount=4 / UnrollFactor=1 / Affinity=4 / DontRemove / MemoryDiagnoser。
- 自動Pilotなし。以前の自動Pilotあり測定とは起動過程が同一ではない。今回の同系列内は統一した。
- 全系列で `DOTNET_TieredPGO=1`。OSR、PGOの閾値、インライン化方針等の変更なし。今回PGO OFFは追加測定していない。
- disassembly：`DOTNET_JitDisasm=Marimo.Kotowari!* Marimo.Parser!*` と起動ごとの `DOTNET_JitStdOutFile`。
- EventPipe：`DOTNET_EnableEventPipe=1`、起動ごとの `DOTNET_EventPipeOutputPath`、buffer `40`（16進、64MiB）、config `Microsoft-Windows-DotNETRuntime:1000001019:5,BenchmarkDotNet.EngineEventSource:ffffffffffffffff:5`。
- [retail JITの診断設定](https://github.com/dotnet/runtime/blob/v10.0.12/src/coreclr/jit/jitconfigvalues.h)、[EventPipeの公式設定](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe)、[BDNのEngineイベント](https://github.com/dotnet/BenchmarkDotNet/blob/v0.15.8/src/BenchmarkDotNet/Engines/EngineEventSource.cs)を確認した。`JitDump`等のchecked/debug JIT専用設定をretailで使えたとは報告していない。

同じ固定コピーで、各runに固有の保存先を使う。診断なしは次の形。

```powershell
dotnet artifacts/word-parser-json-effect-2026-10-05/snapshots/deferred/host/bin/Release/net10.0/WordParserJson.deferred.Measurement.dll --filter '*JsonParserBenchmarks.Kotowari' --job Short --launchCount 1 --warmupCount 10 --iterationCount 16 --invocationCount 4 --unrollFactor 1 --affinity 4 --outliers DontRemove --exporters json --artifacts artifacts/reproduction-pgo-on --keepFiles --join --envVars DOTNET_TieredPGO:1
```

診断付きは上記 `--envVars` に診断環境変数を追加する。出力ファイルを各起動で分離し、別プロセスのdisassemblyを計測値へ結びつけない。

実行前の別プロセスで全文消費4,172,062文字、100,001ノード、木のSHA-256 `5ABDB9015EF96B7E79FADB54B636AF0C5AB9D0AA1864C5AF21FF8669C2A4233C` を再確認した。
各計時反復の木をハッシュ化したわけではない。32起動はすべて完了し、各N=16を検査。EventPipe 12ファイルはすべてEventsLost=0で、反復窓とJIT/inlineイベントの対応も検査した。
解析用一時プログラムはローカルの既存依存だけでbuild成功（0 warnings/errors）。最初のbuildはsandboxのNuGet設定読み取り制限で失敗し、通常アクセスで再試行して成功した。
本体・既存テストを変更していないため、全C#テスト・Analyzerは再実行していない。
電源プランはBDN終了後にバランスへ復元されたことを確認。恒久的なランタイム設定変更・依存追加・インストールなし。

保存物：

- 既存 `word-parser/summary.csv` に32行、`iterations.csv` に1,024行、`launches.csv` に32行を `Experiment=2026-10-05-pgo-codegen` として追記。実測512反復であり、Actual/Resultを二重に独立試行として数えない。
- `MeasurementKind` は `pgo-on` / `pgo-on-jit-disasm` / `pgo-on-jit-eventpipe`。Job中のユーザー固有パスを `<repo>` に置換。既存CSVの先頭部分はbytes/hash照合で保持した。
- [pgo-codegen-evidence.json](word-parser/pgo-codegen-evidence.json)：全32起動の平均と反復、4起動×5メソッドの元disassembly、2起動のTier/inlineイベント抜粋、全32起動のassembly同一性照合結果、rawファイルSHA-256を一つに集約。測定ごとにCSVを増やしていない。
- raw JSON・全disassembly・EventPipe・実行/解析補助はGit対象外 `artifacts/pgo-codegen-2026-10-05/`。CPU記録は `artifacts/benchmark-variance-2026-10-05/pgo-codegen-activity.jsonl`。

## 残る確認

速い約173msでのEventPipe同時採取、生の型profile、サンプリング/タイミングの寄与、各コード差の因果的な寄与は未確認。全起動の対象ILは一致したが、DLLファイル自体を固定した再起動条件での確認も残る。
この調査は「生成コード差と具体的な原因仮説を得る」段階までで、ばらつき対策の実装・効果検証を完了したものではない。
