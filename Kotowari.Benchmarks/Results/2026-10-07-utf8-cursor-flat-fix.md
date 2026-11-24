# Span借用カーソルの値渡し修正（2026-10-07）

文字列は317.12→171.63 ms、UTF-8は356.44→189.57 msへ改善した。PGO有効の20独立起動で、修正前との対応時間比の95%区間も両経路で1を下回った。確認したCharParserの生成コードでは、狙ったカーソル・解析結果の中間コピー列が消えた。

ただし、Span借用対応前の文字列版143.70 msには戻っていない。対応時間比で修正後の文字列は約19%、UTF-8は約31%遅く、UTF-8を元の文字列版と同等にする目標は未達。起動間のばらつきも残る。

## 対象と実装

[原因調査](2026-10-07-utf8-span-regression.md)で確認した、ReadOnlySpanを内包するCursol・ParseResultの中間コピーを減らすための本体修正。今回の製品コード変更は `Kotowari/Core/Cursol.cs` のみ。

UTF-8入力を先頭への `ref readonly byte` と `int` の長さとして保持し、読み取り時に `MemoryMarshal.CreateReadOnlySpan` でSpanを再構成する。両フィールドは同じ入力Spanから取得し、その後変更しない。入力を借用する公開API、文書の同一性、位置、成否、値、Runeでの復号とDocumentの役割は維持する。解析入口の全文コピーは復活させていない。

先頭参照はGCが追跡するmanaged byrefで、raw pointerやpinningは使わない。参照先と長さの整合性は非公開コンストラクターで保ち、借用の寿命はref structの制約に従う。MemoryMarshal自体が長さを検証するわけではないため、この不変条件をフィールドのコメントにも残した。

原因切り分け用の32 bytes版では長さをlongにしていたが、製品ではReadOnlySpan.Lengthと同じintを使う。サイズ合わせ用のpaddingやStructLayoutは追加していない。パーサーの手動インライン化、呼び出し位置の変更、新しいASCII復号も行っていない。

## 版と再構築

変更前は、[Span借用導入記録](2026-10-07-utf8-span-borrowing.md)に保存した3ファイルの差分を基点 `defe7830d2e11b9761f0902e2ad9f0b08acc96b6` へ適用した実装。今回の修正はそのCursolに以下を適用する。

1. `using System.Runtime.InteropServices;` を追加する。
2. `readonly ReadOnlySpan<byte> utf8;` を下記のフィールドとプロパティへ置き換える。
3. CurrentとGoFowardがDocumentへ渡す入力を `Utf8` にする。
4. 文字列コンストラクターは `utf8Reference = ref MemoryMarshal.GetReference(ReadOnlySpan<byte>.Empty); utf8Length = 0;`、UTF-8コンストラクターは `utf8Reference = ref MemoryMarshal.GetReference(utf8); utf8Length = utf8.Length;` とする。

```csharp
    // 入力をコピーせず借用する。参照と長さは、必ず同じSpanから取得して以後変更しない。
    // Spanをフィールドへ内包すると、カーソルと解析結果の値渡しに中間コピーが残るため、
    // 入力記述子を独立したフィールドとして保持し、読み取り時だけSpanを再構成する。
    // managed byrefなのでGCによる移動に追従し、寿命はCursolのref struct制約に従う。
    readonly ref readonly byte utf8Reference;
    readonly int utf8Length;

    ReadOnlySpan<byte> Utf8
        => MemoryMarshal.CreateReadOnlySpan(in utf8Reference, utf8Length);
```

測定した本体3ファイルのSHA-256（UTF-8 BOM・CRLF）:

| ファイル | SHA-256 |
|---|---|
| Kotowari/Core/Cursol.cs | `6589EB6DCF21C7F9FFC7C178C0B7892556B1237CEA0A27C8C1A277D6E17C723B` |
| Kotowari/ParseResult.cs | `D2B0FE51DF1511A11044C591CE44E4BF2FF4609B1ED3B57AEC9A09C9E8F87ABC` |
| JsonParser/JSON.cs | `740F2C0ADB797C2291BB73E5FFFE62A716FF66288F9B57B4E4F551D55670E259` |

本体・JSON・独立ホストの凍結コピー、manifest、測定ログ、完全JSON、生成コード、借用検査プログラムは `artifacts/utf8-cursor-flat-fix-2026-10-07/` に保存する。測定準備・実行・機械的な検証・集計はLuna medium、変更と採用判断は進行役が担当。

## 検証

