# カーソル引数の`scoped in`化の評価（2026-10-10）

結論：候補ごとの32バイトの引数コピーは減ったが、PGO有効の通常計時で改善の根拠は得られなかった。候補は不採用とし、今回の本体変更14ファイルは作業開始時へ戻した。文字列版は平均160.7→162.3 ms、UTF-8版は204.6→239.0 ms。後者の対応比の95%信頼区間は0.896–1.504と広く、悪化量も断定しない。UTF-8版の最終目標である、導入前文字列版から1割増以内には届いていない。

## 目的と変更範囲

[直前のCPU／JIT調査](2026-10-10-sequence-cpu-jit.md)で、UTF-8版のOr候補処理から子パーサーを呼ぶ際の32バイトのカーソルコピーを確認した。本実験では内部実行器の引数だけを読み取り専用参照へ変更し、そのコピーが減るか、PGO有効の通常計時で改善するかを別々に確認する。

```csharp
internal abstract ParseResult<T, TDocument> ParseCore(
    scoped in Cursol<TDocument> cursol);
```

`ParserExecution`のabstractメソッド、override、ZeroOrMore内のprivate子adapterの計18宣言を変更した。呼び出し側は既存の記述のまま。公開／protected internalのジェネリックParseCore、Parse、Cursol／ParseResult／Documentのレイアウト、パーサーの処理本体、JSON文法、テストは変更していない。引数を変更する目的と借用期間を`ParserExecution`のremarksに追記した。

最初の単なる`in`では、参照引数の保存場所が戻り値へ逃げ得るとしてCS8166／CS8347／CS8352等のコンパイルエラーになった。`scoped in`で引数そのものへの参照が呼び出し外へ逃げないよう制限した。結果には従来どおりカーソルの値と文書の借用入力を渡す。これは仕様追加のRedではなく、参照渡し候補を成立させるためのコンパイラー制約への対応である。

試作で変更し、その後復元した本体ファイル（いずれも`Kotowari/`配下）：

- `ParserExecution.cs`、`Parser.cs`、`WithWhiteSpaceParser.cs`
- `Core/CharParser.cs`、`Core/ConditionalCharParser.cs`、`Core/ExpectCharParser.cs`、`Core/WordParser.cs`
- `Core/DelimitedSequenceParser.cs`、`Core/OptionalParser.cs`、`Core/OrParser.cs`、`Core/ParserConverter.cs`、`Core/RecursiveParser.cs`、`Core/SequenceParser.cs`、`Core/ZeroOrMoreParser.cs`

## 版・入力の固定

作業ブランチは`codex/optimize-word-parser`、HEADは`1422292a983a1213b763acb155c1907ddf9f4387`。HEADだけでは既存の未コミット実装を特定できないため、今回の変更前ソース、候補ソース、ホストのハッシュを別途保存した。開始時の40 dirty pathsの個別ハッシュとindexのハッシュも保存している。今回の実装変更は14ファイルだけであり、既存の未コミット変更を含むファイルをHEADへ戻す操作は行わない。

成果物の共通ルートは`artifacts/cursor-in-parameter-2026-10-10/`。この配下はignoredであり、過去の測定ルートは上書きしていない。

| 条件 | 固定した実装 |
|---|---|
| original-string | 実際のUTF-8導入前`defe7830d2e11b9761f0902e2ad9f0b08acc96b6`、`artifacts/typed-document-2026-10-07/original-string/`の凍結ホスト |
| current-string／current-utf8 | `artifacts/execution-commonization-2026-10-09/after/`の凍結ソース31ファイルとホスト。今回の変更前ソース31ファイルとの一致を確認 |
| candidate-string／candidate-utf8 | 上記から今回の14ファイルだけを変更した`candidate/source/`と`candidate/build/host/` |

| ファイル・識別対象 | SHA-256 |
|---|---|
| originalホストDLL | `CD76D06DED9F00EC6E7BDFB4C59819E25A1A9CDF7899A152A24A137CF0C3393B` |
| currentホストDLL | `F6C61B5574049E521CC156CF1F62875EA66C82486DEF44BA3FE579BAD4D3F7FD` |
| candidateホストDLL | `8944DD21DC26062F6B973F41C842C57DC25BE285ACD0687B9D6B1E905AB705E6` |
| 入力`test.json` | `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D` |
| 候補UTF-8版JIT dump | `BAF3D98EF93633AC1F20A95A8B8A9716A76B5F4742FC602295BDEA84599CB3A4` |

入力は既存の4,172,062バイト、全ASCIIのJSONである。候補ホストの`--validate`は入力全体の消費、100,001ノード、System.Text.Jsonとの結果構造一致を確認して成功した。System.Text.Jsonは検証の基準であり、この実験の性能比較対象ではない。

