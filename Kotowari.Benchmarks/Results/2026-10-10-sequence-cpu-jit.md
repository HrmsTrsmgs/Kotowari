# Sequence実行のCPU標本と同一プロセスのTier 1コード

文字列版とUTF-8版を各1プロセスで実行し、CPU標本の命令アドレスを、そのプロセス自身のTier 1 disassemblyへ対応付けた。Dynamic PGOは有効のまま、ライブラリ本体は変更していない。

UTF-8版では、子パーサーへ渡すカーソルの32バイトのコピー、スタック上の局所領域の初期化、読み取り前後の処理に標本があった。次の実験候補は、内部のカーソル引数を読み取り専用参照渡しにして、反復する引数コピーを減らせるかの確認である。単なるメソッド呼び出しの削減や、読み取り処理の手書きインライン化を根拠にしていない。

これだけでUTF-8版の遅さ全体、文字列版の共通化前後の変動、PGOの速い／遅い状態の原因を説明できたわけではない。以下では確認できた事実と、まだ試していない改善仮説を分ける。

## 対象の版と入力

ブランチは`codex/optimize-word-parser`、HEADは`1422292a983a1213b763acb155c1907ddf9f4387`。先行する未コミット変更を含むため、HEADだけを測定版の識別に使わない。

測定したのは、前回の[Execution共通化後の記録](2026-10-09-execution-commonization.md)と同じ凍結済みの変更後実装である。今回のために別の比較実装を作っていない。

| 対象 | パス／識別情報 |
|---|---|
| 凍結ソース | `artifacts/execution-commonization-2026-10-09/after/source/`、31ファイル |
| ソース一覧 | 同ルートの`after/source-hashes.json`、SHA-256 `94CD81D0845CDBA1F5A6FC8EC1E7071FC1B2F8B7D5ADC9D00B3C8FB66FFDC589` |
| 測定ホスト | 同ルートの`after/host/bin/Release/net10.0/ExecutionCommonization.After.Measurement.dll`、SHA-256 `F6C61B5574049E521CC156CF1F62875EA66C82486DEF44BA3FE579BAD4D3F7FD` |
| ホストのファイル一覧 | 同ルートの`after/host-hashes.json`、SHA-256 `4D183BA934D9AE789FD44DD80BFB8A1670F900600FAD6000B7E51E5388784780` |
| 入力 | 同ルートの`before/input/test.json`、4,172,062 bytes、SHA-256 `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D` |

この入力は全バイトがASCIIである。多バイトUTF-8の復号性能についての結果ではない。凍結版の全入力消費、100,001ノードの生成、System.Text.Jsonとの結果同等性は前回の検証を引き継ぐ。

## 記録条件

- .NET 10.0.12、SDK 10.0.401、BenchmarkDotNet 0.15.8、Release、x64。前回と同じWindows／マシンを使用した。
- `JsonStreamBenchmarks.StringInput`と`Utf8Input`を別々の新規プロセスで、文字列版、UTF-8版の順に直列実行した。重い開発処理を並行させていない。
- Affinity mask 4、WarmupCount 16、ActualCount 32、InvocationCount 4、UnrollFactor 1、LaunchCount 1、OutliersはDontRemove。
- 両経路ともMemoryStreamの生成、CopyTo、バッファ参照、解析と結果生成を含む。文字列版はさらに`Encoding.UTF8.GetString`を含む。ディスク読み込みはSetupで行い、Actualには含めない。
- `DOTNET_TieredPGO=1`。PGO OFFの実行はしていない。
- `DOTNET_JitDisasm=Marimo.Kotowari!*`、`DOTNET_JitDisasmWithCodeBytes=1`、`DOTNET_JitStdOutFile`をrun別の絶対パスに指定した。
- WPRでSampledProfileとCLRのJIT／コード生存期間、BenchmarkDotNetのActual開始・終了イベントを同時記録した。EventPipeは重ねていない。
- 管理者権限を使ったのは専用のWPR制御ヘルパーだけで、測定ホストは通常権限のままである。

これらはCPU/JIT診断の条件である。診断を重ねた計時値を、通常計時の新しい性能比較として採用しない。

