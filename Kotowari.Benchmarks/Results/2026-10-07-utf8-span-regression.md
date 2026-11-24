# Span借用導入後の速度低下・原因切り分け（2026-10-07）

## 状態

大幅な速度低下の主因は、共通のCursolへReadOnlySpanを内包させ、そのCursolをParseResultに含めて値で受け渡す形にしたことで、JITの生成コードに中間構造体のコピー・初期化が多数残るようになったこと。UTF-8復号だけでなく文字列版まで遅くした理由もこれで説明できる。ライブラリ本体・公開API・テストにはこの調査で変更を加えていない。

ref struct化だけの対照では約2倍の悪化は起きず、現行と同じ型サイズ・ゼロコピー・DocumentのSpan引数を維持した平坦な診断版では、両入力経路の大きな後退が縮小した。対応するTier 1コードで中間コピーが消えたことも確認した。この組合せを主因特定の根拠とし、構造体のサイズやコードサイズの数字だけから原因を推測していない。

early struct promotionの入れ子型制約はこの結果と整合する。ただし個々のJIT内部パスの実行ログを採ったわけではないため、「どのパスだけで全損失が決まったか」までは断定しない。

## 通常計時の結果

各4独立起動。時間は既存4.17 MB JSON一つのMemoryStream読み取り準備・解析・全結果木生成あたり。SD/CVは起動平均どうしの標本統計で、同一起動の8反復を8独立起動として扱っていない。

| 条件 | 平均ms/JSON | 起動平均のSD ms | 起動間CV | 割り当てMiB/JSON |
|---|---:|---:|---:|---:|
| 文字列・変更前 | 142.69 | 2.83 | 1.98% | 61.51 |
| 文字列・ref struct化のみ | 135.62 | 1.70 | 1.25% | 61.51 |
| 文字列・現行のSpan内包 | 317.64 | 2.91 | 0.92% | 61.52 |
| 文字列・同サイズの平坦な診断版 | 167.63 | 5.89 | 3.51% | 61.51 |
| UTF-8・現行のSpan内包 | 359.73 | 12.16 | 3.38% | 53.56 |
| UTF-8・同サイズの平坦な診断版 | 191.16 | 8.62 | 4.51% | 53.56 |

各組で対応させた幾何平均時間比（分子/分母、1より大きいと分子が遅い）:

| 比較 | 時間比 | 95% t区間（df=3） |
|---|---:|---:|
| ref-only文字列 / 変更前文字列 | 0.95 | 0.92–0.98 |
| 現行文字列 / 変更前文字列 | 2.23 | 2.17–2.28 |
| flat文字列 / 現行文字列 | 0.53 | 0.50–0.56 |
| flat文字列 / 変更前文字列 | 1.17 | 1.10–1.25 |
| flat UTF-8 / 現行UTF-8 | 0.53 | 0.51–0.56 |
| flat UTF-8 / 変更前文字列 | 1.34 | 1.22–1.48 |

現行からflatへの時間比は両経路とも約0.53。結果の生成内容・型サイズ・入力の借用・DocumentのSpan引数を維持して、この差が出ている。マネージド割り当ても実質同程度で、全文コピーを復活させて得た改善ではない。

一方、flat文字列は変更前比で約17%、flat UTF-8は変更前文字列比で約34%遅く、95%区間でも残差がある。「主因が分かった」と「UTF-8が元の文字列版と同等になった」を混同しない。この24起動では旧UTF-8版を追加測定していないため、前回の181.445 msと今回の191.16 msだけから旧UTF-8との有意差を判断しない。

区間は対応する起動平均の対数比を用いたt推定であり、別マシン・別入力・別ランタイムへ一般化するものではない。数値の丸め前の値と各対応組は `final-evidence.json` に保持する。

## 調べる問題

[直前の測定](2026-10-07-utf8-span-borrowing.md)では、既存4,172,062 bytesのJSONの読み込み準備・全解析・結果生成が、文字列144.355→322.828 ms、UTF-8 181.445→367.434 msへ遅くなった。PGO有効、各8新規プロセスの対応比でも大幅な遅延が残った。

基点は `defe7830d2e11b9761f0902e2ad9f0b08acc96b6`。変更後は直前の記録に本体差分・SHA-256を保存したSpan借用実装。今回も同じ凍結済みソースと同じJSON入力を使用する。

