# Execution共通化後のJSON性能評価

転送だけを行うExecution、キャッシュ機構、空白の読み飛ばし、テスト用パーサーを共通化した。全342テストは成功したが、再測定から性能維持を確認できたとはいえない。

初回20起動に同じ実装・指定条件の20起動を追加し、全40起動を残した。合計8組での文字列版の変更後／変更前の時間比は1.089、95%信頼区間は0.969–1.224。点推定は約9%遅いが、改善・悪化のどちらも確定していない。UTF-8版は1.006（0.978–1.035）。「有意な差を検出しなかった」を「同等」と扱わない。

合計8組でのUTF-8版と実際の対応前文字列版との時間比は1.422（1.331–1.519）。MemoryStreamからの読み込みと文字列復号を含めてもUTF-8版の方が遅く、最終速度目標は未達である。文字列版の計時増加の原因はまだ特定していない。初回と追加測定は以下で分けて示す。

## 共通化した処理と維持した契約

- OneOrMoreと3–10要素のSequenceにあった転送Executionを、`ForwardingExecution<T, TDocument>`へ集約した。転送実装は9個から1個になった。
- OrとZeroOrMore独自の`ConditionalWeakTable`を、既存の`ParserExecutionCache`へ統一した。型別の静的factoryと、固定した子の実行器を一度構成して使う方針は維持した。
- WordとWithWhiteSpaceの空白走査を`WhiteSpaceExecution.Skip`へ集約した。走査ループは2個から1個になった。空白パーサーが失敗時に消費した入力は採用しない。
- 8個のテスト用パーサーを`TestParsers.cs`へ集約し、成功値の変換を共有した。外部派生型のフォールバックを検証するため、異なる実際の継承元と呼び出し回数は保持した。

転送Executionは転送元自身の`Definition`を保持する。子のExecutionを直接返すと、Orの候補配列の差し替え判定が転送元と一致しなくなるためである。候補の遅延生成・配列差し替え、外部派生型の呼び出し、ZeroOrMoreの特殊化済み呼び出しも維持した。

各パーサー固有の解析アルゴリズム、外部派生型を区別する型判定、異なる子構成のfactoryは残した。これらは同じ処理の複製ではない。公開API、カーソル、Document、UTF-8復号、JSON文法、依存関係、ベンチマーク本体は変更していない。今回の凍結前との差分は本体7ファイルで+80／-182行、差し引き102行減少した。

## 測定した版

作業ブランチは`codex/optimize-word-parser`、HEADは`1422292a983a1213b763acb155c1907ddf9f4387`。変更前後とも先行する未コミットの実装を含むため、このHEADだけでは測定版を特定できない。以下の凍結ソースとファイル別SHA-256を併用する。

記録ルートは`artifacts/execution-commonization-2026-10-09/`。以下のパスは、特記しない限りこのルートからの相対パスである。

| 条件 | 測定ソース |
|---|---|
| original-string | 実際の文字列版コミット`defe7830d2e11b9761f0902e2ad9f0b08acc96b6`。凍結先は`artifacts/typed-document-2026-10-07/original-string/`。比較用に後から作った合成実装ではない |
| before-string / before-utf8 | `before/source/`の31ファイル。先行するUTF-8 Readの低頻度分岐分離後、今回の共通化前。`artifacts/utf8-read-cold-branch-2026-10-08/after/source/`とハッシュ一致 |
| after-string / after-utf8 | `after/source/`の31ファイル。変更前からの差は上記本体7ファイルだけ |

| 記録ファイル | ファイル自体のSHA-256 |
|---|---|
| `source-hashes.json`（変更前のソース一覧） | `C4158067B00860E1442B7D9B1E666EA77E49805ADA8B0E49844AD9493D9B6250` |
| `after/source-hashes.json`（変更後のソース一覧） | `94CD81D0845CDBA1F5A6FC8EC1E7071FC1B2F8B7D5ADC9D00B3C8FB66FFDC589` |
| `after/host-hashes.json`（変更後の測定ホスト一覧） | `4D183BA934D9AE789FD44DD80BFB8A1670F900600FAD6000B7E51E5388784780` |
| `normal-summary.json`（集計） | `0327ABDD867FEF8D77B6F58AD2915B1D64FABF79569ECF28E93E4C39ADB916FA` |
| `recheck-summary.json`（追加20起動の集計） | `D6B51CE45A5CE118119ACD1C616E5248097593A9DE06A9F6027D1AA6AAA689E2` |
| `combined-summary.json`（合計8組の集計） | `0054C21996C428F057DCDDBA5E6B960268446D397DA229B974E4BCD4A3D6F39D` |

## 入力と測定条件