新しい記録ルートは`artifacts/sequence-cpu-jit-2026-10-10/`。以降の成果物パスは、特記しない限りこのルートからの相対パスである。

## 捕捉と対応付けの監査

| 項目 | 文字列版 | UTF-8版 |
|---|---:|---:|
| 測定worker PID | 50040 | 42644 |
| Actualの窓数 | 32 | 32 |
| Actual中のmain thread CPU標本 | 15,366 | 19,645 |
| 保存された標本の命令アドレス欠損 | 0 | 0 |
| 1窓あたりの標本数の最小–最大 | 464–505 | 589–700 |
| strict Sequence ExecutionのTier 1コードversion | 9 | 9 |
| 対応するTier 1 listing | 9 | 9 |
| 上記Tier 1への命令対応済み標本 | 1,385 | 3,732 |
| 候補が一意でない／見つからないversion | 0 | 0 |
| トレース全体のEventsLost | 103,422 | 853,840 |

`SequenceParser`の部分文字列検索は`DelimitedSequenceParser`も含めて11件になる。ここでは名前空間の`.SequenceParser`とExecutionを厳密に絞り、Delimitedの2件を除いた9件を対象にした。

対応付けは次の手順で行った。

1. 同じworker PIDのCLR Method/LoadVerboseから開始アドレス、サイズ、Tier、MethodID、ReJITID、load時刻を得た。対応するunload時刻も使い、別のコードversionへのアドレス再利用と区別した。
2. 同じrunのassembly listingと、完全なgeneric宣言型名、`ParseCore`、Tier 1、コードサイズで照合した。型名の表記差は`System.Char`→`char`だけを正規化した。サイズだけで照合していない。
3. 全18件で候補は1件、ETWのサイズとlistingの宣言サイズ、命令バイト列の合計が一致した。元の署名とassembly headerも監査用に保存した。署名そのものの文字列等値は照合キーに使っていない。
4. Actual窓内の標本について、コードversionの生存期間とアドレス範囲を確認し、相対offsetから命令とIGへ割り当てた。IGはJIT dump中の命令グループを指す。

対象Sequenceへ帰属したActual標本はすべてOptimizedTier1だった。QuickJitted／QuickJittedInstrumentedのコードも各9件記録されたが、対象Actual標本の帰属先ではない。この捕捉について「まだTier 0だったため遅い」とは説明しない。

PGOの環境設定はONだが、対象9件ずつのTier 1 headerはすべて`optimized using Synthesized PGO`だった。ONという設定だけを根拠に、各メソッドで実測のDynamic PGO profileが使われたと断定しない。子メソッドのinline profileやPGO全体の動作を、このheaderだけで否定するものでもない。

WPRは両runでイベント欠落の警告を出した。32窓と対象コードの対応は確保できたが、欠落がActualのCPU標本へ及んでいないことまでは証明できない。EventsLostは全プロバイダー合計であり、上表のCPU標本数に対する欠落率へ換算しない。以下はホット箇所の候補を得るための観測で、精密な時間配分や統計的な性能差の推定には使わない。

## 確認できたホット箇所

以下はすべて`SequenceParser<T1,T2>.Execution<TDocument>.ParseCore`のTier 1内の標本である。型は表を短くするため、`System.__Canon`を`ref`、`ValueTuple`を括弧で表記する。

| 入力型・T1・T2 | メソッド内の全標本 | 注目箇所 | 標本 |
|---|---:|---|---:|
| String・char・(ref,char) | 1,083 | IG54: 型guard、子の情報取得、StringDocument.Readの呼び出し周辺 | 471 |
| String・char・(ref,char) | 1,083 | IG52: 候補配列／Execution配列の取得、境界・null確認 | 127 |
| UTF8・char・(ref,char) | 1,907 | IG57: inlined Readの境界確認、入力位置・長さの準備、ASCII判定周辺 | 425 |
| UTF8・char・(ref,char) | 1,907 | IG01: prolog全体 | 203 |
| UTF8・ref・char | 1,124 | IG10: 子へのカーソル引数の32バイトコピー | 513 |

### 読み取りとインライン化

文字列版の最大のSequenceでは`StringDocument.Read`へのcallが残っていた。一方、UTF-8版の最大のSequenceではReadの境界確認やASCII経路がインライン化されていた。

