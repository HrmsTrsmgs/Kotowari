# 開発候補・全体レビュー

確認日：2026-10-23。対象：`codex/optimize-word-parser` の `be1dfe7`。
ユーザーの「全行・全観点から見直して改善点の一覧を作る」依頼に基づく。
以下は2026-10-23のレビュー時に整理した改善候補であり、一覧への掲載だけで仕様変更・実装着手の承認とはしない。レビュー時にはソースを変更していない。以後の対応状況は各項目に記す。

## 範囲と読み方

Git管理下のC#は全102ファイル・6,318行を分担して全文確認した。行数は空行・コメントを含む。

| 対象 | ファイル数 | 行数 |
|---|---:|---:|
| Kotowari本体 | 32 | 1,179 |
| 本体テスト・補助型 | 33 | 3,144 |
| JSONサンプル | 8 | 678 |
| JSONテスト | 1 | 525 |
| ベンチマーク | 28 | 792 |

5プロジェクト、solution、中央パッケージ設定、SDK設定、2つのeditorconfig、gitignore、`Check-Stability.ps1`、README、LICENSEも確認した。
正しさ、終了性、境界値、例外・資源解放、公開契約、共有・可変性、テストの検出力、性能比較、再現性、配布・保守の観点を見た。
開発指示・再開記録と関連する採否記録も照合したが、全履歴・全実験CSV/JSONの再監査ではない。
生成物、巨大入力の全行、依存ライブラリ本体の全行は対象外。脆弱性データベース、最新ライセンス、実際のNuGet表示・別clone再構築も未確認。

* **実行確認**：現在のソースから作ったDLLへの小さな診断で確認した現状。
* **静的確認**：制御フロー、設定、テスト入力から確認できる現状。実行再現とは区別する。
* **仕様確認**：現状は分かるが、開発者の意図・変更後の契約が未確定。
* **提案**：採用前に範囲と必要性を判断する。既存挙動を無条件に仕様認定しない。

優先度は、P1＝停止・資源枯渇を先に扱う、P2＝正しさ・検証基盤を次に扱う、P3＝用途・公開計画に合わせて扱う、の目安。修正順の合意ではない。

## P1：終了性と資源使用

### R01：EOFを通常文字として受理し、閉じていないJSON文字列が終了しない