- 既存の`test.json`、4,172,062 bytes。SHA-256は`DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。
- 全版で入力全体の消費、100,001ノードの生成、System.Text.Jsonとの結果同等性を確認した。専用JSONパーサーの時間を主比較へ加えていない。
- Windows 11、build 10.0.26200.9457、Intel Core i7-13700H（14物理／20論理コア）、x64。電源プランはBalanced。
- .NET 10.0.12、SDK 10.0.401、BenchmarkDotNet 0.15.8、Release。Dynamic PGOは`DOTNET_TieredPGO=1`で有効。
- Affinity mask 4、WarmupCount 16、ActualCount 8、InvocationCount 4、UnrollFactor 1、LaunchCount 1、OutliersはDontRemove。JIT dumpやEventPipeなどの診断を通常計時へ重ねていない。
- 5条件×4ブロック、合計20回の新規プロセスを直列に実行した。奇数ブロックはoriginal-string、before-string、before-utf8、after-string、after-utf8の順、偶数は逆順。重いビルドを並行実行していない。

両経路とも、MemoryStreamの生成、CopyTo、バッファ参照、解析と結果ツリー生成を計時に含めた。文字列版には加えて`Encoding.UTF8.GetString`を含めた。ディスク読み込みはSetupだけで行い、計時に含めていない。

## 初回20起動の通常計時

平均は条件ごとの4プロセス平均の算術平均。CVはその4平均間の標本標準偏差／平均であり、プロセス内反復のCVではない。割り当ては1操作あたりの累積マネージド割り当ての概数で、MBは1,000,000 bytesとする。

| 条件 | 平均 ms | プロセス間CV | 割り当て MB |
|---|---:|---:|---:|
| original-string | 151.396 | 4.10% | 64.503 |
| before-string | 151.954 | 14.90% | 64.503 |
| before-utf8 | 208.859 | 8.28% | 56.159 |
| after-string | 170.322 | 10.42% | 64.504 |
| after-utf8 | 213.202 | 11.78% | 56.159 |

全20起動の平均を以下に示す（ms、表示は小数6桁へ丸めた）。全起動を採用し、速い／遅い起動を除外していない。

| ブロック | original-string | before-string | before-utf8 | after-string | after-utf8 |
|---|---:|---:|---:|---:|---:|
| 1 | 156.149806 | 167.415031 | 234.408309 | 195.361556 | 250.807241 |
| 2 | 156.402322 | 118.546403 | 201.234159 | 169.988719 | 202.345144 |
| 3 | 149.700041 | 164.289913 | 203.409109 | 160.544834 | 198.859072 |
| 4 | 143.330125 | 157.564163 | 196.383581 | 155.390969 | 200.795728 |

同じブロック内のプロセス平均を対応付け、変更後／変更前の比を計算した。時間比が1より大きいほど遅い。

| 比較 | 比の幾何平均 | 95%信頼区間 |
|---|---:|---:|
| after-string / before-string | 1.126893 | 0.846015–1.501022 |
| after-utf8 / before-utf8 | 1.018347 | 0.958903–1.081475 |
| after-utf8 / original-string | 1.402320 | 1.202980–1.634692 |

独立な単位はプロセス平均で、各比較は対応する4組。各プロセスの8反復を独立な8起動として水増ししていない。`l = log(同一ブロックの時間比)`とし、区間は`exp(mean(l) ± 3.182446305284263 × sd(l) / sqrt(4))`で計算した（自由度3）。対数比の正規性などを仮定する小標本のt区間であり、分布が検証済みだという意味ではない。

初回の文字列版は区間が広いため性能同等性の判定が未解決。UTF-8版も変化を明確には検出していないが、無条件の同等性保証ではない。両版で変更前後の割り当て量はほぼ同じだった。この通常計時ではJITコードを捕捉していないため、ばらつきや点推定の差をPGO、ウォームアップ、共通メソッドの呼び出しなどに帰属させない。

## 同じ実装と指定条件による追加20起動

初回の文字列版の計時増加を受け、凍結ソース・入力・指定したジョブ条件を変えずに、5条件×4ブロックを追加した。PGOを無効にせず、同じ順序交替とMemoryStreamからの処理を使った。新しい生データは`runs/recheck/block-01/`から`block-04/`に保存し、初回のデータと`normal-summary.json`は変更していない。

| 条件 | 再測定の平均 ms | プロセス間CV |
|---|---:|---:|
| original-string | 175.024 | 7.02% |
| before-string | 184.822 | 9.22% |
| before-utf8 | 253.394 | 3.26% |
| after-string | 193.930 | 2.36% |
| after-utf8 | 251.753 | 1.70% |

追加20起動の各プロセス平均（ms）:

| ブロック | original-string | before-string | before-utf8 | after-string | after-utf8 |
|---|---:|---:|---:|---:|---:|
| 1 | 180.949381 | 190.749631 | 245.803044 | 189.565528 | 247.058128 |
| 2 | 183.410113 | 196.457913 | 265.049222 | 191.799531 | 253.734297 |
| 3 | 178.938438 | 192.575172 | 250.090213 | 200.187591 | 256.651984 |
| 4 | 156.797544 | 159.505056 | 252.631875 | 194.168591 | 249.568350 |

再測定の比は再測定系列内の同じブロックの条件同士で計算した。統合は初回系列内の4組と再測定系列内の4組、合計8組の対数比を使う。初回と再測定を互いに対応付けた比ではない。統合区間は`exp(mean(l) ± 2.3646242510103 × sd(l) / sqrt(8))`、自由度7。指定した条件は同じだが、絶対時間の測定系列間の変動があるため、系列別の結果を併記する。小標本のt区間の仮定と限界は初回と同様である。

| 比較 | 再測定4組の比 | 95%信頼区間 | 合計8組の比 | 95%信頼区間 |
|---|---:|---:|---:|---:|
| after-string / before-string | 1.052637 | 0.897161–1.235057 | 1.089132 | 0.968944–1.224228 |
| after-utf8 / before-utf8 | 0.993810 | 0.948374–1.041423 | 1.006004 | 0.977848–1.034970 |
| after-utf8 / original-string | 1.441028 | 1.290208–1.609478 | 1.421542 | 1.330557–1.518749 |

初回の文字列版の約13%増という点推定は、追加分では約5%、全体では約9%になった。ただし、統合区間にも最大約22%の悪化が含まれており、性能維持は確認できていない。「再測定で差が消えた」「初回は単なる測定誤差だった」とは結論しない。

## 文字列版の計時増加について確認できたこと

文字列の読み込み、UTF-8からstringへの復号、カーソルの実装は共通化前後で変更していない。今回変わったのは両入力経路で使う転送Executionの型と、空白走査の呼び出し先などである。処理結果を保つ共通化でも、生成コードまで同じだとは限らない。通常計時だけでは確認できなかったコード差を、下記の別プロセスの診断で調べた。

初回の文字列版は4組中2組で増加、2組で減少した。再測定も2組ずつだった。変更していないoriginal-stringも平均151.396 msから175.024 msへ変動したため、絶対時間の増加を共通化だけに帰属させる根拠はない。一方、この事実だけで共通化の影響がないと判断することもできない。

全40起動の記録で、ランタイム・SDK・BDN・PGO・affinityの検査は通過し、電源プランはBalancedだった。監視対象の外部dotnetプロセスでCPU使用時間増分が0.25秒を超えた記録は0件。これは全アプリの負荷、CPU周波数・温度やJIT生成コードの一致を保証する監査ではない。詳細は`recheck-final-audit.json`に保存した。

通常計時の約9%増という点推定の原因は未特定である。今回の変更が文字列版を一定割合だけ遅くしたとも、PGO・ウォームアップ・マシン負荷などが原因だったとも断定しない。下記の診断は生成コードの差を確認するために行い、通常計時の統計には追加していない。原因を決めつけた本体の変更は行っていない。

## 文字列版の生成コードとCPUサンプル

同じ凍結ソース・入力のStringInputを、変更前、変更後、変更後、変更前の順で新規4プロセスへ読み込んだ。PGOは有効のまま、JitDisasmとコードバイト、JITイベント、SampleProfiler、BDNの工程イベントを取得した。ActualCountだけを診断用に32へ増やし、WarmupCount 16、InvocationCount 4など他の指定条件は通常計時と同じにした。診断付きの時間は性能評価にも、速い状態／遅い状態の割合の推定にも使わない。

4プロセスともActual／Resultは32件、計時とトレースのPIDは一致した。EventPipeのEventsLostとSampleProfilerのMissingStacksはともに0。PGO OFFの対照実験は今回追加していない。

| 起動 | PID | 空白処理の対象Tier 1が最後にロードされた時刻 秒 | 本計時開始 秒 | 本計時区間のスタックサンプル数 |
|---|---:|---:|---:|---:|
| before-01 | 43268 | 4.623 | 16.571 | 736 |
| after-01 | 51520 | 4.664 | 16.650 | 702 |
| after-02 | 34780 | 4.849 | 16.730 | 704 |
| before-02 | 39392 | 4.737 | 17.155 | 703 |

時刻は各プロセスのトレースを基準とする。対象はWithWhiteSpaceのchar／参照結果型のParseCoreと、旧SkipBlankAsyncまたは新Skip。これらは4回ともウォームアップ期間中にTier 1になり、本計時中の未最適化状態ではなかった。Marimo名前空間のOSRロードと、JIT出力のTier1-OSRブロックは4回とも0だった。profileの採取範囲やTier 1生成時に使われたprofileの正確なスナップショット時刻は、このトレースでは直接取得していない。

after-01では別の入口8メソッドがActualの3回目にTier 1へ移行した。JSON.Parse、Cursol.FromString、StringDocumentとInputのコンストラクタ、Cursolのコンストラクタ、Parser.Parse、最上位ParserConverter.ParseCoreとExecution.Getである。約18.681–18.686秒のロードを記録したが、この入口の移行が通常計時の差を説明するとは断定しない。固定回数のウォームアップだけでは、全入口の移行完了まで保証できなかった。

### 空白走査の呼び出し境界

| 対象 | 変更前のTier 1 | 変更後のTier 1 |
|---|---|---|
| WithWhiteSpaceの空白走査 | ParseCore内にループがあり、Or.ParseCoreとCharParser.ParseCharacterのinline成功を確認 | 共通のSkipへのcallが残る。char／参照結果型ともinline拒否理由は`unprofitable inline` |
| WithWhiteSpaceのchar結果の子解析 | CharParser.ParseCharacterをinline化 | 型guardはあるがParseCharacterへの直接callが残る。こちらも`unprofitable inline` |
| Wordの前後の空白走査 | SkipBlankAsyncのinline成功1件、拒否1件、残ったhelper call 1箇所 | Skipのinline成功1件、拒否1件、残ったhelper call 1箇所 |

この差は変更前2回、変更後2回それぞれで確認した。変更後のWithWhiteSpace<char>では、Skipの戻り値をスタック上のカーソル用バッファへ受け、子解析へ渡している。入力本文をコピーする変更ではない。共通メソッド化で実際に残った呼び出しと、カーソルを呼び出し境界で受け渡す費用が原因候補になる。ただし、呼び出しが残ったことだけから全体の約9%増を説明したとは扱わない。

Wordでは前後ともhelper callが1箇所であり、空白走査の共通化が一律に新規callを2箇所追加したわけではない。新Skip自体のTier 1には、空白パーサーのOrと、その中のCharParser.ParseCharacterのinline成功がある。空白の子解析まで一律にvirtual dispatchへ戻ったわけでもない。

### 転送と入力読み取り

代表のSequence3<char,参照型,char>と、対応するForwardingExecution<ValueTuple<char,参照型,char>>のTier 1を比較した。変更前後とも、子Converterの型guard、Sequence2の型guardと直接call、変換delegateのguardとinline、各fallbackが残っている。before-01／after-01のスタック領域と保存レジスタも同じで、分岐配置には差があった。転送共通化でdevirtualizationが一律に失われた、あるいはレジスタ退避が増えたという根拠にはならない。全4回のブロック位置・コードバイト数は`key-tier1-blocks.json`に保存した。コードの大きさ自体を速度の根拠には使わない。

StringDocument.ReadのTier 1は4回とも70 bytes。命令の符号化バイトとネイティブアドレスを比較から外すと、bounds check、文字取得、範囲外例外をNULへ変換する経路を含む命令列は一致した。少なくともこの読み取りメソッドのbounds check除去の成否は、今回の前後差ではなかった。他の全メソッドや呼び出し元の機械語が一致するという意味ではない。

StringDocumentはreadonly struct、Utf8Documentはreadonly ref structである。文字列版とUTF-8版が同じ参照型の入力引数としてJITコードを共有した、という説明はこの設計に合わない。結果型に現れる`__Canon`と、入力型のコード共有も混同しない。

JITヘッダーのPGO表記はメソッド別に記録した。StringDocument.ReadにはDynamic PGO、空白処理にはSynthesized PGOの表記があり、マーカーのないTier 1も存在する。これはPGO設定を無効にしたという意味ではない。ヘッダーはJIT内部のprofile重みの由来を表示しており、inline先のprofile情報とも別である（[.NET 10.0.12のJIT出力](https://github.com/dotnet/runtime/blob/v10.0.12/src/coreclr/jit/codegencommon.cpp#L1804)、[profile重みの扱い](https://github.com/dotnet/runtime/blob/v10.0.12/src/coreclr/jit/fgprofile.cpp#L12)）。この表記だけで今回の時間差をPGOへ帰属させない。

### CPUサンプルの範囲

次の値は本計時区間のサンプル数であり、ミリ秒や呼び出し回数ではない。selfはそのメソッドがスタックの末端だったサンプル、inclusiveは同じメソッドを1サンプル中で1回だけ数えた値。参照結果型の`__Canon`を含む個別メソッド単位で集計し、重なるinclusive行を合算しない。

| メソッド | before-01 self／inclusive | before-02 self／inclusive | after-01 self／inclusive | after-02 self／inclusive |
|---|---:|---:|---:|---:|
| Word.SkipBlankAsync | 5／6 | 7／7 | 対象なし | 対象なし |
| WhiteSpaceExecution.Skip | 対象なし | 対象なし | 34／42 | 45／48 |
| WithWhiteSpace<char>.ParseCore | 7／10 | 15／15 | 0／13 | 3／19 |
| WithWhiteSpace<参照結果型>.ParseCore | 34／143 | 34／135 | 8／122 | 5／119 |
| Word.ParseCore | 51／78 | 45／67 | 44／61 | 46／68 |

新Skipのselfは34/702と45/704、約4.8%と6.4%だった。ただし、変更前は同じ走査の仕事が呼び出し元へ展開されていた。これを「新しい呼び出し費用だけで4.8–6.4%増えた」と解釈しない。数百サンプルの探索的な診断で、メソッドの区切りも変わったため、約9%増への寄与率を確定する精度や対照実験はない。

### 最小限の対策候補

最初の候補は、空白走査の共通化を維持し、`WhiteSpaceExecution.Skip`だけに`MethodImplOptions.AggressiveInlining`を付けた実験版を比較することである。今回実際に拒否された境界を対象にするもので、走査コードを各パーサーへ手作業で再展開する案ではない。属性は速度もinline成功も保証しないため、まだ本体へ実装・採用していない。

まずPGO ONの生成コードでSkipへのcallとカーソルの受け渡しがどう変わるかを確認する。仮説どおり変わった場合だけ、診断なしの通常計時で前後の対応比と信頼区間を比較し、UTF-8版への影響も別に確認する。呼び出し境界が変わっても全体差が改善しなければ、この候補を約9%増の解決策とは扱わない。根拠なくCharParserなどへ属性を広げたり、転送型・キャッシュを分割し直したりする案は現時点では採らない。

測定条件の対策は別に扱う。Actualに残った入口のTier移行が問題になるなら、予備解析の回数を調整して工程イベントで移行完了を確認する。ただし、すでにTier 1になっている空白処理のinline拒否を、ウォームアップ延長で解消できる根拠はない。速い起動だけを選別せず、PGOを有効にした通常計時で評価する方針は維持する。

今回の診断は通常計時の速い／遅い起動そのものから取得したトレースではない。したがって、過去の各起動のJITコード差、速い状態になる割合、profile採取時点の因果、CPU周波数・温度などの外因は未解決である。4起動で切り上げ、本体・公開API・テストは変更していない。

診断データは`diagnostics/string-cause/`へ集約した。`diagnostic-plan.json`、`run-index.json`、各起動の生JIT／nettrace／イベント・CPU集計、`diagnostic-summary.json`、`key-tier1-blocks.json`を対応付けた。抽出条件とinclusive集計の訂正は`analysis-corrections.json`に記録し、生データは採り直していない。最終summaryのSHA-256は`FA777AD1083EC5A7FE96465D6BD344873E6628C76D8582C3E0B9576C4AD9A558`。入力、凍結ソース、主ホスト、Git indexのハッシュは診断前後で維持された。作業ツリーの本体31ファイルと先行する本体・テスト16ファイルも凍結後のハッシュと一致し、ステージングは0。診断後のdotnet残留はなく、電源プランはBalancedへ復帰した。

## Skipのinline指定を試した隔離実験

上記の候補を、本体へ適用せず`diagnostics/skip-inline/candidate/`で試した。対照は空白処理を共通化した後の凍結版である。31ソースファイル中、差分は`WhiteSpaceExecution.Skip`に付けた`[MethodImpl(MethodImplOptions.AggressiveInlining)]`の1行だけ。走査の手作業による再展開、他メソッドへの属性追加、公開API・仕様・テスト内容の変更は行っていない。

候補のソースmanifestのSHA-256は`CEE7032E3B44E9DE629C23D3FAF06BB59C12BAAE9F38A7F66B5CA03A90B5CE40`、ホストDLLは`9365B6CF0F5AE672B99415AAA7A67AF101681415CD11861087833F41F155ED05`。変更したParserExecution.csは`14F267BEA565CA0118A6D53FC016E3F3C9436583C4C152020BDB543618996283`。入力、SDK、ランタイム、BDNは前節と同じで、PGOは有効のままである。

### 候補版の検証

既存テスト20ファイルを隔離コピーし、コピーしたcsprojのProjectReferenceだけを候補ライブラリへ向けた。既存テストをそのまま本体に対して実行して候補を検証したことにはしていない。候補ホストと両テストプロジェクトのRelease・アナライザー付きビルドは警告・エラー0。Kotowari 227件、JSON 115件、合計342件が成功し、失敗・Skipは0件だった。共有validatorで文字列／UTF-8の全入力消費、100,001ノード、System.Text.Jsonとの結果一致も確認した。復元は既存のNuGetキャッシュを使い、新規パッケージの導入はしていない。

### 生成コードの比較

候補のStringInputを新規2プロセスで診断し、対照の`diagnostics/string-cause/codegen/after-01`と`after-02`をコード比較に使用した。対照の古い時間と候補の診断時間を性能比較には使わない。両候補とも32 Actual／32 Result、計時とトレースのPID一致、EventsLost 0、Tier1-OSRブロック0だった。

| 起動 | PID | WithWhiteSpace<char> Tier 1ロード 秒 | WithWhiteSpace<参照結果型> Tier 1ロード 秒 | 本計時開始 秒 | 本計時スタックサンプル数 |
|---|---:|---:|---:|---:|---:|
| candidate-01 | 47004 | 4.576 | 4.748 | 16.704 | 727 |
| candidate-02 | 3120 | 4.517 | 4.631 | 16.521 | 699 |

対象はどちらもウォームアップ中にTier 1になっていた。次のcall数は対象Tier 1内の静的なcall配置数であり、実行回数ではない。

| 対象 | 対照2起動 | 候補2起動 |
|---|---|---|
| WithWhiteSpace<char> | Skip call 1、子ParseCharacter call 1 | Skip call 0、空白Or call 2、子ParseCharacter call 1 |
| WithWhiteSpace<参照結果型> | Skip call 1 | Skip call 0、空白Or call 2 |
| Word | Skip call 1。前後の空白処理の一方だけinline成功 | Skip／空白Or／ParseCharacterのcall 0。前後ともinline成功 |

WithWhiteSpaceからSkipへのinline成功は両候補のイベントでも確認した。ただし、その展開先のOrは`unprofitable inline`で拒否され、型guard付きの直接callとvirtual fallbackが残った。対照のstandalone Skip内ではOrとCharがinline化されていた。char結果の子ParseCharacterも候補で引き続き`unprofitable inline`だった。したがって、Skipへのcall除去は確認できても、空白解析全体の呼び出し費用が減ったとはまだ言えない。2つのOr call配置は異なるループ経路にあり、呼び出し回数が2倍になったという意味ではない。

候補のchar結果の呼び出し元では、Skipの戻り値用バッファは消えたが、OrのParseResult用バッファとカーソルの受け渡しは残る。保存レジスタは2個から6個、`sub rsp`は56から72へ変わった。この呼び出し元だけの命令差を、呼び出し先も含めた総費用の増減とは扱わない。コードバイト数も速度の根拠には使わない。

CPUサンプルでは候補のSkipは観測されず、Wordのself／inclusiveは60／86と57／76、WithWhiteSpace<参照結果型>は29／141と29／131だった。inline化で仕事の帰属先が変わるため、対照との差を費用の削減率として解釈しない。inclusiveは前節同様、個別メソッドごとのサンプル内重複除去で、重なる行は合算しない。

生成コードの変化が確認できたため、同じ対照と候補を下記の診断なしの通常計時で比較した。属性を付ければ空白処理全体が有利になる、という仮説は支持されなかった。本体への採用・コミットは行っていない。

診断データは`diagnostics/skip-inline/`の`diagnostic-summary.json`、各起動のJIT／nettrace／イベント・CPU集計、候補の`source-delta.patch`と検証ログへ集約した。candidate-01の生成Jobは候補ホストを絶対ProjectReferenceで参照していたが、検査側の旧パス結合が誤ってfalseを返した。生記録を上書きせず、正しい参照先とSHAを`job-reference-verification-correction.json`へ別記している。候補01は採り直していない。測定前の準備エラーも別記し、失敗を成功扱いしたり、速い起動だけを残したりしていない。

### 診断なしの通常計時

PGO有効、同じMemoryStreamからの読み込み開始、同じ4,172,062-byte入力で、現状の共通化版と候補を比較した。ディスク読み込みは計時外で、文字列経路のUTF-8からstringへの復号は計時に含む。WarmupCount 16、ActualCount 8、InvocationCount 4、UnrollFactor 1、LaunchCount 1、affinity 4、DontRemoveを使用。JitDisasm／EventPipe／SampleProfilerは有効にしていない。

文字列の現状、文字列の候補、UTF-8の現状、UTF-8の候補を1組とし、偶数組は全体の順序を逆転させた。各条件は新規プロセスで起動し、8組・計32起動を直列に実行した。途中の時間確認に対して、ユーザーが予定の8組までの続行を選択した。結果による標本数の変更、速い／遅い起動の除外や採り直しはしていない。次の値は各プロセスの8反復から得た平均msである。

| 組 | 現状 string | 候補 string | 現状 UTF-8 | 候補 UTF-8 |
|---|---:|---:|---:|---:|
| 1 | 135.214 | 170.054 | 209.114 | 241.043 |
| 2 | 172.859 | 157.369 | 196.906 | 220.332 |
| 3 | 155.093 | 166.082 | 200.996 | 233.815 |
| 4 | 156.740 | 156.612 | 200.893 | 207.702 |
| 5 | 159.470 | 183.086 | 197.156 | 223.083 |
| 6 | 174.625 | 164.626 | 197.974 | 214.034 |
| 7 | 176.448 | 165.373 | 197.907 | 218.750 |
| 8 | 155.949 | 160.433 | 207.209 | 216.396 |

| 条件 | 8プロセス平均 ms | プロセス間CV % | プロセス内CVの平均 % | 割当量平均 bytes/op |
|---|---:|---:|---:|---:|
| 現状 string | 160.800 | 8.509 | 5.232 | 64,503,001 |
| 候補 string | 165.454 | 5.118 | 6.378 | 64,564,929 |
| 現状 UTF-8 | 201.019 | 2.338 | 5.463 | 56,158,634 |
| 候補 UTF-8 | 221.894 | 4.866 | 5.977 | 56,158,397 |

独立単位は1プロセスの平均であり、プロセス内の8反復を独立標本にしない。同じ組・同じ入力の`candidate/control`の対数比を8個取り、対数比の標本標準偏差とdf 7のt値2.3646242510103で95%信頼区間を求めた。対数比の平均と区間を指数変換した結果が下表で、単純な平均時間同士の比とは別である。診断時間、以前のnormal／recheck／combined系列は混ぜていない。

| 比較 | 対応比の幾何平均 | 95%信頼区間 | 評価 |
|---|---:|---:|---|
| 候補 string / 現状 string | 1.031186 | 0.939888–1.131353 | 約3.1%増の点推定。約6.0%改善から13.1%悪化までを含み、改善も同等性も確認できない |
| 候補 UTF-8 / 現状 UTF-8 | 1.102983 | 1.063645–1.143776 | 約10.3%悪化。区間は約6.4–14.4%悪化側にあり、この条件での悪化を支持する |

この単独属性の候補は見送る。Skipのcallを消すことはできたが、内側のOrまで有利に展開されるわけではなく、全体の改善にはつながらなかった。文字列のプロセス間CVが小さくなった点だけを「PGOのばらつきを改善した」と評価しない。文字列のプロセス内CVとUTF-8のCVはむしろ増えており、速度を保った安定化は確認できていない。

元の共通化前後の文字列版の約9%増について、原因が確定したわけではない。今回の介入はSkip境界だけでなく内側のinline判断も変えているため、これをSkip呼び出し費用だけの因果実験とは扱わない。生成コードから確認できたのは、同じ共通の走査でも呼び出し元によってinline配置が異なることである。次に調べるなら、WithWhiteSpaceでのOrのinline拒否とその内訳を対象にする案があるが、根拠なく属性を他メソッドへ広げる実装は行わない。

候補stringの8組目は割当量64,997,272 bytes/opで、他の候補stringより多かった。この割当量差の原因は未特定で、平均から除外していない。メモリ削減の効果があったとは主張しない。また、今回の対照は共通化後の現状版であり、真の旧文字列版original-stringは追加測定していない。UTF-8を旧文字列版以下の時間にする最終目標が達成されたという証拠にはならない。

全32起動は終了コード0で、各8 Actual／8 Resultが揃った。通常計時のsetup failure、生成Jobの参照系譜不整合、電源復元漏れ、監視対象の外部dotnet CPU使用時間の閾値超過は0件。PowerはBalancedへ復帰し、残留dotnetは0だった。入力、候補31ソース、対照・候補ホストを照合した。親でも作業ツリーの本体31ファイルと既存テスト20ファイルが実験前のハッシュと一致することを確認し、Git indexのSHAも維持された。過去3系列のsummaryは変更していない。

通常計時は`diagnostics/skip-inline/runs/normal/block-01/`から`block-08/`へ集約し、既存の記録以外に起動別CSVをGit対象へ追加していない。集計`diagnostics/skip-inline/normal-summary.json`のSHA-256は`02F1662A7F96F76EF6E8EB5343D53B842B6D99A935DFE5882C16EE9C3E7D17F1`。候補の検証失敗から再試行成功までの区別は`validation-summary.json`に保存した。計画メモの説明用ホスト相対パスと出力範囲は終了後に訂正し、旧メモは`normal-plan-before-final-record.json`として保持した。実コマンドと生成Jobは最初から正しい凍結ホストを参照しており、計時結果は変更・再測定していない。本体コード、公開API、既存テストには今回の候補を適用せず、コミット・pushも行っていない。

## 現行の文字列版とUTF-8版のボトルネック診断

共通化後の現行ソースを変更せず、文字列版とUTF-8版を各2プロセスで診断した。対象は`after/source/`の31ファイルで、見送ったSkipのAggressiveInlining候補ではない。目的はUTF-8経路に残る追加処理を絞ることであり、共通化前後の文字列版の約9%増という点推定の原因調査とは区別する。

### 条件と捕捉結果

SDK 10.0.401、.NET 10.0.12、BenchmarkDotNet 0.15.8、Release、PGO有効、affinity mask 4。WarmupCount 16、ActualCount 32、InvocationCount 4、UnrollFactor 1、LaunchCount 1、DontRemoveを使用した。JitDisasm、Runtime EventPipe、SampleProfiler、BDNの工程イベントを同時に捕捉した診断であり、この実行時間を通常計時の速度比較へ混ぜない。

入力は従来と同じ4,172,062 bytes、100,001ノードのJSONで、SHA-256は`DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。全バイトを確認すると0x80以上は0個で、ASCIIだけだった。したがって、この入力の差を「多バイト文字の復号が重い」で説明することはできない。