従って、Sequenceのself標本1,385対3,732だけを比較して「Sequenceの連接処理がこの分だけ遅い」とはいえない。UTF-8版では子の読み取り費用の一部までSequence内へ帰属し、文字列版では別メソッドへ帰属するためである。IG57の425件も、すべてをUTF-8復号そのものの費用とは扱わない。

### 初期化を伴うスタック上の局所領域

最大のSequenceの`sub rsp`は文字列版280 bytes、UTF-8版888 bytesだった。IG01では、ゼロを設定したXMMレジスターから局所領域へ書き込むループが生成されていた。

UTF-8版のprolog全203件のうち、そのゼロ書き込み命令上の標本は138件（offset `0x37`が18件、`0x40`が120件）。文字列版ではprolog全59件、ゼロ書き込み命令上は21件だった。prolog全体をすべて初期化費用として数えていない。

局所領域の初期化が実際に残り、そこで標本を捕捉したことは確認できた。ただし、枠のサイズだけで遅さの原因を説明したり、安全性確認なしに初期化を省略する提案へつなげたりしない。

### 子パーサーへ渡すカーソルのコピー

UTF-8版の`T1=ref, T2=char`のIG10は次の2命令だった。offsetは当該Tier 1メソッドの先頭からの値で、元listingは`runs/run-02-utf8/jit.asm`の64629行と64630行。

```asm
; offset 0x1AB: 3 samples
vmovdqu ymm0, ymmword ptr [rsp+0x220]
; offset 0x1B4: 510 samples
vmovdqu ymmword ptr [rsp+0x60], ymm0
```

直後のIG11はコピー先の`[rsp+0x60]`のアドレスを子の`ParserExecution<char,Utf8Document>.ParseCore`へ渡す。前後の候補走査、Execution配列の取得、型guard、fallback呼び出しと既存ソースを照合すると、これはインライン化されたOrの候補処理内にあるカーソル引数のコピーである。外側のSequence固有の連接処理だけを指しているわけではない。

IG10の513件は、このメソッド内1,124件の約45.6%、UTF-8版Actual main thread全19,645件の約2.61%である。これは観測されたIPの割合であり、命令の正確な費用や、そのコピーを消したときの短縮率ではない。割り込みの標本位置と周辺命令の待ち、イベント欠落も区別する必要がある。これ一箇所で既存のUTF-8性能差全体を説明したとはいえない。

virtual callや型guard、配列境界確認もコード内に残っている。ただし、それらの存在だけで支配的な原因とは判定しない。今回、最初の変更仮説に使う直接の根拠は、実際に生成された32バイトコピーと、その周辺への標本の集中である。

## 次の最小実験案（未実装）

内部の`ParserExecution<T,TDocument>.ParseCore`のカーソル引数を、現在の値渡しから`in Cursol<TDocument>`へ変える候補を試す。読み取り専用カーソルをOrの候補ごとにコピーして渡す必要を減らす狙いであり、既存の処理構造を手で展開する変更ではない。

ただし、次の点は実装・検証前に効果を保証できない。

- 内部abstractメソッドと各overrideの整合が必要になる。Orの1行だけで完結する変更ではない。公開Parse／ParseCoreの契約はこの実験の変更対象にしない。
- `result.Cursol`は現在値を返すプロパティなので、単に引数へ`in`を付けても一時コピーが残る場合がある。コピーが別の場所へ移っただけなら成功とは扱わない。
- 借用期間の制約、子から戻る結果との整合、生成コードは試作で確認する。公開型のレイアウト変更、unsafe、初期化省略、全体へのNoInlining指定を先取りしない。
- 既存ソース・入力を固定した比較用の実験版で、問題の引数コピーが減ったかを確認する。その後、既存テストとPGO ONの通常計時で文字列版・UTF-8版の両方を評価する。
- 最終的なUTF-8の目標は、MemoryStreamからの処理全体で実際のUTF-8対応前文字列版に対して1割増を上限とする、既決の基準を維持する。この診断の標本割合で達成扱いにはしない。

初期化や別の読み取り費用を同時に変更しない。まず引数コピーという一つの仮説を検証し、減らせなかった／通常計時で有効でなかった場合は採用しない。