**2026-10-25の対応：JSONの非終了対策はGreen完了、`b9cf5ba` に保存済み。** ユーザーの指摘を一件ずつ解決する指示に従い、まずJSONサンプルだけを変更した。
未終端キー・値がParseExceptionで終了する回帰テスト2ケースと、空キー・空値の正常テストを追加。JCharをJSON内の非公開パーサーで包み、Indexが入力長に達したときFailureを返す。
本体のEOF/NUL・一般反復の契約や、実入力NULの受理は維持した。したがって本項の本体側のEOF仕様検討まで完了したわけではない。
監視付き回帰・全326テスト・非増分Releaseビルドとアナライザーが成功。formatは既存書式指摘とworkspace読み込み警告でexit 1、性能は未測定。現在の停止位置と検証の留保は[再開記録](development-context.md#現在の作業状況)を参照する。

続くRefactorでは、同じ終端判定・委譲を行う補助型をJSON専用の内部型 [JSONStringCharacterParser](../JsonParser/JSONStringCharacterParser.cs) として別ファイルへ分離した。全326テスト・非増分Releaseビルドとアナライザー成功。今回のformatは既存指摘とworkspace読み込み警告でexit 2。2026-10-25にユーザーがコミットを了承し、この更新を含むFコミットで保存する。

以下はレビュー時に確認した問題と、未解決の契約に関する根拠。

**実行確認＋静的確認。EOF契約自体は仕様確認が必要。**

[Cursol](../Kotowari/Core/Cursol.cs) の48～54行は範囲外をNULとし、末尾で移動を止める。
[ConditionalCharParser](../Kotowari/Core/ConditionalCharParser.cs) の9～14行と [JSONのJChar/JString](../JsonParser/JSON.cs) の153～165行を通ると、EOFも通常文字として成功する。
`JSON.Parse("{\"a\":\"x")` は閉じ引用符を待つ反復から戻らず、NULを蓄積し続ける。

診断では子プロセスに2秒・観測working set 128 MiBの停止条件を置いた。最終確認では戻り値・例外の出力がないまま約303 msで観測値135,098,368 bytesとなり、監視側が強制終了した。
これは性能計測や割り当て量の測定ではない。永久反復の根拠は同じ位置で成功し続ける制御フローであり、タイムアウトだけから断定していない。
空入力で `CharParser('\0')`、`ConditionalCharParser(_ => true)`、`ExpectCharParser(CharParser('a'))` が成功し、Indexが0のままであることも確認した。

**改善方向**：まずJSON文字列の不正入力を安全に拒否する。本体ではEOFと実入力NULを区別すべきか決める。
[Wordの既存テスト](../Kotowari.Tests/Core/WordParserのテスト.cs) の192・204行付近にはEOF／負位置のNUL受理を固定するものがあるため、黙って削除・反転しない。
文字列パーサーの仕様修正と、次項の一般的な反復契約は別の変更単位にできる。

### R02：入力を消費しない成功を反復すると終了しない

**実行確認＋静的確認。対処時の契約は未確定。**

[ZeroOrMore](../Kotowari/Core/ZeroOrMoreParser.cs) 29～44行、これを使う [OneOrMore](../Kotowari/Core/OneOrMoreParser.cs)、[DelimitedSequence](../Kotowari/Core/DelimitedSequenceParser.cs) 32～49行、[WithWhiteSpace](../Kotowari/WithWhiteSpaceParser.cs) 23～31行、[Wordの空白スキップ](../Kotowari/Core/WordParser.cs) 62～67行に進捗の確認がない。
EOF問題がなくても、空WordやOptionalの不在成功で発生する。

例：`ZeroOrMore(Optional(Char('a')))` を空入力へ、`OneOrMore(Word(""))` を任意入力へ適用する。
診断用の子が5回目に例外で停止するようにし、反復・区切り反復・空白スキップで位置が `0,0,0,0,0` のまま呼ばれることを確認した。値収集はメモリを増やし、空白スキップはCPUを使用し続ける。

**改善方向**：非消費成功を例外にするか、停止として扱うか決める。Delimitedでは要素と区切りを合わせた一周の進捗を考え、片方が空というだけで禁止しない。
後退や文書差し替えを許す契約も確認し、Indexの単純な不一致だけを万能な終了保証とはしない。正常入力の性能への影響は変更案が決まってから評価する。

## P2：正しさとテスト

### R03：正方向のカーソル移動がオーバーフローで負位置になる

**実行確認。** [Cursol](../Kotowari/Core/Cursol.cs) 53～54行の `Math.Min(index + step, Length)` は加算後に制限する。
`new Cursol("abc").GoFoward(1).GoFoward(int.MaxValue).Index` は `-2147483648` になった。
広い型での加算または事前の範囲判定が候補。既存の負位置許容まで一緒に変更せず、非ゼロ開始位置と整数境界のテストを追加する。

### R04：JSON数値の取得結果が実行カルチャーで変わる

**実行確認。** [JSONLiteral.Get](../JsonParser/JSONLiteral.cs) 55～56行は `Convert.ToDouble(string)` を使う。
同じ `{"a":1.2}` のGet結果がen-USでは1.2、de-DEでは12、fr-FRではFormatExceptionになった。
JSON用のカルチャー非依存の変換へ改める候補。現在のテストは主に数値の字句を確認し、Getの値を確認していない。
doubleの精度・範囲をどこまで扱うかは、この誤変換の修正と分けて判断する。

### R05：JSON.Parseが未解析の末尾を捨てる

**実行確認。全文解析を入口の契約にするかは要確認。** [JSON.Parse](../JsonParser/JSON.cs) 221～228行は最初のobjectの成功だけを返す。
`{}garbage` と `{}{}` を受理し、返却値には残り入力がない。部分解析用コンビネーターと、文書全体を読む入口を区別する必要がある。
全文解析なら末尾の許可された空白とEOFを確認する。単なるテストのために公開APIを増やす案ではない。

### R06：JSONの対応文法を整理し、実際の規格差を補う

**実行確認。対応範囲・互換性は仕様確認。** [JSON.cs](../JsonParser/JSON.cs) 72～156行、219～228行が対象。

| 入力例・条件 | 現状 |
|---|---|
| `{"a":"\u0041"}`、`{"a":"\/"}` | ParseException |
| 文字列内の生の改行・NUL | 受理 |
| `{"a":01}`、`{"a":١}`（U+0661） | 受理 |
| `{"a":TRUE}` | 受理。既存テストも大文字受理を固定 |
| トップレベル `[]` | 拒否。公開入口はJSONObjectを返す |
| `{"a":1,"a":2}` | ToDictionary由来のArgumentException |

標準文法では、文字列の制御文字をエスケープし、`\uXXXX` と `\/` を扱う。数値はASCII数字で、先頭ゼロに制約がある。リテラルは小文字で、トップレベルはobject以外も含む。重複キーの拒否自体を規格違反とは扱わない。[RFC 8259 §2～7](https://www.rfc-editor.org/rfc/rfc8259.html#section-2)

**改善方向**：通常文字・escape・整数部の文法を小さく分けて補う。JSON規格に合わせるという過去のユーザー指示を踏まえ、大文字受理を固定する既存テストとの食い違いを明示して修正範囲を決める。
サンプルをフルシリアライザーへ拡大したり、UTF-8試作・Rune設計を再開したりすることは既定の提案にしない。
重複キー、トップレベルの返却型、受理する拡張はそれぞれ判断し、例外の種類も整理する。

### R07：一部テストが名前に書かれた失敗・成功経路を通っていない

**静的確認。テスト入力の改善候補。**

* [Orテスト](../Kotowari.Tests/Core/OrParserのテスト.cs) 54～60行：「先頭失敗・2番目成功の値」が入力・期待値とも `abc` で、先頭候補の成功を見ている。`ab` へ直す候補。
* [4要素Sequenceテスト](../Kotowari.Tests/Core/要素を4つ持つSeqenceParserのテスト.cs) 35～60行と5～10要素の同型ケース：`abc cd ...` では先頭の `ab` が接頭辞成功し、次の子で失敗する。単語内部の不一致に直す。2・3要素は修正済みなので重複追加しない。
* [JSONテスト](../JsonParserのテスト/JSONのテスト.cs) 219～228行：`e10+`、`+e10` は整数部がなく、指数符号の位置を検証していない。478～502行の空白否定ケースも数字欠落を同時に含む。例として `1e10+`、`1 .0`、`1.0 e1`、`1.0e +1` のように狙った不正条件だけを残す。

件数を増やすのではなく、既存テストが想定した経路を確実に検証する。必要な代表ケースで短絡や子の呼び出し位置を確認する。

### R08：JSONの深い比較が値の違い・配列順序を見逃す

**実行確認。** [JSONテスト](../JsonParserのテスト/JSONのテスト.cs) 20～47行の期待木比較と408行付近の空白有無の比較は、`BeEquivalentTo` の既定設定を使う。
同じ比較で `{"a":1}` と `{"a":2}` が合格し、`{"a":[true,false]}` と `{"a":[false,true]}` も合格した。後者は `WithStrictOrdering()` だけを足しても合格する。

原因は、Pairs/Elementsの要素が `IJSONValue` として宣言され、そのinterfaceには通常の値プロパティがないこと。既定のメンバー選択は宣言型を優先する。加えて、コレクションの順序は既定では無視される。[Fluent Assertions公式文書](https://fluentassertions.com/objectgraphs/)
実行時型のメンバーを使う設定で数値差を、実行時型＋厳密順序の設定で配列順序差を検出することまで確認した。キー名の違いは既定設定でも検出できたため、比較が全面的に無効という意味ではない。

**改善方向**：実際のノード型・値とJSON配列順序を比較する。既存の独立した期待木を生かし、値を故意に変えたとき比較が失敗することも確認する。objectのキー順序まで仕様として強制しない。

### R09：失敗時の消費・巻き戻し規則を明文化し、Optionalの直接テストを補う

**静的確認＋仕様確認。** [OptionalParser](../Kotowari/Core/OptionalParser.cs) 17～22行は、子が消費して失敗した場合、その位置で「不在」の成功を返す。
Sequence/Orは開始位置へ戻し、ZeroOrMoreは最後の失敗位置を使い、OneOrMoreは先頭失敗と後続失敗で異なり、Delimitedは最後に成功した要素の直後へ戻す。
これらを一律にそろえるべきとは断定しない。複数の動作は既存テストで保護されている。

利用者が組み合わせる際の契約として説明し、Core OptionalParserの成功・不在・消費失敗・例外伝播を直接検証する候補。
[Optionalのテスト](../Kotowari.Tests/Core/Optionalのテスト.cs) は値型Optionalのテストであり、このパーサーの直接テストではない。

### R10：左再帰・深い再帰の制約を示す

**静的確認＋仕様確認。枯渇する深さは未測定。** [RecursiveParser](../Kotowari/Core/RecursiveParser.cs) 12～13行と [JSONの再帰文法](../JsonParser/JSON.cs) 175～198行には深さ制限がない。
同位置で自分を呼ぶ文法は終了せず、正常な右再帰でも入力のネストに比例してスタックを使う。
少なくとも左再帰非対応・深い入力の制約を説明する。外部入力へ使う範囲に応じ、深さ上限などが必要か判断する。
毎回のgetter評価をやめるキャッシュ案ではない。危険なスタック枯渇の再現実行はしていない。

## P2：性能比較・再現性

### R11：標準の測定前検証を、失敗を検出できる入口にする

**静的確認。** [ValidateAll](../Kotowari.Benchmarks/JsonParserBenchmarks.cs) 20～43行は例外を表示するだけで、[Main](../Kotowari.Benchmarks/Program.cs) 10～15行は正常終了する。
ノード数も表示だけで期待値とは比較せず、Setupは入力を読むだけ。したがって `--validate` の終了コードでは解析失敗を検出できない。

**改善方向**：失敗を集約して非ゼロ終了し、全文消費・値・配列順序を含む結果一致を測定前に検証する。検証処理を計時へ混ぜない。
過去の測定では一時harnessによる全文消費・正規化木の検証を行っており、過去データを一律に未検証扱いしない。
旧SpracheJSONの異なる木・専用JSONパーサーの参考値は引き続き分ける。

### R12：比較用アダプター間の結果オブジェクト共有方針が違う

**実行確認＋静的確認。寄与率は未測定。** [Pidginアダプター](../Kotowari.Benchmarks/PidginJsonParser.cs) 24～26行はtrue/false/nullのノードを文法構築時に作り `ThenReturn` で共有する。
Sprache、Superpower、KotowariのJSONサンプルは出現ごとに生成する。
同じ `true` を2回解析した参照の一致はPidginだけtrue、Sprache/Superpowerはfalseだった。

値やノード数が一致しても割り当て作業は同じではない。比較用の生成・共有方針をそろえるか、差の具体条件を明記する。
この発見だけでPidginの速度差を説明したり、優劣が逆転すると結論したりしない。

### R13：比較用の独自JSON文法にも異なる受理範囲がある

**実行確認。ライブラリそのものではなく、このリポジトリのアダプターの問題。**

| 入力 | Pidgin版 | Sprache版 | Superpower版 |
|---|---|---|---|
| `[]`、`{}` | 成功 | 成功 | 成功 |
| `[ ]`、`{ }` | 拒否 | 成功 | 拒否 |
| `true ` | 成功 | 拒否 | 成功 |
| `{"a":1e+}` | 受理 | 受理 | 受理 |

[Pidgin](../Kotowari.Benchmarks/PidginJsonParser.cs) 16～42行、[Sprache](../Kotowari.Benchmarks/SpracheJsonParser.cs) 16～44行、[Superpower](../Kotowari.Benchmarks/SuperpowerJsonParser.cs) 19～44行が対象。
数値を許可文字の反復として読む、escapeを持たないなど、Kotowari側と検証処理量も異なる。
使用中の4 MB入力が壊れているという証拠ではないが、「同じJSON機能の速度」と一般化するには条件差がある。
比較対象とする文法の範囲を定め、小さな共通の妥当／不正入力で各アダプターを検証する候補。

### R14：最新の測定経路を別cloneで再構築できるようにする

**既存記録で確認した制約。** [Sequence3報告](../Kotowari.Benchmarks/Results/2026-10-20-sequence3-projection.md) の保存資料には、生成済みMeasurement/ChildHarnessのソースが含まれていない。
再計時は固定されたローカル成果物に依存する。通常のbenchmark入口はstringの解析だけで、最近のMemoryStream生成・コピー・復号込みの判定条件とは異なる。

最小hostのソースまたは生成手順、測定前検証、集計の再現手順を既存benchmark配下へ集約する候補。DLLや巨大ログを無制限にGitへ追加する案ではない。
別cloneで一度再構築を確認し、旧記録の条件と現在の入口を区別する。既に留保が書かれているため、過去報告の誤記としては扱わない。

## P2/P3：API・公開・保守

### R15（P2）：本体ライブラリの最小利用例をREADMEへ追加する

**提案。** [README](../README.md) はbuild/test/benchmarkが中心で、公開APIによる最小解析例、成功・失敗、残り入力の扱いがない。
現在のAPIだけで短い例と参照方法を示し、JSONサンプルと本体の責務を区別する。
利用例を書くためだけに公開APIを追加・変更しない。

### R16（P3）：共有利用と異常値の契約を明確にする

**静的確認＋一部実行確認。仕様判断が必要。**

* [OrParser](../Kotowari/Core/OrParser.cs) は渡された配列を保持し、外から差し替えると文法が変わる。既存テストもこの挙動を固定。
* ignoreCaseのChar/Wordは解析時カルチャーに依存する。JSON数値Getの誤変換とは別の既存契約。
* 一部のnull引数は構築時でなく、その枝へ到達した時点で例外になる。これも固定テストがある。
* `default(Cursol).Text.Length` は0だが、`Current` はNullReferenceExceptionになることを実行確認した。defaultを無効値とするなら、その条件を示す。

文法を並行共有できる条件を説明する候補。防御コピー、InvariantCulture化、一律の早期null拒否を無断で既定の修正にしない。
並行実行そのものの負荷試験はしていない。

### R17（P3）：未実装の公開ToJSONと未使用JSONMapを整理する

**静的確認＋用途判断。** [JSONObject](../JsonParser/JSONObject.cs) 63行、[JSONArray](../JsonParser/JSONArray.cs) 64行、[JSONLiteral](../JsonParser/JSONLiteral.cs) 84行のToJSONは必ずNotImplementedExceptionを投げる。
出力も提供するのか、未対応APIを整理するのか決める。

[JSONMap](../JsonParser/JSONMap.cs) は内部・未使用で、現在のJSON.Parseには影響しない。
32行の `IsSubclassOf(typeof(IDictionary))` はinterface実装判定にならず、89行の条件は非配列IListを拒否し、数値写像は要求型によらずboxed doubleを返す。
GetProperties/GetFieldsはinstance限定でなく、将来使うとstaticメンバーも書き換える経路がある。
保持する理由を確認し、不要なら整理、使うなら対象型・副作用を限定してテストする。未公開経路の不具合を現行Parseの障害に数えない。

### R18（P3）：解析失敗の診断情報を用途に応じて補う

**静的確認＋提案。** [ParseException](../JsonParser/ParseException.cs) 5～7行とJSON.Parseのthrowは位置・期待要素などを持たない。
入力誤りを直す利用場面では診断が難しい。現在の結果・カーソル設計と合わせて、最小限どの情報を返すか考える。
大きなエラー回復機構やテスト専用APIを先に作る案ではない。

### R19（P3）：既存のXMLコメントを配布先へ届ける

**設定の実効値を確認。生成パッケージは未確認。** [本体csproj](../Kotowari/Marimo.Kotowari.csproj) は `GenerateDocumentationFile=false` と評価された。
XML生成・同梱と公開APIの不足コメントを確認する候補。内部最適化に関する長い性能remarksを利用者説明の代わりにはしない。

### R20（P3）：配布対象と公開前検証を明確にする

**設定確認＋提案。** [JSONサンプルcsproj](../JsonParser/JsonParser.csproj) は `IsPackable=true`、Versionは1.0.0と評価された。
本体だけを配布するならサンプルを明示除外し、本体のpack手順を示す。
trackedのCI/release workflowとパッケージ消費側のsmoke testは見当たらない。本体の `EnablePackageValidation` は有効だが基準版は未設定。
初回公開前にbuild/test/packと別プロジェクトからの参照を小さく自動検証する候補。自動公開の追加は別判断であり、今回の作業ではpack・publishしていない。

### R21（P3）：SDKの「固定」という説明を実態に合わせる

**静的確認。** [README](../README.md) 7行はpinnedと説明するが、[global.json](../global.json) は10.0.302＋latestFeatureを許可し、既存測定は10.0.401を使う。
厳密固定を強制するのではなく、許可する更新範囲と実測時のSDK/runtimeを区別して説明する候補。

### R22（P3）：最適化検証の無効化を必要な測定に限定する

**静的確認。** [Program.cs](../Kotowari.Benchmarks/Program.cs) 18～19行の `DisableOptimizationsValidator` は全benchmarkへ適用される。
旧SpracheJSONを扱う理由は記録済みだが、Word単独等にも抑制が及ぶ。必要な比較だけ別設定にして、他の測定で誤設定を検出できるようにする候補。

### R23（P3）：測定候補型のIgnoreCaseプロパティを実設定とそろえる

**静的確認。** [WordParserAccumulationParser](../Kotowari.Benchmarks/WordParserAccumulationParser.cs) 13～14行で、子にはignoreCaseを渡すがプロパティには代入せず常にfalse。
現在の解析動作は子の引数に従い、速度比較が壊れているという指摘ではない。未使用なら削除、必要なら初期化する小さな候補。

### R24（P3）：古い非同期名を実態に合わせる

**静的確認。** 多くのテスト名にParseAsync、Word等の同期メソッド名にSkipBlankAsyncが残る。
実際のAPIと失敗一覧が食い違うため、関連箇所を直す際に同期する候補。これだけで大規模な一括改名・整形を始めない。

## 維持する点・今回結論しないこと

* ParsedValueBufferの結果コピー、finallyでのpool返却、参照を含む配列のクリアを確認した。具体的な漏れ・結果の使い回し混線は見つけていない。pool返却数の実測保証ではない。
* Sequenceのタプル射影順序・要素抜けは見つけていない。テスト入力の誤りと実装不具合を混同しない。
* ParseCore特例、既存の採用済み最適化、UTF-8撤回は[判断記録](development-context.md#判断記録)を維持する。既存の不採用案を新しい原因根拠なく再提案しない。
* Check-Stability.ps1はResult/Actualの二重計上を避け、起動間CVを扱う。今回の静的確認では重大な計算誤りを見つけていない。全過去データの再計算はしていない。
* 今回は新しいCPU profile・JIT比較・通常計時を実施していない。高速化が確定した新案も、最適化余地が尽きたという結論もない。

## レビュー時の検証と対応順の提案

SDK 10.0.401・.NET 10.0.12で、現在のソースのRelease solution build成功（既存SpracheJSON由来NU1701が1件）、全323テスト成功（本体260・JSON63、失敗・Skipなし）。
最初の制限環境内のno-buildテストは検出後に進まないため中断し、制限外でbuildと全テストを実行して確認した。中断した試行を成功扱いしない。
format/SARIFの個別再実行、性能測定、パッケージ生成はしていない。

機能診断は `artifacts/full-review-2026-10-23/` に、最小probeソース・観測JSON・build/testログを保存した。これはローカルのみでGitへ追加していない。
通常probeは現在のRelease DLLを参照して再ビルドし、各入力の受理、返却値、例外、非消費反復、比較用ノード共有を確認した。
JSONの規格差はRFC本文を根拠とし、System.Text.Jsonは追加の対照確認にだけ使用した。危険な入力を無制限に実行しない。

レビュー時にはR01/R02の終了性、R03/R04、R07/R08の検出力、R11～R14の比較基盤を扱う順序を提案した。
2026-10-25にR01のJSON側から着手した。各項目の対応記録と再開記録を現在の進捗とし、残る項目の優先度・仕様は個別に確認する。一覧にある全案を一括採用する意味ではない。
