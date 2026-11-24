# UTF-8カーソル・次の変更候補の調査（2026-10-07）

本記録は実装前の調査時点の内容。後続の[ASCII文字幅直接取得の評価](2026-10-07-utf8-cursor-ascii-width.md)では改善を確認できず、実験変更を取り消した。下記の「次の変更案」は当時の候補であり、現在の実装予定ではない。

## 当初の結論と未確認事項

次の小さい変更候補は、`Utf8CharacterLength` でASCIIの文字幅を直接1と返すこと。ホットな呼び出し元のTier 1生成コードに、文字幅を取得するだけの `Rune.DecodeFromUtf8` 呼び出しが残っていた。

ただし、この処理が文字列版との差の全てを説明するとは判断しない。実装変更および変更後の速度測定はまだ行っていない。本記録は候補を選ぶための調査であり、改善実績ではない。

[直前の通常測定](2026-10-07-utf8-cursor-single-decode.md)では、UTF-8版180.961 ms、文字列版144.193 ms。起動ごとの対応を使った時間比は1.25498、95%区間は1.21564〜1.29559だった。実際の差が残っているため、その処理箇所を調べた。今回のプロファイル・生成コード取得時の所要時間を、この通常測定へ混ぜていない。

## 対象実装、入力、環境

- 対象: `defe783`（`UTF-8カーソルの二重復号を省く`）。調査中の本体・テストの変更なし。
- Cursol.cs SHA-256: `23F28F7808953A33809A6A85FF9D36A45059DA83A9DBA3ADD9CFB0EDB5839334`。
- 入力: `Kotowari.Benchmarks/Data/test.json`、4,172,062 bytes、SHA-256 `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。BOMなし、ASCIIのみ。前回と同じ入力を使った。
- 同じ測定ホストと読み込み経路を使用。MemoryStream間のコピー、文字列版のUTF-8からstringへの復号、UTF-8解析API内の配列コピー、JSON全体の解析、結果木生成を含む。物理ディスクI/Oは含まない。
- 全結果木の100,001ノード一致は、直前の測定時に両経路について確認済み。調査時には同じ実装・ホストを使い、この検証は再実行していない。
- Windows 11 25H2 (10.0.26200.9457)、Intel Core i7-13700H、.NET SDK 10.0.401、.NET 10.0.12、X64 RyuJIT、Concurrent Workstation GC、BenchmarkDotNet 0.15.8、Release。
- 全診断で `DOTNET_TieredPGO=1`。PGO無効化による対処・比較は行っていない。

## CPUサンプル

文字列とUTF-8をそれぞれ新規起動した1プロセスで、直列にEventPipe計測した。共通設定はLaunchCount=1、WarmupCount=16、IterationCount=32、InvocationCount=4、UnrollFactor=1、OutlierMode=DontRemove、Affinity=4 (mask `100`)、`--profiler EP`。各起動の高パフォーマンス電源プランへの変更と元のプランへの復帰を確認した。

BenchmarkDotNetのWorkloadActual開始・終了イベントの内側にある、実行スレッドのサンプルのみを既存のTraceAnalysisで集計した。

| 経路 | Actual区間数 | 実行スレッドのサンプル | EventsLost | MissingStacks |
|---|---:|---:|---:|---:|
| 文字列 | 32 | 683 | 0 | 0 |
| UTF-8 | 32 | 751 | 0 | 0 |

主なSelfサンプルは次のとおり。型表記は短縮した。

| メソッド | 文字列 | UTF-8 |
|---|---:|---:|
| SequenceParser<char, (Canon, char)>.ParseCore | 81 (11.86%) | 108 (14.38%) |
| DelimitedSequenceParser<KeyValuePair<Canon, Canon>, char>.ParseCore | 71 (10.40%) | 102 (13.58%) |
| WordParser.ParseCore | 50 (7.32%) | 79 (10.52%) |
| SequenceParser<Canon, char>.ParseCore | 28 (4.10%) | 76 (10.12%) |
| WithWhiteSpaceParser<Canon>.ParseCore | 32 (4.69%) | 46 (6.13%) |

UTF-8の集計には、Utf8Documentの読み取り・移動が独立したフレームとして現れなかった。インライン化された処理は親メソッドのサンプルになるため、カーソルのコストがなくなったとは扱わない。割合は各経路の実行スレッドサンプルを分母とする値であり、その差を処理時間差へ直接換算しない。Actual区間内にもTieredCompilationのイベントがあり、全サンプルが安定したTier 1コードに属すると断定しない。

Self全件とInclusive上位50件を既存 `word-parser/stacks.csv` に保存した。`string-after` / `utf8-after` は二重復号削除後を意味する。子呼び出し込みのサンプルは重なるため合算しない。

## ホットな呼び出し元の生成コード

CPUサンプルだけでは親メソッド内の読み取りと移動を分けられないため、別プロセスで文字列・UTF-8の生成コードを取得した。両経路ともWarmupCount=16、IterationCount=8、それ以外は上記の共通設定。EventPipeは併用せず、`DOTNET_JitDisasm=Marimo.Kotowari!*` と `DOTNET_JitStdOutFile` を指定した。

両プロセスの終了コード0、Result反復8件、電源プランの復帰、Tier 1出力を確認した。CPUサンプルを取ったプロセスとは別であり、そのサンプルの命令位置をこのコードへ対応付けたものではない。

`SequenceParser<Canon, char>.ParseCore` の最後のTier 1出力を比較した。両方のヘッダーは `optimized using Synthesized PGO` と記されている。この出力だけで、動的profileの生成時点や起動間のコードの安定性まで確認できたとは扱わない。

文字パーサーの一致後、UTF-8文書用の型guardに成功したカーソル移動経路では、ReadOnlyMemoryからSpanを取得するチェックとスライス計算の後に、次の呼び出しが残っていた。

```asm
; UTF-8: G_M000_IG17
call [System.Text.Rune:DecodeFromUtf8(System.ReadOnlySpan`1[byte],byref,byref):int]
add  r13d, dword ptr [rsp+0x60] ; consumed分だけバイト位置を進める
```

この経路では、ASCIIにもこの呼び出しを通る。Runeの値は使わず、文字幅だけを使っている。一方、同じ呼び出し元の文字列用の型guardに成功した移動経路は次の形だった。

```asm
; 文字列: G_M000_IG14
inc   r13d
mov   ecx, dword ptr [r15+0x10]
cmp   r13d, ecx
cmovg r13d, ecx
```

重要な制限として、Utf8Document.GoFoward単体のTier 1出力では、Runeの処理がインライン化され、既にASCIIの幅を1とする経路がある。したがって「全ての移動で復号関数呼び出しが残る」とは言わない。今回の候補は、実際にサンプルが多い呼び出し元で残った処理を減らし、このインライン化の成否への依存を小さくできないか試すものである。命令数・コードサイズだけを速度の根拠にはしていない。

## 当初の変更案（後に棄却・取り消し）

対象を `Utf8CharacterLength` に限定する。

```csharp
static int Utf8CharacterLength(ReadOnlySpan<byte> bytes)
{
    if (bytes[0] < 0x80)
        return 1;

    Rune.DecodeFromUtf8(bytes, out _, out var consumed);
    return consumed;
}
```

- 省く処理は、幅が1と確定しているASCIIでのUnicodeスカラー復号。パーサーの組み合わせ・責務分離を崩すインライン実装は行わない。
- 現在の呼び出し箇所はGoFowardのループ内のみ。UTF-8カーソルは非負の位置から始まり、正の移動の `index < Length` のときにだけ呼び出すため、対象のSpanは空でない。
- 非ASCIIは現在と同じRune処理を使う。不正入力の消費幅を先頭バイトだけで推測する方式へは変更しない。
- Currentの読み取り、メモリの別名参照、公開API、文書同一性は変えない。入力の基になるメモリの変更を反映する契約があるため、カーソルへ文字や幅をキャッシュする案は今回選ばない。
- 効果の大きさは未確認。採用判断は、この小さい変更の振る舞い確認と、前回と同じPGO有効・独立起動の通常測定で行う。UTF-8版の変更前後に加え、文字列版との差も比較する。
- 非ASCIIの性能は未確認。既存の巨大JSONがASCIIのみであることを隠さず、非ASCIIへの一般的な改善と断定しない。

## 記録・検証状態

既存stacks.csvへ `Experiment=2026-10-07-utf8-cursor-next-investigation` の185行を追記した。文字列はSelf 45 + Inclusive 50、UTF-8はSelf 40 + Inclusive 50。プロファイル付きの時間を通常測定のsummary・iterations・launchesへ追記していない。

元のトレース、集計JSON、生成コード、ログ、生成コード取得スクリプトはGit対象外の `artifacts/utf8-cursor-next-investigation-2026-10-07/` に保持している。比較に使ったコードは `codegen/string/sequence-canon-char.asm` と `codegen/utf8/sequence-canon-char.asm`。トレースは大きいためコミットしない。

今回新たな本体・テスト変更はないため、全テスト・全体ビルド・スタイル検査は再実行していない。コミットした実装の直前の確認結果は、全324テスト成功、全体ビルド・アナライザー成功、dotnet formatは既存箇所の指摘で終了コード1。詳細は[二重復号削除の記録](2026-10-07-utf8-cursor-single-decode.md#振る舞い静的検査)を参照。

## 取り消し後の検討（当時は未実装・未測定）

以下は当時の候補。ユーザーの指示により、一律コピーを追加しないReadOnlySpanの直接借用を先に実装・測定した。配列と範囲をDocumentに保持する案は実装していない。後続の結果は[Span借用・巨大JSON比較](2026-10-07-utf8-span-borrowing.md)を参照。

次の調査候補は、Utf8Documentでの `ReadOnlyMemory<byte>.Span` の反復取得。Currentの読み取りとGoFowardの各移動で取得している。上記のUTF-8生成コードのG_M000_IG13〜IG16には、文書型guardに加えて、Memoryの背後の型の判定、格納範囲の計算・チェック、現在位置からのスライス計算がある。このコードが存在することは確認できるが、既存のメソッド単位CPUサンプルではそのコストを分離できていない。したがって、まだボトルネックや改善効果とは断定しない。

検討するデータ構造は、配列入力なら文書構築時に配列と対象範囲を確定して保持し、毎文字の汎用Memory取得処理を避けるもの。JSONのUTF-8入口は現在もbyte配列を生成しているので、この測定入力には適用できる。RuneのASCII処理の再実装や、パーサーの組み合わせを手動で展開する案とは分ける。

- 公開のReadOnlyMemory入力は狭めない。配列以外のMemoryは既存経路を維持する案とし、構築時の一律コピーにはしない。
- 配列のスライス範囲、入力の別名参照、文書同一性を保つ。文字や幅のキャッシュは行わず、現在位置の境界確認も残す。
- 次に進む場合は、まずPGO有効のカーソル単独の読み取り・1文字移動で取得経路の差を切り分ける。単独測定での差だけをJSON全体への効果と扱わず、採用判断には同じ巨大JSONでの通常測定も必要とする。
- この段階では新しい本体変更、計測、計測用実装を行っていない。機械的な計測準備・実行・集計はLuna mediumへ委任し、仮説と結果の解釈は親が担当する。