測定対象は共通ホストのMemoryStreamからの読み込み、解析、結果木生成まで。ディスク読み込みはsetupで行い計時外とし、文字列版のUTF-8からstringへの復号は対象に含む。文法・検証処理・入力・ホストを揃え、新規プロセスを次の順で直列起動した。

| 起動 | PID | Actual／Result | スタック標本数 | Managed | External |
|---|---:|---:|---:|---:|---:|
| string-01 | 43132 | 32／32 | 718 | 565 | 153 |
| utf8-01 | 19692 | 32／32 | 841 | 741 | 100 |
| utf8-02 | 19784 | 32／32 | 809 | 727 | 82 |
| string-02 | 35988 | 32／32 | 701 | 564 | 137 |

4起動とも終了コード0、workloadとtraceのPID一致、EventsLostとMissingStacksは0。捕捉したKotowariのJITブロックにTier1 OSRはなかった。電源はBalancedへ復帰し、監視対象の外部dotnet CPU使用時間の閾値超過と残留dotnetプロセスは0だった。

### サンプルと生成コードから分かったこと

次は対象メソッドに帰属したself標本数の抜粋で、総CPU時間や追加費用の割合ではない。Sequenceの型は`SequenceParser<char, ValueTuple<__Canon, char>>`、ExpectCharとWordは各文書型のExecutionを指す。