## 生成コードで確認できたこと

候補UTF-8版について、PGO有効・warmup 16・Actual 32・invocation 4でJIT出力を採取した。終了コード0、Tier 1の厳密なSequence対象は9件。対象ヘッダーはSynthesized PGOであり、PGO有効という設定だけから各メソッドが動的profileを使ったとは断定しない。診断runの所要時間は、以下の通常計時の比較へ混ぜない。

変更前は、Sequenceへインライン化されたOr候補処理内のIG10で、作業用カーソルから子への引数領域へ32バイトをコピーしていた。直前のCPU調査でこの2命令に513標本を捕捉したが、標本割合を削減可能な時間割合とは扱わない。

候補では、該当するSequenceからZeroOrMoreのLoopへ直接呼び出す形になっており、LoopはそのSequenceにはインライン化されていない。コピーが別メソッドへ移っただけではないか、実際のLoop側も確認した。

- LoopのIG03は、入力から更新用の`current`へ32バイトを一度コピーする。これは可変の作業用カーソルの初期化として残っている。
- IG04からIG12へ入り、成功時のIG30もIG12へ戻る。失敗した候補はIG09から次候補へ進む。IG03はこの反復の戻り先ではない。
- 候補のvirtual fallbackであるIG08は、`lea r8,[rsp+0xC8]`で`current`のアドレスを直接渡す。変更前のような、候補ごとの別引数領域への32バイトコピーはない。
- OrChildの転送はOrの`ParseCore(byref)`へのtail jumpであり、その転送にも追加のカーソルコピーはない。

必要な初期化コピー、結果カーソルの取り出しや戻り値の移動は残る。「カーソルコピーをすべてなくした」という変更ではない。また、インライン化の構成も変わっているので、通常計時の差を32バイトコピー一箇所の効果だけには帰属させない。

候補の元listingは`candidate/diagnostics/utf8-pgo/jit.asm`（Sequence `ref,char`は63711行、OrChildは56941行、該当Loopは64519行から）。変更前の対応listingとCPU標本は直前の調査記録を参照する。

## 通常計時

条件は開始前に固定した5条件×4ブロック、計20新規プロセス。奇数ブロックは上表順、偶数は逆順とした。20/20プロセスが終了コード0、すべて8 Actual／8 Resultを取得した。結果を見て試行回数や除外規則を変更していない。通常計時はhost.logの作成・更新時刻で約2026-10-10 04:21:47–04:32:45 UTC（13:21:47–13:32:45 JST）。これは厳密なworker開始終了イベントではなく、PIDもこのrunnerでは保存していない。

Windows 11 25H2（10.0.26200.9457）、Intel Core i7-13700H（14 physical／20 logical cores）、Concurrent Workstation GC。Release、SDK 10.0.401、.NET 10.0.12、BenchmarkDotNet 0.15.8、`DOTNET_TieredPGO=1`、affinity 4、launch 1、warmup 16、Actual 8、invocation 4、unroll 1、DontRemove。診断用JIT出力、WPR、ETW、EventPipeは通常計時では使っていない。同じマシン上で直列に実行し、親のビルド・テストも並行していない。

各operationは同じ入力バイト列を使い、MemoryStreamからのコピー、文字列版のUTF-8デコード、JSON解析と結果構築を含む。ディスクからの入力準備はSetupなので含まない。既存共通ホストのProgram.cs／JsonStreamBenchmarks.csを使用し、候補用の独自文法は作っていない。

独立単位を各プロセスの8 Result（Actualからoverheadを補正した値）のBenchmarkDotNet平均とし、4ブロックで対応を取る。比は同じブロック内の所要時間のlog比の平均を戻した幾何平均。95%信頼区間は4個のlog比からt区間（df=3、t=3.182446305284263）で算出する。小標本であることを明記し、区間に1を含む結果を「同等」や「差がない」とは断定しない。最終目標はcandidate-utf8／original-stringが1.10以内である既定の条件を維持する。

### プロセス単位の結果

単位は1 operationあたりのms。各セルは独立した新規プロセスの平均、右端はその4値の算術平均である。以下の対応比の幾何平均とは集計方法を区別する。

| 条件 | block 1 | block 2 | block 3 | block 4 | 4プロセス平均 |
|---|---:|---:|---:|---:|---:|
| original-string | 148.953 | 144.991 | 142.207 | 145.078 | 145.307 |
| current-string | 156.301 | 154.106 | 157.485 | 174.746 | 160.659 |
| current-utf8 | 195.176 | 219.086 | 206.850 | 197.147 | 204.565 |
| candidate-string | 165.226 | 162.079 | 158.863 | 162.942 | 162.278 |
| candidate-utf8 | 260.143 | 255.237 | 190.798 | 249.889 | 239.017 |