文字列経路では文法、入力の復号、StringDocumentの文字読み取り本体は変わっていない。共通変更は、CursolへのSpanフィールド追加、Cursol/ParseResultのref struct化、DocumentへのSpan引数追加。これらを切り分ける。

## 確認したTier 1生成コード

独立した型サイズ確認プログラムの `Unsafe.SizeOf<T>()` で次を確認した。

| 型 | 変更前B | 現行B |
|---|---:|---:|
| Cursol | 16 | 32 |
| ParseResult<char> | 24 | 40 |
| ParseResult<string> | 32 | 48 |

PGO有効の文字列版のCharParser.ParseCoreでは、変更前後ともStringDocumentの型guardとGoFowardのインライン展開が残っている。ヘッダーのinline数も同じ。一方、変更後には成功・失敗どちらにも構造体全体のスタックコピーと初期化が追加されている。

変更前の成功・失敗が合流する結果返却部分:

```asm
mov word ptr [rbx], r14w
mov byte ptr [rbx+0x02], al
mov gword ptr [rbx+0x08], rdi
mov dword ptr [rbx+0x10], ebp
```

変更後の成功経路に現れるコピーの一部（間の分岐などは省略）:

```asm
vmovdqu ymm0, ymmword ptr [rbx]
vmovdqu ymmword ptr [rsp+0xA8], ymm0
vmovdqu ymm0, ymmword ptr [rsp+0xA8]
vmovdqu ymmword ptr [rsp+0x88], ymm0
vmovdqu ymm0, ymmword ptr [rsp+0x80]
vmovdqu ymmword ptr [rsp+0xC8], ymm0
```

この後も結果を呼び出し元へ転送する32+8 bytesのコピーがある。失敗側も同様のコピーを経由する。UTF-8のCharParser.ParseCoreにも同じコピー列を確認した。UTF-8ではReadOnlyMemoryからSpanを取得する判定は減っており、入力参照の簡略化と結果の受け渡しの悪化が同時に起きている。

| 文字列経路のメソッド | 変更前のローカルスタック確保B | 変更後B | 変更前コードB | 変更後B |
|---|---:|---:|---:|---:|
| CharParser.ParseCore | 40 | 240 | 211 | 494 |
| Cursol.GoFoward | 40 | 80 | 84 | 136 |
| OrParser<char>.ParseCore | 88 | 360 | 379 | 706 |
| ExpectCharParser.ParseCore | 104 | 344 | 502 | 770 |
| SequenceParser<Canon,char>.ParseCore | 200 | 984 | 1,347 | 2,138 |
| ZeroOrMoreParser<char>.ParseCore | 72 | 632 | 381 | 1,709 |

スタック確保は `sub rsp` の値で、保存レジスタなどを含む総フレームサイズではない。コードサイズの増加だけを速度低下の証拠にはしない。ZeroOrMoreではPGOが展開した子パーサーの型も異なるため、このメソッドだけの差を今回の全原因へ帰属させない。

### CPUサンプルとの対応

別起動のEventPipe診断では、Actual区間だけを対象に、変更前662・変更後1,394の実測スレッドサンプルを取得した。各32 Actual区間、欠落イベント0・スタック欠落0。

| メソッド | 変更前Selfサンプル | 変更後Selfサンプル |
|---|---:|---:|
| SequenceParser<Canon,char>.ParseCore | 12 | 175 |
| OrParser<Canon>.ParseCore | 1 | 237 |
| StringDocumentの読み取り | 30 | 25 |

共通コンビネーター側へCPU負担が増えた方向を示している。ただしサンプル数は厳密な所要時間ではなく、起動時間・JITの展開先も変わる。特にOrParser<Canon>は、変更前後でPGOが選んだ型とinline treeが異なる。ここでのSelf増加の全量を構造体コピーのせいとは断定しない。因果の切り分けには次の対照実験を使い、生成コードでは同じinline数・型guardを持つCharParserを中心に比較する。

上位件数による省略を除き、診断用TraceAnalysisコピーで既存ETLXを全件再集計し、`string-profile-{before,after}/analysis-full.json` に保存した。Selfは変更前44・変更後41メソッド、Inclusiveは63・58メソッド。採取や計時はやり直していない。