| メソッド | string-01 | string-02 | utf8-01 | utf8-02 |
|---|---:|---:|---:|---:|
| 上記SequenceのParseCore | 64 | 63 | 111 | 111 |
| WhiteSpaceExecution.Skip | 41 | 32 | 59 | 60 |
| ExpectCharのParseCore | 4 | 7 | 28 | 38 |
| WordのParseCore | 34 | 43 | 52 | 54 |

既存の解析器のselfはTraceEventが返す最上位managed stack frameで、SampleProfilerのTypeによる除外はしていない。Externalを含むため、上表を排他的なCPU占有率として読まない。また、inline化によって同じ処理の帰属先が変わるため、文字列版との差をそのメソッド自身の追加費用と見なすこともできない。PollGCの標本数もGC時間そのものではない。

各標本の時刻とアドレス範囲からJITコードの版を対応させ、UTF-8のSequence、Or、ExpectChar、Skip、WithWhiteSpaceについて878標本を241のコード版・IG領域へ整理した。アドレスはcall stack由来で、イベントに別の実行IPがあるわけではない。生の実行IPかreturn addressの表現かは既存解析器では確認できていない。結果確認の分岐や呼び出し直後にも標本が集まるため、「このcmp／コピー／virtual callが高価」と個別命令の費用を断定しない。