比は小さいほど分子の条件が速い。95%区間は各比較の区間であり、多重比較を調整した同時区間ではない。

| 比較 | 対応比の幾何平均 | 95%信頼区間 |
|---|---:|---:|
| current-string／original-string | 1.1044 | 1.0003–1.2194 |
| current-utf8／current-string | 1.2735 | 1.0917–1.4857 |
| candidate-string／current-string | 1.0113 | 0.9221–1.1090 |
| candidate-utf8／current-utf8 | 1.1608 | 0.8956–1.5045 |
| current-utf8／original-string | 1.4065 | 1.2694–1.5585 |
| candidate-utf8／original-string | 1.6326 | 1.3251–2.0115 |

### 判断

1. 対象の引数コピーは生成コードから減った。しかし文字列版・UTF-8版の候補／現状の区間はいずれも1をまたぎ、高速化の根拠は得られていない。UTF-8版の点推定は約16.1%遅い側だが、「16.1%の悪化が確定した」とも扱わない。
2. 候補UTF-8版のプロセス平均は190.8–260.1 msと幅があり、4ブロック中1回だけ現状UTF-8版より速かった。これを速い状態の発生確率の推定値や、PGOによる二状態の原因特定とは扱わない。PGO依存の変動を縮小できたとはいえない。
3. 導入前の文字列版に対し、現状UTF-8版は対応比1.4065、候補は1.6326。両区間とも1.10より上なので、この条件で既定の最終性能目標は未達である。
4. コピーが減るという局所的な成功だけでは、全体性能の改善とはいえない。インライン化の構成や他のカーソル移動も変わるため、今回の速度差の原因を一箇所へ帰属させていない。「inは一般に遅い」とも一般化しない。追加の当てずっぽうの変更は行わず、候補を不採用にした。

今回の18宣言と追加remarks 3行だけを戻した。候補の凍結ソース・ホスト・JIT出力・20プロセスの測定結果は削除せず保存している。開始時に存在した未コミット実装は維持した。残す変更は本記録とREADMEの索引1行であり、コミット・pushはまだ行っていない。

## 実装の検証

- Releaseのライブラリビルド：成功、警告・エラー0。
- 関連テスト：108成功。全テスト：342成功（本体227、JSON 115）、失敗・Skip 0。テストの追加・削除・変更なし。
- 非incrementalのsolutionアナライザービルド：失敗。既存の`WordParserBenchmarks.cs:12`と`WordParserAccumulationBenchmarks.cs:48`のclassフィールドがref structのCursolを保持するCS8345の2件。両ファイルとCursol.csはHEADに対する差分がなく、今回の実装では変更していない。旧SpracheJSONのNU1701警告も1件残る。本体・JSON・テストプロジェクトはこのビルド中にコンパイルされている。
- `dotnet format --verify-no-changes`：既存テスト／JSONの空白、未変更constructor等の指摘で終了コード2。自動修正は行っていない。
- SARIFの確認：今回変更した引数と追加remarksの行への診断0。既存の別行の診断を解消したとは扱わない。
- `git diff --check`：成功。変更14ソースの既存UTF-8 BOM／CRLFを維持し、bare LF 0。
- 復元後もReleaseビルドを伴う全テストを再実行し、342成功、失敗・Skip 0。`verification/restored-all-tests.log`に保存した。

実際のコマンド、終了コード、ログ先は`verification/parent-verification.json`に保存した。候補ホスト単独のReleaseビルドは警告・エラー0で成功しており、solution全体の既存エラーを隠して計測成功とするものではない。

## 保存物・制約

- 開始前の作業ツリー：`baseline/working-tree.json`、`baseline/source/`、`baseline/source-snapshot-hashes.json`。
- 候補凍結版とホスト：`candidate/candidate-freeze.json`、`candidate/candidate-host-hashes.json`、`candidate/candidate-verification.json`。build用コピーは凍結sourceと別に配置した。
- 診断：`Run-CursorInParameterJitDiagnostic.ps1`と`candidate/diagnostics/utf8-pgo/`。
- 通常計時：`Run-CursorInParameterMeasurements.ps1`、`measurement-readiness.json`、`runs/normal/block-XX/`のhost.log／BDN full JSON／run-summary.json／block-summary.json。各ActualとResultの値も保持する。
- 集計：`runs/normal/aggregate-summary.json`。全20プロセスの平均・生値、環境、4ブロックの6比較の比と区間を保存。

新たなCSVは追加していない。全ASCIIという入力の性質、少数の新規プロセス、別runの生成コードであるという制約を残す。多バイト文字が多い入力の性能、一般の文法すべてに対する効果、実運用での速い／遅いPGO状態の確率を、この実験だけで説明しない。コミット・pushは行っていない。
