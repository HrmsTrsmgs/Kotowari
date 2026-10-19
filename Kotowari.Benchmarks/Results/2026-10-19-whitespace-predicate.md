# JSON空白判定の重複削減 2026-10-19

JSONサンプルの4種類の空白を、4個のCharParserのOrではなく、既存ConditionalCharParserでまとめて判定した。PGO有効・MemoryStreamからの読み込みを含む既存JSON入力で、探索8組に続く独立確認8組でも約6.7%の短縮を確認した。事前の採用条件を満たしたため変更を残し、今回の試行を区切った。測定時点では未コミット。2026-10-19にユーザーがソース差分を確認してコミットを指示し、実装・テスト・本記録を同じコミットへ保存する。

ライブラリ本体の一般的な高速化ではなく、既存コンビネーターを使うJSONサンプルの構成改善である。先に試した[再帰定義の生成削減](2026-10-19-recursive-definition.md)と[文字列化の二重配列コピー削減](2026-10-19-char-materialization.md)は撤回済みで、この結果に混ぜていない。

## 実装と契約

変更前:

```csharp
static Parser<char> WhiteSpace =
    new OrParser<char>(
        new CharParser(' '),
        new CharParser('\t'),
        new CharParser('\r'),
        new CharParser('\n'));
```

変更後:

```csharp
// 候補ごとの文字取得を繰り返さず、JSONの空白4文字をまとめて判定する。
static Parser<char> WhiteSpace =
    new ConditionalCharParser(it => it is ' ' or '\t' or '\r' or '\n');
```

受理する文字はSPACE・TAB・CR・LFの4種類だけで、char.IsWhiteSpaceへの拡大ではない。成功時の文字・1文字の消費・失敗時のカーソルを維持する。EOFと入力内NULはいずれも失敗する。前後の空白をどこで消費するかはWithWhiteSpaceParser／WordParserの既存処理に任せ、公開APIや失敗規則を変えていない。既存部品の組み合わせだけを変更し、本体の解析処理を手書き展開していない。

## 生成コードによる根拠

基準のTier 1ではOr/Charがガード付きでinline化されているが、空白候補の配列走査・候補ごとの型ガード・Current取得と範囲判定・IgnoreCase分岐が残る。したがって削減対象は「virtual呼び出し4回」ではない。非空白では4候補、TABでは2候補、CRでは3候補、LFでは4候補を試す処理が対象になる。SPACEでは基準も最初の候補で成功する。

今回の基準JIT `diagnostics/jit/baseline/capture-01/jit.asm:41725` と候補 `diagnostics/jit/candidate/capture-01/jit.asm:41881` のWithWhiteSpaceParser<char>.ParseCoreを比較した。候補の空白判定経路にはOrの候補配列走査がなく、1回のCurrent取得・境界判定からpredicateの型ガードと呼び出しへ進む。候補predicateのTier 1（同ファイル40336行）はTAB/LF/CRの範囲・bitmap判定とSPACE比較で、指定した4文字だけを受理する。

メソッド全体のnative code sizeは1,182から585 bytesへ変化したが、コード量の比率を速度効果とは扱わない。通常計時で評価するのは変更後のサンプル全体であり、Digitで使う別predicateとのprofile共有等の影響も含む。JIT採取は両版とも別プロセスで正常終了し、診断付きの時間は速度比較へ含めていない。今回新たなCPUサンプリングは実施していない。

## 測定条件