ソースと現在のTier 1生成コードを併せて確認すると、次の処理は実際に残っていた。

- `Current`は`Utf8Document.Read(Index)`で文字を読み、成功時の`GoFoward(1)`は同じ位置から別途文字幅を求める。どちらも`Rune.DecodeFromUtf8`を利用する。
- 今回のASCII経路でも、同じ位置の読み取りと前進のために境界確認、Spanの範囲・アドレス計算、先頭バイト読み取り、ASCII判定が別々に展開される。ExpectCharのTier 1などで確認でき、コンパイラが一つにまとめているわけではなかった。
- Read、GoFoward、文字幅計算にはTier 1への移行とinline成功が記録され、Runeのinline成功もある。一律に「inlineされていないから遅い」とは説明できない。一方、呼び出し元や展開量によるinline拒否もあり、全箇所で成功する保証ではない。
- UTF-8のSequenceやSkipの対応領域には、上記のASCII読み取り処理が含まれる。多バイト処理の除去やASCII特別処理の追加を、今回の主要候補にはしない。

これは重複処理の存在と調査対象を裏付けるが、その処理だけで旧文字列版に対する約42%の時間差を説明できたわけではない。本診断では旧文字列版も共通化前版も起動しておらず、以前の通常計時の比や信頼区間を更新しない。

### 次に検証する対策案

削減候補は、一文字の読み取りで「文字」と「次の位置」を同時に得て、成功時の前進で文字幅の計算をやり直さない形である。メソッド呼び出しを手でinline化する案ではなく、現在の生成コードにも残る処理自体を減らす案になる。ただし、実装・通常計時はまだ行っておらず、改善量も最終目標の達成も未確認である。重複の存在を根拠に先に第一候補としたが、時間の集中先から選んだ優先順位ではないため、採用や先行実装の根拠にはしない。

現在の`IDocument`はpublicで、ReadとGoFowardを別々の契約として公開している。したがって、この案をそのままAPIへ追加・置換することは内部だけの変更ではない。実装前に契約と影響範囲を確認する。失敗時のカーソル、入力終端・不正UTF-8・補助平面文字の扱い、既存派生パーサーへのフォールバックを維持する必要がある。借用入力の変更をCurrentが読み直す既存の振る舞いも維持し、カーソルへ文字や幅を永続キャッシュしない。特にユーザーの述語・コールバックの前後で借用入力が変更される場合は、読み取り結果の再利用で振る舞いを変えないか確認する。

OneOrMoreの先頭配列とConcat、反復結果のListなどの中間データは別の共通費用の候補である。今回の表やoffsetだけでUTF-8固有の差、または共通化前後の文字列版の差の原因とは結び付けない。これらも今回の本体へは適用していない。

### 記録と保全

今回の成果物は`artifacts/execution-commonization-2026-10-09/diagnostics/utf8-bottleneck/`に集約した。計画は`diagnostic-plan.json`、起動の索引は`run-index.json`、実コマンド・環境は各`codegen/<condition>/process-record.json`、raw traceと全JIT出力も同じ起動別ディレクトリに保存した。

- `diagnostic-summary.json`: `416E9AB124417E084E92CC4583C2A43327160EB1E621168FB328007543AB6C07`
- `sample-kind-summary.json`: `57535F404EAAC55608437B74D195036F89981B38CB066670B57BD49A7EDC39C3`
- `utf8-native-offsets.json`: `6CF08490E90E6616C9A32E459076C6975D06ED0DEC9F49BC0399684FE26FAF44`

元のCPU集計でType未フィルタとアドレスの意味が説明不足だったため、上記の制限を説明メタデータへ追記し、訂正前コピーを保持した。offset明細の空フィールドは集約時のフィールド名不一致だったため、別成果物`utf8-native-offsets.json`へ明細を保存して参照を付けた。raw trace、元のアドレスmap、process recordは書き換えていない。

設定ハッシュにも説明の訂正がある。起動別recordにコピーされていた`FAA64F41B7B40113BBB5A3433B2C12EEB8912A5A7C33B7407594D12270F77D7F`は以前の通常計時用`measurement-settings.json`ファイルのハッシュで、新診断のSettingsオブジェクトのハッシュではなかった。新診断Settingsのcompact UTF-8 JSONのSHA-256は`FC8142710B581EFCB7BB371543E49D3DB307EDC31A4608EF180CE5FCC082FEA5`。訂正経緯と元の値は`settings-hash-correction.json`に残し、実コマンド・実設定やraw記録は変更していない。

本体31ファイルは凍結ソースと一致し、ホスト・入力・Git indexも一致した。本体・公開API・既存テストの変更、新しい通常計時、コミット、pushは行っていない。変更したGit対象文書はこの測定記録だけで、過去の値は維持した。新しい起動別CSVはGit対象へ追加していない。以下の342件成功などは共通化時の結果であり、この診断でテストを再実行した結果ではない。

### CPU時間の採取方法の訂正と権限の確認