振る舞いを維持する性能修正として、既存テストを基準に検証した。時間の大小を通常の単体テストに埋め込む形式的なRedは作っていない。

- 対象テスト（Cursol/ParseResult）: Release 27成功、失敗・Skipなし。
- 全テスト: Releaseでライブラリ210・JSON115、合計325成功。既存のテストの値・成否・カーソル・例外・stackallocスライスの借用確認を変更していない。
- 指定ソリューション非増分アナライザービルド: 20.34秒、失敗。旧WordParserBenchmarksとWordParserAccumulationBenchmarksのCursolフィールドでCS8345が2件。これは修正前から確認済みのSpan借用型との非互換で、旧測定コードは未変更。既存SpracheJSONのNU1701も残る。本体・JSON・両テストのコンパイルは成功した。
- SARIF: 本体の既存IDE0090×2、テストIDE0200×1、JSONの既存IDE0066・CA1854・CA1862のみ。今回変更箇所の診断なし。
- 指定dotnet format verify: 失敗（終了コード1）。既存WHITESPACE、IDE0290、IDE0044等が残る。Cursolの空コンストラクターのWHITESPACE指摘も以前からの未変更行で、今回の追加フィールド・プロパティに新しい指摘はなかった。

借用の追加検査は、凍結した修正後のCursol・ParseResultソースを組み込む独立プログラムで実施した。

- 空およびdefaultのReadOnlySpan、stackallocのASCII、BMP文字の非先頭スライス: Current・GoFoward・Copyが期待値と一致した。
- 呼び出し側で配列を別変数として保持せず、配列の非先頭スライスからカーソルを返す構成: compacting GCを2回要求した後も、読み取り・位置移動・コピー・文書同一性の比較が成功した。個々の配列が実際に移動したことまでは計測していない。
- 呼び出し元のReadOnlySpan引数を借用してCursol・ParseResultを返すコード: コンパイル成功。
- ローカルstackallocからCursol・ParseResultを返すコード: CS8352・CS8347で拒否された。寿命を越える借用の禁止を確認する、意図したコンパイル失敗である。

X64での実測サイズ（Unsafe.SizeOf）:

| 型 | Span借用対応前 | 修正前（Spanを内包） | 修正後（参照とint長） |
|---|---:|---:|---:|
| Cursol | 16 bytes | 32 bytes | 24 bytes |
| ParseResult<char> | 24 bytes | 40 bytes | 32 bytes |
| ParseResult<string> | 32 bytes | 48 bytes | 40 bytes |

## 巨大JSON測定の条件