[.NET 10.0.12のearly struct promotion](https://github.com/dotnet/runtime/blob/v10.0.12/src/coreclr/jit/lclvars.cpp)は、通常の入れ子構造体について、複数フィールドを再帰的に分解する扱いに制約がある。旧Cursolは文書参照と位置だけだったが、現在は2フィールドのSpanを内包する。これでCursolのearly promotion条件が変わる。ただし、[physical promotion](https://github.com/dotnet/runtime/blob/v10.0.12/src/coreclr/jit/promotion.cpp)で補える場合もあり、このソース条件だけを今回の遅延原因の確定証拠にはしない。旧ParseResultも複数フィールドのCursolを内包していた点を区別する。

## 対照実験

製品コードの書き換えではなく、Git対象外の診断用コピーで次を比較する。

- 変更前: 上記基点の実装。
- ref-only: 変更前のCursolとParseResultをref structにする。Spanをフィールドへ追加せず、元のDocument署名・ReadOnlyMemory保持・JSON入口コピーを維持。record struct由来の等価性は現行と同じ明示実装にする。
- 現行: 直前に測定したSpan借用実装。
- flat-current: 現行の入力参照と長さを、Spanの入れ子フィールドではなく、`ref readonly byte`と整数フィールドで保持する。使用時にSpanを再構成し、DocumentのSpan引数、ゼロコピー、パーサー構成を維持する。長さをlongにするのは現行と32 bytesのサイズを合わせる診断上の対照のためで、製品のAPI案ではない。

flat-currentはMemoryMarshalを使う実験であり、そのまま採用する方針ではない。原因の切り分けと、ゼロコピーを保って速度低下を避けられる可能性の確認に使う。

### 同サイズの診断版で消えたコピー

flat-currentのTier 1では、CharParserの引数Cursolをフィールドごとにレジスタへ読み、成功・失敗の共通返却部分で結果へ直接格納する形になった。現行の32 bytes Cursolと40 bytes ParseResult<char>を一時スタック領域間でコピーする連鎖は消えている。文字列版の返却部分:

```asm
mov word ptr [rbx], r13w
mov byte ptr [rbx+0x02], al
mov gword ptr [rbx+0x08], rdi
mov bword ptr [rbx+0x10], rbp
mov qword ptr [rbx+0x18], r14
mov dword ptr [rbx+0x20], r15d
```

UTF-8版もレジスタの割り当て以外は同じ6ストアの形になった。Cursol.GoFowardもカーソル全体の中間コピーではなく、各フィールドを直接返却先へ格納する。参照先のJSON本文をコピーしているわけではなく、参照・長さ・位置などを持つ構造体の頻繁な受け渡しが問題だった。

| 経路・メソッド | 現行スタックB | flatスタックB | 現行コードB | flatコードB | call数（現行/flat） |
|---|---:|---:|---:|---:|---:|
| 文字列 CharParser.ParseCore | 240 | 56 | 494 | 316 | 5 / 5 |
| UTF-8 CharParser.ParseCore | 272 | 88 | 1,238 | 1,034 | 8 / 8 |
| 文字列 Cursol.GoFoward | 80 | 56 | 136 | 142 | 1 / 1 |
| UTF-8 Cursol.GoFoward | 96 | 72 | 223 | 246 | 3 / 3 |

CharParserのPGOありinline数は文字列3/3、UTF-8 10/10、PGOなしinline数はいずれも1/1。single-block inlineは文字列11→17、UTF-8 49→55で同一ではなく、flatではSpanを作り直す補助処理が加わる。パーサーを手動展開したり、呼び出し数を減らす変更はしていない。GoFowardのコードサイズはむしろ増えており、コード量の大小を原因として判断してはいない。

各抽出対象は各asm内でTier 1が1版。本文は `codegen-extracts/{current-string,current-utf8,flat-string,flat-utf8}-{CharParser.ParseCore,Cursol.GoFoward}.Tier1.asm`、機械集計は同ディレクトリの `index.json` に保存する。

## 診断条件・証拠

- 入力SHA-256: `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。既存ファイルを変更・縮小していない。
- 通常計時: Release、Dynamic PGO=1、LaunchCount=1、WarmupCount=16、IterationCount=8、InvocationCount=4、UnrollFactor=1、DontRemove、Affinity=4。各比較を4組の新規プロセスで直列実行し、奇数組と偶数組で実行順を逆転する。
- 奇数組は文字列・変更前 → ref-only文字列 → 文字列・現行 → flat文字列 → UTF-8・現行 → flat UTF-8、偶数組はこの逆順。6条件×4組で24独立起動。
- 通常計時にはJIT診断・プロファイラーを付けない。起動平均を独立試行として対応付き対数時間比と自由度3の95% t区間を計算する。
- Tier 1診断は同じ条件へ `DOTNET_JitDisasm=Marimo.Kotowari!*` と出力ファイル指定を加えた独立起動。CPUサンプルは文字列の変更前後を各1起動、Actual=32、他は同じ条件でEventPipe採取する。
- 元証拠は `artifacts/utf8-span-regression-measurement-2026-10-07/`。変更前後の生成コードは `retry-01/{string-before,string-after,utf8-before,utf8-after}/jit.asm`。
- 最初のsandbox起動はWMIのPermissionDenied (0x80041003)でbenchmark 0件、asmなし、有効計時なしだった。必要な権限を付けた再実行をretry-01へ分離した。

### 環境・測定範囲

- .NET SDK 10.0.401、実行ランタイム .NET 10.0.12、BenchmarkDotNet 0.15.8、X64 RyuJIT、Concurrent Workstation GC。
- Windows 11 25H2 build 26200.9457、Intel Core i7-13700H。PGO無効の計時は行っていない。
- 既存のJsonStreamBenchmarksをそのまま使用。MemoryStreamから読み取り用バッファへのコピー、文字列経路のUTF-8→string変換、完全なJSON結果の構築を含む。物理ディスクI/OはGlobalSetupで済ませるため、計時には含まない。
- 両診断版のReleaseビルドは警告0・エラー0。String/UTF-8両経路の結果ツリーをSystem.Text.Jsonと比較し、100,001ノードの同等性を確認した。これは比較の正当性確認であり、製品としての境界条件全般のテストではない。
- ref-onlyの型サイズは16/24/32 bytes、flat-currentは32/40/48 bytes（Cursol / ParseResult<char> / ParseResult<string>）。flat-currentと現行は同じサイズ。

## 診断ソースの再構築

診断版はGit対象外の `artifacts/utf8-span-regression-measurement-2026-10-07/variants/{ref-only,flat-current}/` に全ソース・ホスト・manifestを保存した。今後そのディレクトリがなくても再構築できるよう、変更をここに示す。基点と現行の3ファイル差分・ハッシュは[直前の記録](2026-10-07-utf8-span-borrowing.md)を使う。改行CRLF・UTF-8 BOMで保存したソースファイルのSHA-256は以下。

| 版 | 置換するファイル | SHA-256 |
|---|---|---|
| ref-only | Kotowari/Core/Cursol.cs | `554390811BAF5A39EEA842D784891947E4D5F8C0A375A7C74728569981260EE5` |
| ref-only | Kotowari/ParseResult.cs | `BD401AD4C3861157DA16742E6825E3A8141A34B2EC02C746C582E1F377AC6BC9` |
| flat-current | Kotowari/Core/Cursol.cs | `55FF5EFEF9C523A2E18E84F8C1AA0383DBB62C8D32A1960E1908744A815C1215` |

### ref-only

基点の `public readonly record struct Cursol` を `public readonly ref struct Cursol` に、`public readonly struct ParseResult<T>` を `public readonly ref struct ParseResult<T>` に変更する。Cursol.Copyの直後へ次を追加する。それ以外の本体・JSONソースを変更しない。

```csharp
    public bool Equals(Cursol other)
        => ReferenceEquals(document, other.document) && Index == other.Index;

    public override bool Equals(object? obj) => false;

    public override int GetHashCode()
        => HashCode.Combine(document, Index);

    public static bool operator ==(Cursol left, Cursol right) => left.Equals(right);
    public static bool operator !=(Cursol left, Cursol right) => !left.Equals(right);
```

### flat-current

現行のCursol.csへ次の変更だけを行う。`using System.Text;` の直後に `using System.Runtime.InteropServices;` を追加する。`readonly ReadOnlySpan<byte> utf8;` を次で置き換える。

```csharp
    // Diagnosis only: four scalar fields permit the early promotion path.
    // Keep the input length as long so Cursol has the same 32-byte size as the Span-containing control.
    readonly ref readonly byte utf8Reference;
    readonly long utf8Length;

    ReadOnlySpan<byte> utf8
        => MemoryMarshal.CreateReadOnlySpan(in utf8Reference, (int)utf8Length);
```

文字列コンストラクターの `utf8 = default;` を次で置き換える。

```csharp
        utf8Reference = ref MemoryMarshal.GetReference(ReadOnlySpan<byte>.Empty);
        utf8Length = 0;
```

UTF-8コンストラクターの `this.utf8 = utf8;` を次で置き換える。

```csharp
        utf8Reference = ref MemoryMarshal.GetReference(utf8);
        utf8Length = utf8.Length;
```

MemoryMarshalによるSpan再構成は、参照と長さの整合性をこの型が管理する診断用実装。longによるサイズ合わせも含め、そのまま本体へ採用する提案ではない。

## 検証と変更範囲

- 通常24起動すべてでActual/Result各8反復、Release、PGO=1、指定affinity、電源プラン復帰を確認した。flatの生成コード診断2起動も完了した。
- 終了時の本体3ファイルは凍結済みcurrentとSHA-256が一致する。
- ref-onlyは対象2ファイルを除く22個のC#ソースがbaselineと一致。flat-currentはCursol以外の23個がcurrentと一致。4ホストのJsonStreamBenchmarks.csも同一ハッシュ。`source-identity.json` に照合結果を保存した。
- 本体・公開API・テストは本調査では未変更。前回の未コミット実装・テスト差分を維持している。本調査の製品コード変更がないため、全テスト・ソリューションアナライザー・format検査は再実行していない。前回記録の旧WordParser測定コードによるソリューションビルド失敗も未解決で、今回解消したものとは扱わない。
- 本調査のGit対象は本記録、Results/README、前回記録からのリンク、既存4 CSVのみ。診断用コピー・ホスト・asm・トレース・一時スクリプトはGit対象外のartifactsに保存する。ステージ・コミット・pushは行っていない。

## データの保存と照合

実験IDは `2026-10-07-utf8-span-regression`。既存の `word-parser/` 配下へ追記し、新しいCSVは増やしていない。

| CSV | 追記行数 | 最終データ行数 |
|---|---:|---:|
| summary.csv | 24 | 592 |
| launches.csv | 24 | 758 |
| iterations.csv | 384 | 13,876 |
| stacks.csv | 206 | 735 |

iterationsは192 Actual + 192 Resultで、384独立試行ではない。stacksは今回の2プロファイルのSelf/Inclusive全件で、Variantを `string-profile-before` / `string-profile-after` に分けた。診断付き起動の時間は通常24起動へ混ぜていない。

4 CSVとも追記前のファイルを `csv-before/` へ保存し、元領域のbyte-prefix完全一致を確認した。新規summary/launches/iterationsの各セルを測定JSONと、stacks全206行を `analysis-full.json` と照合して一致した。証拠は `csv-update-evidence.json` と `final-evidence.json` にある。CSV追記後の証拠JSON生成は初回にPowerShellのboolean記述誤りで失敗したが、追記を繰り返さず、独立した証拠生成・照合スクリプトで完了した。

記録・README・CSVの差分確認とUTF-8/BOM/改行確認を行い、既存形式を維持した。`git diff --check` 成功。コミット候補は本記録・Results/README・前回記録へのリンク・4 CSV、メッセージ案は「Span借用後の性能低下の原因調査を記録」。ただし各CSV等には前回からの未コミット結果も含まれるので、コミット時はその作業単位も確認する。

## 次に修正を検討する範囲

今回の大幅な後退は、UTF-8入力やSpan借用そのものが成立しない証拠ではない。入力バイト列のコピーを復活させずに後退を大きく減らせた。ただし診断版の速度を、元の文字列版と同等になった証拠とは扱わない。今回のASCII入力以外の性能も未確認。

修正対象は、借用入力を含む共通カーソルと結果型の頻繁な値渡し。小さい候補として入力側の読み取り専用参照渡しは検討できるが、今回確認したコピーは結果生成・返却にもあるため、それだけで解消すると予測しない。入力の保持と解析位置の受け渡しを分け、内部結果に入力記述子を毎回含めない設計も検討対象になるが、これは複数パーサーへ及ぶ変更であり、最小修正と称して一括実装しない。

flat-currentの低レベル表現をそのまま採用するかも含め、製品側の修正は別途判断する。本調査ではパーサーの手動インライン化、独自ASCII復号、PGO無効化を解決策にしていない。