基準コミットは `d2f7aa8915ca1f580fc009b0a5d88d7b3c9d1ec1`。入力は既存 `Data/test.json`、4,172,062 bytes、SHA-256 `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。MemoryStreamの作成・コピー、UTF-8からstringへの復号、全文解析と結果木生成を含む。物理ディスク読み取りは含めない。両版で全文消費・100,001ノード・System.Text.Jsonとの木の一致を確認した。System.Text.Jsonは結果検証用で、速度の比較対象ではない。

SDK 10.0.401、.NET 10.0.12、BenchmarkDotNet 0.15.8、Windows 25H2 build 26200.9711、i7-13700H。PGO有効、affinity mask 4、warmup 16、Actual 8、invocation 4、unroll 1、DontRemove。各系列は新規プロセス8組16起動、AB/BA交互順。各起動の8 Result平均を単位として、8個の対応log時間比の95% t区間（自由度7、t=2.364624251）を求めた。

探索で区間上限が1未満なら独立8組へ進み、確認でも上限が1未満の場合だけ採用する。途中結果による組数変更・外れ値除外は行わず、探索と確認は合算しない。実行中に親のビルド・テストを重ねず、各ブロック前にAC接続・省電力無効・スリープ抑止helperのheartbeatを確認した。

## 結果と判断

| 系列 | 組数 | 基準 ms/解析 | 候補 ms/解析 | 対応時間比 | 95%対応log-t区間 |
|---|---:|---:|---:|---:|---:|
| 探索 | 8 | 134.385 | 127.628 | 0.949357 | [0.921351, 0.978214] |
| 独立確認 | 8 | 136.728 | 127.568 | 0.933078 | [0.921385, 0.944921] |

独立確認では約6.69%短縮、区間は約5.51〜7.86%短縮で、採用基準を満たした。平均割り当ては基準49,845,275、候補49,844,642 bytes/解析で同水準。この案は解析中の割り当て削減ではなく、空白候補ごとの判定の重複を減らすものとして評価する。この入力・環境での結果を、別の文法や入力全般へ一般化しない。

全32起動が正常終了し、各Actual/Resultは8反復、各反復4操作だった。入力とDLLのハッシュを確認した。本体DLLは両版とも `711A6D91E2BF0132ABB50DEC81A5B80555CDDF2BD0F4D3B8F5C849AC2F676BA8`、候補JSON DLLは `BAD351B1D5A6673C2096A4D0C2E04C4C2743B73EBAF70E1135890AF9504F4418`。固定hostの28 DLLのうちJSONサンプルだけが異なる。

## 検証と記録

空白4種類の混在、VT/FF/NBSP/NULの拒否、空白だけの入力という6ケースを既存JSONテストへ追加した。変更前のJSON63件、変更後の全311件（本体248、JSON63）が成功し、失敗・Skipはない。全テストのコンソール記録は保存したが、共通TRX名のためJSON側の変更後TRXは本体側に上書きされた。保存済みTRXだけで両プロジェクトを確認したとは扱わない。

コメント位置を修正した最終ソースの非増分Releaseビルドとアナライザーは9.66秒で成功。テスト後の変更はコメント移動だけで、同じテストを形式的には再実行していない。既存NU1701とSARIFの提案6件が残る。初回formatの新規整形指摘2件はコメント位置を修正して解消し、最終formatは従来と同じ66件の既存指摘でexit 2。位置番号を除いた内容と件数の一致を確認した。既存のreadonly提案などは本案へ混ぜていない。

[証拠JSON](experiments/2026-10-19-whitespace-predicate.json)に両版のソース・DLL、候補差分、全観測、系列別集計、検証、JIT抜粋、再生スクリプトを保存した。原本は `artifacts/string-whitespace-predicate-2026-10-19/`。保存資料だけからのクリーン再構築・再実行は未検証。作業全体のスリープ抑止は、先行候補から継続した `artifacts/string-recursive-definition-2026-10-19/power-02/` で管理した。03:51:06 UTCにStopFileで解除し、取得時と同じスレッド、ReleaseReturn=2147483649、Error=nullを確認した。

## 今回の変更ファイル

実装・テスト:

- `JsonParser/JSON.cs`
- `JsonParserのテスト/JSONのテスト.cs`

採否と根拠の記録（以下は `Kotowari.Benchmarks/Results/` 内）:

- `2026-10-19-whitespace-predicate.md`、`experiments/2026-10-19-whitespace-predicate.json`
- `2026-10-19-recursive-definition.md`、`experiments/2026-10-19-recursive-definition.json`
- `2026-10-19-char-materialization.md`、`experiments/2026-10-19-char-materialization.json`
- `2026-10-19-next-candidates-investigation.md`（Sequence案の設計負担・テスト不足と空白判定の根拠を追記）
- `README.md`（結果への索引）

測定終了時点では、開始時から存在したその他の未コミット記録を保持し、コミット・pushは行っていなかった。保存前に固定候補のソースmanifest 43件と現在のファイルのハッシュ一致を確認した。全311テスト・Releaseビルド・アナライザーの確認後は文書だけの変更のため、同じ検証を再実行していない。先行6候補の調査・不採用記録は `76449ff` に保存した。