- 入力は既存 `Kotowari.Benchmarks/Data/test.json`、4,172,062 bytes、SHA-256 `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。ASCIIのみ・BOMなし。縮小・再生成しない。
- 共通のJsonStreamBenchmarksでMemoryStreamからバッファへの読み取り準備、解析、全結果木生成を含める。文字列経路はUTF-8→string変換も含む。物理disk I/OはGlobalSetupで済ませ、計時には含まない。
- 5条件×4組、20新規プロセスを直列実行する。奇数組は元の文字列版 → 修正前文字列 → 修正前UTF-8 → 修正後文字列 → 修正後UTF-8、偶数組は逆順。
- Release、Dynamic PGO=1、LaunchCount=1、WarmupCount=16、IterationCount=8、InvocationCount=4、UnrollFactor=1、OutlierMode=DontRemove、Affinity=4。通常計時にはJIT診断・プロファイラーを付けない。
- 解析した結果ツリーの全100,001ノードと全値をSystem.Text.Jsonへ比較してから測定する。System.Text.Jsonは正当性確認用で、性能の主比較対象ではない。
- 各組の起動平均を対応させた対数時間比から、幾何平均と自由度3の95% t区間を計算する。起動内の8反復を8独立起動とは数えない。
- 修正後の文字列/UTF-8のTier 1診断は別起動で各1回行う。通常時間へ混ぜない。

環境はSDK 10.0.401、.NET 10.0.12（10.0.1226.42308）、BenchmarkDotNet 0.15.8、Windows 11 25H2（10.0.26200.9457）、Intel Core i7-13700H、X64 RyuJIT。既存ホストのJsonStreamBenchmarksはSHA-256 `1FF949FB7D5DB1BA115A82F47DCC4CF54F1EEFCFD6A63D16BD27A691FD425383` と一致する未変更のソースを使用した。

## 通常計時の結果

各条件は4独立起動。時間は巨大JSON一つのMemoryStream読み取り準備・解析・全結果木生成あたり。SDとCVは、4つの起動平均どうしの標本統計である。

| 条件 | 平均ms/JSON | 起動平均のSD ms | 起動間CV | 割り当てMiB/JSON |
|---|---:|---:|---:|---:|
| 対応前の文字列版 | 143.70 | 2.27 | 1.58% | 61.51 |
| 修正前・文字列 | 317.12 | 4.62 | 1.46% | 61.51 |
| 修正前・UTF-8 | 356.44 | 8.27 | 2.32% | 53.56 |
| 修正後・文字列 | 171.63 | 9.28 | 5.41% | 61.51 |
| 修正後・UTF-8 | 189.57 | 20.53 | 10.83% | 53.56 |

組ごとに対応させた幾何平均時間比（分子/分母）と95% t区間:

| 比較 | 時間比 | 95%区間（df=3） |
|---|---:|---:|
| 修正後文字列 / 修正前文字列 | 0.541 | 0.487–0.601 |
| 修正後UTF-8 / 修正前UTF-8 | 0.530 | 0.462–0.607 |
| 修正後文字列 / 対応前文字列 | 1.193 | 1.071–1.330 |
| 修正後UTF-8 / 対応前文字列 | 1.314 | 1.115–1.548 |
| 修正後UTF-8 / 修正後文字列 | 1.101 | 0.888–1.365 |

修正前からは、文字列で約46%、UTF-8で約47%の時間短縮。割り当て量は実質同程度で、入力コピーの復活による改善ではない。一方、対応前文字列版との比は95%区間でも1を上回り、同等速度へ到達したとは扱わない。修正後のUTF-8と文字列の経路差については区間が1を含むため、この4起動ずつではどちらが速いと断定しない。

修正後UTF-8の起動平均は175.92–220.13 msで、起動間CVは10.83%。遅い起動も除外していない。今回の修正をばらつきの解消策とは扱わず、PGOで速い・遅い状態になる母集団の確率も、この4起動だけからは推定しない。残差の原因は本測定で追加特定していない。

区間は対応起動の対数比に基づくt推定で、起動平均の対数比の分布を用いるモデル上の区間である。別入力・別マシン・別ランタイムへの保証でも、任意の変更へ適用する固定の差のしきい値でもない。丸め前の数値と各起動の対応はfinal-evidence.jsonへ保存した。

## 修正後のTier 1生成コード

PGO有効の独立した診断起動を文字列・UTF-8で各1回行い、CharParser.ParseCoreとCursol.GoFowardの最終Tier 1を抽出した。各起動は8 Actual・8 Resultを完了したが、JIT診断付きの時間は通常計時の集計へ混ぜていない。修正前は、同一の凍結ソース・ランタイム・入力で[直前の原因調査](2026-10-07-utf8-span-regression.md)が保存したTier 1コードを参照した。

| メソッド・経路 | ローカルスタック確保B（修正前→後） | コードB（修正前→後） | call命令数（修正前→後） |
|---|---:|---:|---:|
| CharParser.ParseCore・文字列 | 240→56 | 494→251 | 5→4 |
| CharParser.ParseCore・UTF-8 | 272→88 | 1,238→1,031 | 8→8 |
| Cursol.GoFoward・文字列 | 80→56 | 136→136 | 1→1 |
| Cursol.GoFoward・UTF-8 | 96→72 | 223→245 | 3→3 |

ローカルスタック確保はsub rspの値で、保存レジスタを含む総フレームではない。コード量の増減を、そのまま速度の根拠とはしない。UTF-8のGoFowardはコード量が増えている。

CharParserでは両入力とも、成功・失敗に残っていたCursol・ParseResult全体の中間コピー列が消え、返却先へ各フィールドを直接書く形になった。入力本体のコピーの話ではなく、カーソルと解析結果という値型のコピーである。修正後の共通返却部分は次の6ストアになる（UTF-8版のレジスタ名）。

```asm
mov word  ptr [rbx], r13w
mov byte  ptr [rbx+0x02], al
mov gword ptr [rbx+0x08], rdi
mov bword ptr [rbx+0x10], r14
mov dword ptr [rbx+0x18], r15d
mov dword ptr [rbx+0x1C], esi
```

Documentへ渡すSpanの一時構築、例外経路のstackalloc、UTF-8復号処理は残る。あらゆる一時コピーやスタック初期化を消した、という主張ではない。

PGOに関係するinline数はCharParserの文字列で3→3、UTF-8で10→10。single-block inline数は11→17、49→55で、Span再構成に伴う展開が加わる。GoFowardもPGO付きinline数は文字列1→1、UTF-8 2→2のままである。ただしPGOの判断が全て同じだったとは言えない。文字列CharParserのReadは、修正前の型guard付き直接呼び出し・fallback仮想呼び出しから、修正後の一つの仮想呼び出しへ変わった。call数5→4はこれに対応し、製品コードでパーサーの処理を削除した結果ではない。GoFowardのStringDocument guardは修正後にも残る。CharParserのTier 1ヘッダーは修正前後ともSynthesized PGO表記だった。

中間コピー列の消失と実際の通常計時の改善が、先行調査の同サイズ対照実験とも整合することを、本修正を採用する根拠とする。今回の24 bytes版と前回の32 bytes平坦診断版は新たな対応比較をしていないので、int長への縮小だけの速度効果は断定しない。

抽出4ファイル、各起動のjit.asm、機械集計のdisasm-summary.jsonは、今回のartifactsルートに保存した。

## 記録とデータ照合

既存3種のCSVへExperiment `2026-10-07-utf8-cursor-flat-fix` として追記した。測定テーマ別の新しいCSVは作っていない。

| ファイル | 追記行 | 旧行数→新行数 | 元JSONと照合した新セル数 |
|---|---:|---:|---:|
| word-parser/summary.csv | 20 | 592→612 | 520 |
| word-parser/launches.csv | 20 | 758→778 | 380 |
| word-parser/iterations.csv | 320 | 13,876→14,196 | 4,800 |

iterationsの320行は、同じ160反復のActualとResultをそれぞれ保存したもの。320独立試行とは数えない。JIT診断起動と未完了の中断起動は含めていない。stacks.csvは今回分の追記なし、735行。現在のSHA-256は前回の追記後記録と一致し、143,127 bytesのファイル内容が不変であることも確認した。

追記前の3CSVをartifacts内へ退避し、追記後も元byte-prefixが全て不変であること、退避コピーと元SHAが一致することを確認した。追記済みCSVを独立した検査で読み戻し、元の20完全JSONから再生成した全5,700セルと一致、summary・launches・iterationsそれぞれの複合キーに重複なしを確認した。旧データの変換・丸め直し・削除はしていない。

凍結した修正前後の27ファイルも照合し、差分はCursol.csだけだった。本体3ファイルおよび入力の最終SHAは記録した値と一致する。機械的な検査の証跡は、preflight-evidence.json、source-identity-comparison.json、final-evidence.json、csv-postappend-verification.jsonにある。Results/README.mdのリンクと行数も更新した。

### 実行中の訂正と復帰

初回のサイズ速報は、ProbeSource切り替え後の--no-buildで以前の修正後probeバイナリを実行したため、修正前のサイズを誤帰属した。測定を一時停止し、正しいソースを再ビルドしたprobe、Compile対象パス、ソースSHA、各独立ホストのDLL SHAで版対応を確認して訂正した。測定ホストの修正前ソース・DLLそのものが修正後版に置き換わっていたわけではない。

完了済みの01-string-baseline・01-string-beforeは正常な版と8反復を確認して保持し、未完了の01-utf8-beforeだけをfailed-attemptへ保存して最初から実行し直した。run-summaryの共通after SHAという不正確な版ラベルも、各版のソースとDLL SHAへ補正した。訂正は版対応の記録であり、生ログ・完全JSONの計時値は変更していない。

中断起動は高パフォーマンスの電源設定を残していた。通常計時20起動のSetupは全て高パフォーマンスの同一GUIDであり、計時中の電源条件は一致していたが、最初の2起動以外はその高パフォーマンス設定へ復帰していた。通常計時後に、初回ログから確認した元のバランスGUID `381b4222-f694-41f0-9685-ff5bb260df2e` へ明示的に戻した。後続の診断2起動も元の設定へ復帰し、最終powercfg出力で一致を確認した。

## 完了範囲と残課題

本体の小さな表現変更、325テスト、巨大JSONの20通常起動、別起動のTier 1確認、バージョン保存、既存CSV追記と照合を完了した。本体・JSON・テストはビルドできるが、旧WordParser測定コード2か所のref struct非互換によるソリューションビルド失敗、既存のformat診断は未解決である。依頼外の旧測定コードやテストは変更していない。

対応前文字列版との性能残差、修正後の起動間変動、入力やランタイムが変わった場合の性能は残課題。新しいチューニングを混ぜず、今回の改善と未達部分を分けて記録した。ステージ・コミット・pushは行っていない。