上記のSampleProfiler標本は、CPU上で実行中の時間だけを採る標本ではなかった。Microsoftの説明では、旧`cpu-sampling`プロファイルはCPU使用に関係なく全スレッドを採る方式で、誤解を招く名称のため現在は`dotnet-sampled-thread-time`になっている。Managedだけへ絞っても実CPU時間の標本になるわけではない。[dotnet-traceの公式説明](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace#dotnet-trace-collect)

さらに、この方式にはsafe-point biasとmanaged-onlyのスタックという制限がある。したがって、先のself標本数は呼び出し経路の調査には使えても、復号、dispatch、コピー、結果生成、GCの排他的CPU費用を示すものではない。二重読み取りが全体時間を支配するボトルネックと特定できた、とは扱わない。[高CPU使用率調査の公式説明](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/debug-highcpu)

実行中CPUの採取へ切り替えるため、インストール済みWindows Performance Recorderを調べた。`wpr -profiledetails CPU`ではSampledProfileのkernel providerとstack captureを確認できた。しかし、既存のrecordingがないことを確認して`wpr -start CPU -filemode`を一度試すと、終了コード`-984068079`、`0xC5585011`、`Failed to enable the policy to profile system performance`で開始に失敗した。自分のstartは成功していないためstop／cancelは行っておらず、測定workloadも新たに起動していない。

Visual Studio 18 Communityなど既存インストール先も確認したが、代替の採取用`VSDiagnostics.exe`、CPUUsage設定、VSPerf collectorは見つからなかった。VSPerfReportは存在したが解析用であり、採取ツールの代替にはならない。

現在のWindows実行権限では採取を開始できなかったため、ここからのon-CPU測定には限定的な管理者昇格が必要になる。昇格したshell、UAC、新しいツールのインストール、サービス・registry・権限設定の変更は行っていない。既存4プロセスの結果は保持するが、ボトルネック測定が完了した結果としては報告しない。本体・テスト・公開APIとGit indexは変更していない。

実コマンド、エラー、ツール確認、source 31ファイル・index・電源・非記録状態の最終監査は`diagnostics/utf8-bottleneck/cpu-attribution-reanalysis/inventory-and-access-probe.json`に保存した。SHA-256は`7273B2E123889944F123869AD4496887505FC4D321E2AA91CB3153D9A0AEBF11`。既存traceの再集計スクリプトは作成途中で止め、実行していない。新しいCPU時間内訳や再集計結果は得られていない。

### 限定昇格によるon-CPU採取の準備と開始失敗

上記の非昇格での失敗を受け、WPRの開始・終了だけを管理者権限で行うヘルパーを準備した。ベンチマークは通常権限の別プロセスで実行する。採取対象はkernelのSampledProfileとそのスタック、プロセス・module情報、CLRのメソッド情報、BenchmarkDotNetの測定区間マーカーに絞った。新しいツールのインストールや永続的なOS権限・サービス・registry設定の変更は行っていない。

ヘルパーは固定名のWPR sessionだけを操作し、既存sessionの停止・cancelは行わない。全体15分、各採取120秒を上限とし、終了時には自分で開始に成功したsessionだけを停止する。最小WPRプロファイルは`wpr -profiledetails`で受理され、ヘルパーと通常権限runnerのPowerShell構文解析も成功した。これは採取成功の検証ではない。

PGO有効、凍結済みの同じ4,172,062 byte入力、MemoryStreamからの読み込み込みで、文字列・UTF-8・UTF-8・文字列の4新規プロセスを直列に採る計画とした。最初の1回で実行中CPUのInstructionPointer、Actual区間、メソッド解決、event lossを確認してから残りへ進む。今回の診断計時は、通常計時の性能比較結果とは区別する。

昇格起動は一度だけ試したが、Windowsから「この操作はユーザーによって取り消されました」と返り、ヘルパーPIDは得られなかった（失敗の観測時刻: 2026-10-09 11:57:10 UTC）。この返却だけでは、人が拒否したのか承認待ちが失効したのかは区別できない。PowerShellの非終端エラーにより呼び出しshell自体は終了コード0だったが、昇格起動は失敗として扱う。再試行はしていない。

ヘルパーの状態ファイル・開始signalは作られておらず、workload起動0回、ETL採取0件。既存WPR sessionも非記録状態である。したがって実CPUのボトルネック測定は未完了であり、新しいCPU時間内訳・原因特定・改善効果は報告できない。次の実採取には、PC側でUAC確認を承認できることが必要になる。本体・公開API・テストの追加変更は行わず、過去の測定値を保持した。

準備物は`artifacts/execution-commonization-2026-10-09/diagnostics/native-cpu/`に保存した。最小プロファイルのSHA-256は`335FBDD8E3D0A7DDFB50E4A609E5583C2952A0A45EDD0C050FB8FC9654ACCCBB`、昇格ヘルパーは`1E351685B6173D3DAF18BF8F3FB4E1D9164ACE72C0A926D70A72A9BFF9261833`、状態の受理条件を合わせた通常権限runnerは`AD2FBAB8C38D9DB17F1D3032C7A4D98DC8072BB4042859EE0ADC28E7829247E9`。これらはGit対象外の準備物であり、今回の実測結果ではない。

既存のローカルDLLだけを参照する最小TraceEvent解析器はReleaseビルドに成功し、警告・エラーは0件。ただしETLがないため、実サンプルのInstructionPointer、Actual 32区間、イベント損失、メソッド対応付けは未検証であり、CPU内訳の集計器が実動確認済みという意味ではない。準備・開始失敗・保全検査は`preparation-audit.json`へ保存した（SHA-256: `2B5092405154C10374C389192393EB11244184FF72245E2ACFE1E9B28B2A0252`）。

### 実行中CPUの採取とメソッド単位の比較

限定昇格の再試行ではUAC承認後にWPRを起動でき、文字列版1プロセスとUTF-8版2プロセスのETLを保存した。ベンチマーク本体は通常権限のまま直列実行し、ライブラリ本体・公開API・テストは変更していない。実行中CPUを採るkernel SampledProfileのInstructionPointerと、BenchmarkDotNetのActual開始・終了を対応付けた結果であり、前のEventPipe sampled-thread-timeの集計とは異なる。

対象は凍結済みの現行実装と同じ4,172,062 byteのASCII入力。Release、SDK 10.0.401、.NET 10.0.12、BenchmarkDotNet 0.15.8、PGO有効、affinity mask 4、warmup 16、Actual 32、invocation 4、unroll 1、launch 1、DontRemoveを維持した。各プロセスのActualは計128回の解析。MemoryStreamからの読み込みを含み、ファイルをディスクから読むGlobalSetupは含まない。UTF-16対応前の実装との新しい性能比較ではなく、現行の入力型別の診断である。

| 条件 | workload PID | Actual区間 | workload threadのCPU標本 | 同じ区間内の全process threadのCPU標本 | trace全体の欠落イベント |
| --- | ---: | ---: | ---: | ---: | ---: |
| 文字列1回目 | 39328 | 32 | 14,462 | 15,073 | 41,313 |
| UTF-8 1回目 | 20368 | 32 | 27,111 | 27,748 | 616,446 |
| UTF-8 2回目 | 13952 | 32 | 27,750 | 28,360 | 23 |

ETLのSampleProfileIntervalは1.000 ms、取得できたSampledProfileのCountは全件1。標本間の平均時刻差をsampling intervalの代用にはしない。Actual区間の標本はInstructionPointerとstackの両方が存在し、IPはイベント時刻に対応するTraceLogのcode addressへ結び付けた。CPU標本の割合はCPU時間分布の推定として扱い、呼び出し回数や個々の命令の厳密な所要時間とは区別する。

欠落数はtrace全体の合計であり、workloadのCPU標本が同じ割合で欠落したとは判断できない。WPRはいずれも保存には成功したが再採取を促す警告を出した。特にUTF-8 1回目は欠落が多く、精密な内訳の主根拠にはしない。UTF-8 2回目で同じ上位経路を確認できたが、文字列版は1プロセスだけである。通常計時の速度差の確定やPGO状態の出現確率の推定には使わない。

4回目の文字列版は未実行。最初のstopでngenによるシンボル生成を含む保存に約4分かかり、3回目の保存後にはヘルパーの15分上限まで十分な採取時間が残らなかった。3本のstart／stopはすべて終了コード0、stop signalで採取を終了した。ヘルパー自体は4回目の開始待ちで時間上限のエラー終了となったが、取得済みETLの保存成功とは区別する。終了後は既定・自分の名前付きsessionとも非記録状態だった。

#### 自己CPU標本の上位経路

下表は各Actual区間のworkload threadの直接IP標本。すべて計128解析分の観測数で、名前解決済み標本だけへの再正規化は行っていない。型引数の「参照型」はJITの共有表現`System.__Canon`を省略したもので、具体的な入力の型が変わったという意味ではない。

| 宣言型とメソッドの略記 | 文字列1回目 | UTF-8 1回目 | UTF-8 2回目 |
| --- | ---: | ---: | ---: |
| SequenceParser<char, (参照型, char)>.Execution.ParseCore | 1,028 | 2,742 | 2,676 |
| DelimitedSequenceParser<KeyValuePair<参照型, 参照型>, char>.Execution.ParseCore | 821 | 1,724 | 2,209 |
| WhiteSpaceExecution.Skip | 554 | 1,618 | 1,712 |
| SequenceParser<参照型, char>.Execution.ParseCore | 122 | 1,583 | 1,579 |
| WordParser.Execution.ParseCore | 721 | 1,406 | 1,566 |
| ExpectCharParser.Execution.ParseCore | 609 | 1,102 | 1,140 |
| coreclr内だが関数名未確定 | 5,287 | 7,742 | 7,862 |

UTF-8版の上位は連接・区切り列・空白の実行経路に分布している。ただしParseCoreのselfには、読み取り・前進・結果の構築などinlineされた処理も含まれる。この数をvirtual dispatch自体の費用とは呼ばない。独立したRune.DecodeFromUtf8に帰属したselfはUTF-8の1回目246、2回目249標本だが、inlineされた復号処理を含まないため、復号全体が軽いという結論にも使わない。

初期の`MissingStacks`はstack leafのFullMethodNameが空の標本を数えており、stack欠落と名前未解決を混同していた。訂正版でstackの存在とメソッド・module解決を分離した。名前解決前のFullMethodName取得数は文字列8,225、UTF-8 1回目17,635、2回目18,541。直接IPとstack leafのFullMethodNameは解決済み標本で一致したが、これは名前の一致であり数値アドレスの一致を測ったものではない。初期JSONの`IpAndStackLeafExactMatch`という項目名を、そのままアドレス一致の根拠にはしない。

Actual markerのID 15／16と測定範囲は、使用版BenchmarkDotNet 0.15.8の公式ソースでも確認した。RunIterationの明示的なGcCollectはIterationStartの前とIterationStopの後にあり、その強制GCは今回のActual区間に入らない。[Engineのソース](https://github.com/dotnet/BenchmarkDotNet/blob/v0.15.8/src/BenchmarkDotNet/Engines/Engine.cs)、[Actual markerの定義](https://github.com/dotnet/BenchmarkDotNet/blob/v0.15.8/src/BenchmarkDotNet/Engines/EngineEventSource.cs)

#### ネイティブ関数別集計の不一致と報告の撤回

未解決標本の最大部分がcoreclr内だったため、Microsoftの公開symbol serverから当該moduleのPDBだけを取得した。ETLのPDB GUIDは`07fb51ec-fd57-48be-9db9-32401e0670cd`、age 1。PDBのSHA-256は`10CC92EF13FD745DC1A0B8FBE44347D502049A73800063972C12520A26802D1D`。既存Visual StudioのDIA DLLをartifact内で再利用し、新しいツールのインストールや権限設定変更は行っていない。

ところが、関数名を7,862件すべて解決したとする`run-03-coreclr-pdb-test.json`と、0件とする別の抽出結果に不一致が見つかった。後者はmodule選択や抽出コードの失敗による無効な試行である。前者も、成功したというprobeの実行ソース・コマンドを追跡できておらず、独立に再計算できる根拠が揃っていない。そのためGC関数別の数値は採用せず、「GCにCPUが集中している」という途中報告を撤回した。moduleとしてcoreclrへ帰属した標本数と、上記の管理対象メソッドの集計はこの不一致と分ける。ネイティブ費用の関数別内訳は未確定である。

不一致の2ファイルは削除・上書きせず保持した。前者のSHA-256は`FDC1A0079AB123EA88817D70DC064503DC22FB4EE5E2CB0A7FF9AFE153808775`、後者`run-03-utf8-02-coreclr-pdb-actual.json`は`6681F7F4F814F6FF621A6E2A9C57EBBF0D7F8C0B714BB47A65A1B623A197DE59`。別プロセス解析器のDIA読み込み失敗による空JSONも、成功成果物として扱わない。

有効な直接IP集計は`diagnostics/native-cpu/run-01-string-01-summary-v2.json`、`run-02-utf8-01-summary.json`、`run-03-utf8-02-summary.json`、採取ツールの実行記録は`elevated-helper-result.json`。正確なメソッド名、self／inclusive、全thread版、未解決moduleの明細はこれらを参照する。raw ETLは保存し、新しい起動別CSVをGit対象へ追加していない。本体の最適化案を実装する前に、ネイティブ集計の不整合と、ホットなParseCore内のどの処理が費用を占めるかを確認する必要がある。

#### ネイティブ名解決の再実行

追加10分の承認後、取得済みのUTF-8 2回目（PID 13952）のETLXと一致するPDBを使って、別の実行ソースから再解析を試みた。新規採取・ETLX再変換・本体変更は行っていない。

直接確認できた失敗原因はDIA DLLの配置先で、解析器が探す`analysis/bin/Release/native/amd64/msdia140.dll`に対し、既存DLLは`analysis/bin/Release/net10.0/native/amd64/msdia140.dll`にあった。同じDLLをartifact内の期待位置へコピーした後は、起動時のロード例外は出なくなった。両DLLのSHA-256は`55E1CE73E8FFD54C88A75C0FDCB11A56DF4BA0F80949CF585741855E9E4181AA`。

解析ソースでは固定runtimeパスによるmodule選択をやめ、対象PIDのイベント時点の直接IPに対応するmodule path、PDB GUID／age、ImageBase以上ImageEnd未満の範囲を照合してRVAを計算するようにした。`analysis/Program.cs`の実行時SHA-256は`21F675E14C4B23789F0F96C80465244D3AC0465F7CE0E23D74EA0458967C92B1`。既存ETLXとPDBのハッシュは採取時のものと一致した。

再解析は継続したが、時間枠内に集計JSONは出なかった。2026-10-09 13:10:31 UTCに実行セッションへCtrl-Cを送り、直後に当該runnerと解析器の停止を確認した。`run-03-native-pdb-fixed.stdout.txt`と`.stderr.txt`はともに0 byte、終了コードは未取得。停止時点でどの処理に時間がかかっていたかも特定していない。したがって、DLLの配置修復と集計の完走・正しさは区別し、追加のネイティブ関数別CPU内訳やGC原因説は採用しない。前節の管理対象メソッド・module単位の有効な集計と、未検証probeの撤回を維持する。

今回の変更はGit対象外の解析器・DLL配置と本記録への追記のみ。本体・公開API・テスト・Git indexは変更していないため、ライブラリのビルドとテストは再実行していない。

#### 解析器の遅延原因と修復後の検証

工程別の計時で解析器自体の遅延を特定し、取得済み3本のネイティブ関数別集計を完了した。旧probeの数値を採用したのではなく、保存した解析ソース・実行コマンド・正常終了した出力から再計算した結果である。新規採取やライブラリ本体の最適化は行っていない。

修復前の短試行は50.017秒時点で停止し、対象PIDのCPU標本2,204件まで処理していた。このうち`matchPdbModule`が47.9545秒、`FindNameForRva`は352回で0.1057秒だった。標本ごとに全trace moduleを再照合している工程が、解析器の遅延の主因だった。ログは`run-03-profile-short-01.stderr.txt`。停止した短試行を完走結果としては扱わない。Stopwatchの周波数は当環境で10,000,000であり、最終版では生ticks・周波数と、周波数から換算したmsを記録する。

修復ではPDB GUID／ageの読み取りと一致module候補の抽出を一度だけ行い、候補をpath別に保持した。各イベントでは、時刻に対応するIPのmodule pathとimage rangeを照合する。trace内には同じPDBのmoduleが3箇所あるが、対象PIDの標本では`dotnet/shared/Microsoft.NETCore.App/10.0.12/coreclr.dll`のpath候補1件・範囲一致1件だけを選んだ。関数名の検索方法やActual区間の選択は変えていない。進捗を確実に保存するため、起動は`Start-Process`によるstdout／stderrの直接リダイレクトへ変更した。

| 条件 | 解析器の完走時間 | 実行threadのActual CPU標本 | coreclrの関数名解決／対象 | GC主要8関数のself標本 |
| --- | ---: | ---: | ---: | ---: |
| 文字列1回目 | 4.749秒 | 14,462 | 5,287／5,287 | 3,735（25.83%） |
| UTF-8 1回目 | 4.869秒 | 27,111 | 7,742／7,742 | 5,533（20.41%） |
| UTF-8 2回目 | 4.320秒 | 27,750 | 7,862／7,862 | 5,549（20.00%） |

ここでの秒は取得済みtraceを処理する解析器の所要時間であり、JSONパーサーの性能測定値ではない。全件を処理したUTF-8 2回目では、module照合48,418回の累積は14.5 ms、候補の抽出は2.2 ms、PDB名検索13,816回は959.0 msだった。修復前後で処理件数が異なるため、短試行から解析速度の倍率は推定しない。計時した工程の合計が全経過時間になるとも扱わない。

GC主要8関数は`WKS::gc_heap::`の`plan_phase`、`mark_object_simple1`、`relocate_address`、`relocate_survivors_in_brick`、`compact_in_brick`、`relocate_survivors`、`compact_plug`、`compact_phase`。割合の分母は名前解決済み標本だけでなく、実行threadのActual CPU標本全体である。GC全体の費用を網羅する分類ではなく、明確に帰属できた主要処理の合計を示す。

全3本でActual 32区間、実行threadの標本数、欠落イベント数が既存の有効summaryと一致した。旧top 20のうちcoreclr以外の19項目は、新しい全関数マップでも各件数が一致した。旧summaryに全マップはないため、旧新全関数の完全一致とは呼ばない。新マップは全標本を含み、管理対象名・PDBで解決したcoreclr・その他module・module不明の排他的な内訳は、文字列が8,225＋5,287＋803＋147、UTF-8 1回目が17,635＋7,742＋1,512＋222、2回目が18,541＋7,862＋1,155＋192で、それぞれ総数と一致した。native全関数内訳の合計、IPとRVAの差分も検査した。

同じ最終ソース・DLLでUTF-8 2回目を別名へ再解析し、4.380秒で正常終了した。最初の確定出力と再実行出力のSHA-256はともに`0968E7995F8A1E8E6F0B14CBE38A311735D8F747CF931F06FC6DC72BD78E7119`で、集計JSONはバイト単位で一致した。工程の計時を記録するstderrは一致条件に含めない。解析器のReleaseビルドは終了コード0、警告・エラー0件、確定した4実行もすべて終了コード0だった。準備時のbackup `.cs`混入による`CS8802`は、backupをコンパイル対象外の`.cs.txt`へ移して解消した。

確認できた費用は、GCのmark／relocate／compactと、前節の連接・区切り列・空白などの管理対象経路に分布している。GCの費用は文字列版にもあり、「UTF-8版が遅い原因はGC」とは特定していない。ParseCoreのselfにはinlineされた読み取り・前進・結果構築も含まれるため、virtual dispatchや復号のどれを削るべきかも、この集計だけでは決めない。文字列版の欠落41,313、UTF-8 1回目の616,446という品質制約は解析器の修復で消えたわけではなく、精密な入力型間の比較には引き続き適用する。

最終ソース`analysis/Program.cs`のSHA-256は`CC39BB3DA220AB20C48DFD49C40A75300FD56E3AC971A3E4B270F13FC4B0117C`、実行DLLは`574C42C142C45B4063B65101DA22931CC1760BD979DAFBFBDA1ED972AB991958`。確定出力は`run-01-native-pdb-final.stdout.txt`、`run-02-native-pdb-final.stdout.txt`、`run-03-native-pdb-final2.stdout.txt`、`run-03-native-pdb-repeat.stdout.txt`。全メソッドマップは`IpSelfFullWorkloadThread`、native全内訳は`IpNativeSymbolBreakdownWorkloadThread`、対応例は`NativeSampleExamples`を参照する。再現手順とハッシュは`native-pdb-analysis-report.md`、具体コマンドと終了コードは`native-pdb-run-commands.txt`へ保存した。いずれも`artifacts/execution-commonization-2026-10-09/diagnostics/native-cpu/`内で、再実行時は別名を使って保存済み成果物を上書きしない。

今回の変更はGit対象外の解析器・解析ログ・再現記録と、本ファイルへの追記のみ。ライブラリ本体・JSONパーサーは凍結版とハッシュが一致し、テスト・公開API・Git indexも変更していない。本体のビルドとテストは再実行せず、新しい起動別CSVもGit対象へ追加していない。

## テスト・ビルド・静的検査

- 既存の振る舞いを確認する4テストを、本体の共通化前に追加して成功を確認した。振る舞い変更ではないため、形式的なRedやGreenの実装変更は行わずRefactorとして進めた。
- Kotowari 227件、JSON 115件、合計342件成功。失敗・Skipとも0件。
- 両テストプロジェクトの非増分・アナライザー付きビルドは成功し、警告・エラーとも0件。
- ソリューション全体のビルドは失敗。既存の`CS8345`が`WordParserAccumulationBenchmarks.cs:48`と`WordParserBenchmarks.cs:12`に残る。旧SpracheJSON 1.0.1の`NU1701`も残る。これらは今回の共通化前からの問題で、ベンチマーク本体を今回の変更へ混ぜていない。
- `dotnet format --verify-no-changes --severity info`は終了コード2。67件すべて先行する診断と一致し、新規診断・変更行上の診断は0件。SARIFの8件も先行診断と一致し、変更行上の診断は0件。
- `git diff --check`は成功。本体・テストの16ファイルは既存のUTF-8 BOM／CRLFを維持した。Git indexと範囲外の既存変更は保全した。

コマンド、終了コード、所要時間、差分と保全結果は`refactor-manifest.json`、`refactor-only.patch`、`preservation-audit.json`に記録した。

## 変更ファイルと未コミットの範囲

今回のコード変更は以下の16ファイル。先行する未コミットのParserExecution導入などへ依存するため、HEADとの差分全体を今回だけの変更と取り違えない。コミット前には先行実装を含めた対象範囲の確認が必要である。

本体7ファイル:

- `Kotowari/ParserExecution.cs`
- `Kotowari/Core/OneOrMoreParser.cs`
- `Kotowari/Core/SequenceParser.cs`
- `Kotowari/Core/OrParser.cs`
- `Kotowari/Core/ZeroOrMoreParser.cs`
- `Kotowari/Core/WordParser.cs`
- `Kotowari/WithWhiteSpaceParser.cs`

テスト9ファイル:

- `Kotowari.Tests/Core/CharParserのテスト.cs`
- `Kotowari.Tests/Core/ExpectCharParserのテスト.cs`
- `Kotowari.Tests/Core/OrParserのテスト.cs`
- `Kotowari.Tests/Core/SeqenceParsetのテスト.cs`
- `Kotowari.Tests/Core/ParserConverterのテスト.cs`
- `Kotowari.Tests/Core/ZeroOrMoreParserのテスト.cs`
- `Kotowari.Tests/Core/WordParserのテスト.cs`
- `Kotowari.Tests/Core/WithWhiteSpaceParserのテスト.cs`（新規）
- `Kotowari.Tests/Core/TestParsers.cs`（新規）

記録は本ファイルと`Kotowari.Benchmarks/Results/README.md`の2ファイル。コミットメッセージ案は「Executionとテスト用パーサーの重複を共通化し、再測定結果を記録」。まだステージング・コミット・pushは行っていない。

## 生データと測定中の中断

全20プロセスの完全JSON、ログ、CPU監視、実際の生成Jobの設定・DLLハッシュは`runs/normal/block-01/`から`block-04/`に保存した。集計は`normal-summary.json`。新しい起動別CSVをGit対象へ追加せず、この記録へ全プロセス平均をまとめた。凍結ソースと生データの`artifacts/`はGit対象外で、これらだけを恒久保存済みのコミットとして扱わない。

準備中のエラーと、BDNの生成Job出力まで不変とした過剰な保全チェックによる中断を別に記録した。既に完了していた2プロセスは再測定・除外せず、残り18プロセスを続行した。20測定プロセスはすべて成功し、それぞれActual／Resultの8反復が揃っている。

BDNは凍結ソースを参照する測定プロジェクトを生成Jobから再ビルドする。生成JobのDLLと事前ビルドしたホストDLLのバイト一致を版確認の条件にせず、ソース、参照プロジェクト、生成設定と実出力のハッシュを記録した。実際の変更前Jobでも全入力消費・結果同等性・100,001ノードを検証した。

以前の`Job-WRGPCZ-1`にある生成済みDLL／PDBの4ファイルはBDNにより更新され、正確な旧コピーは見つからなかったため復元していない。旧コピーが確認できた`.notcs`だけ復元し、更新後のDLL／PDBとハッシュは別保存した。これはGit対象外のBDN生成ラッパーで、本体ソース、主ホスト・ライブラリ、入力、ユーザーの既存変更とは別である。詳細は`setup-failures.json`、`runner-correction-job-scratch.json`、`final-preservation-audit.json`を参照する。