## 成果物と再集計

生のETL、ETLX、JIT dump、診断用コードとJSONはignoredな`artifacts/`に保存し、この記録へ数値と識別情報を集約した。新しいCSVは追加していない。過去の測定記録・生データは上書きしていない。

| 成果物 | パス |
|---|---|
| 準備と凍結版の監査 | `preparation-manifest.json`、`review-hashes.json` |
| 実行条件・PID・終了コード | `capture-summary.json`、`sequence-cpu-jit-helper-result.json` |
| 文字列版CPU記録 | `run-01-string.etl`、同名`.etlx`、`run-01-string.native-cpu-summary.json` |
| UTF-8版CPU記録 | `run-02-utf8.etl`、同名`.etlx`、`run-02-utf8.native-cpu-summary.json` |
| 同じrunのJIT出力 | `runs/run-01-string/jit.asm`、`runs/run-02-utf8/jit.asm` |
| Actual窓・標本IP・コードversionイベント | `run-01-string.sequence-jit.stdout.json`、`run-02-utf8.sequence-jit.stdout.json` |
| 採用した命令対応の集計 | `sequence-jit-map-summary-v4.json` |
| ETLXイベント抽出器 | `analysis/Program.cs`、`analysis/SequenceJitTraceAnalysis.csproj` |
| v4集計を実際に生成したソース | `analysis/Build-SequenceJitMapSummary.v4-source.ps1` |
| 上書き拒否を追加した再集計用ソース | `analysis/Build-SequenceJitMapSummary.ps1` |
| コマンド記録 | `analysis/run-native-cpu-summaries.command.txt`、`analysis/run-sequence-jit-analysis.command.txt`、`analysis/run-sequence-jit-map-audit.command.txt` |

| ファイル | SHA-256 |
|---|---|
| 文字列版`jit.asm` | `F5BCA62BF0B555FB97B5CADCC1D27EEA154620F382B97C003D50AF1661A90CE8` |
| UTF-8版`jit.asm` | `7532E1167118E49B759446283918E21E7AF7AD764EA702DA8B0C919A5499C68B` |
| `sequence-jit-map-summary-v4.json` | `8EC29B27A4FAFD69B2732446ABD8CE72074EC9F1E381612FECAD7786149D865D` |
| `Build-SequenceJitMapSummary.v4-source.ps1` | `4F3DB9581B23297028C9FD016D8AEBC7EA4AA902D9B8FC2C4582D95E99A81E33` |
| 上書き拒否追加後の`Build-SequenceJitMapSummary.ps1` | `D2358E8DF631D9B8AB229FFB89793D883E98541FFF9169BD4C16D6F2A6287895` |

再集計用スクリプトは保存済みJSONとassemblyを読むだけで、WPRや測定を開始しない。実行時は新しい`-OutputPath`とログ先を指定する。既存出力を渡した場合に拒否し、既存v4のハッシュが変わらないことも確認した。

初版`sequence-jit-map-summary.json`は型名の照合不備で候補0となった不採用出力、v2は対象IG10の抽出条件が誤っていた出力、v3は正しい対応だがゼロ標本のprolog命令を省略した出力である。これらも診断経緯として残したが、本記録の集計根拠はv4だけとする。

## 検証結果と未解決事項

- 準備時の診断用イベント抽出器のビルドは成功、警告・エラー0。新しい外部ツールや依存パッケージの導入はしていない。
- 両BenchmarkDotNet、WPR start／stop、ETLX解析、v4集計は終了コード0。専用WPRヘルパーは正常終了し、既定・専用named instanceとも記録中ではないことを確認した。
- 18件の候補一意性、バイト数一致、標本合計、32窓のIP欠損0を確認した。上書き拒否の検証だけは、期待どおり終了コード1である。
- 今回は本体・テストを変更していないため、製品のビルドとテストは再実行していない。前回の342テスト成功を、未実装の参照渡し案の検証として流用しない。
- イベント欠落があり、各条件1プロセスである。速度差、速い状態／遅い状態の確率、コピー削減による改善量、一般の多バイト入力については未確認である。
- 次の実験案は未実装。ライブラリの既存未コミット変更を維持し、この調査記録も未コミットのままである。
