# UTF-8カーソル・ASCII文字幅の直接取得（2026-10-07）

## 結果と判断

既存4.17 MBのJSONをMemoryStreamから読み込み、同じ結果木を生成する条件で、変更前182.241 ms、変更後180.456 msだった。8組の起動平均の対応から求めた時間比は0.99028（0.97%短縮）。95% t区間は0.96605〜1.01512で、1.51%延長〜3.39%短縮に相当する。無差の1を含むため、今回の測定では改善を確認できなかった。

| 経路 | 独立起動 | 平均ms/JSON | 起動平均の範囲ms | 起動間CV | 割り当てMiB/JSON |
|---|---:|---:|---:|---:|---:|
| UTF-8・変更前 | 8 | 182.241 | 176.238–188.165 | 2.051% | 57.535 |
| UTF-8・ASCII幅を直接取得 | 8 | 180.456 | 177.020–185.083 | 1.573% | 57.536 |
| 文字列・同時期の対照 | 8 | 147.725 | 141.713–154.860 | 3.798% | 61.515 |

変更後UTF-8と文字列版の時間比は1.22221、95%区間は1.18983〜1.25546。文字列版より22.2%遅く、同等以上という基準は満たしていない。前回の測定バッチの文字列144.193 msと今回の147.725 msを直接比較して、UTF-8だけの効果を判定していない。

割り当てに実質的な変化は観測していない。起動間CVは点推定を記載したもので、8起動の結果からばらつき対策に成功したとは判断しない。

今回の結果からASCIIの文字幅取得を大きなボトルネックとみなす根拠は得られなかった。一方、無差が証明されたとも扱わない。改善を確認できなかったため採用を見送り、ユーザーの指示でASCII分岐を取り消した。Cursol.csは変更前のSHA-256へ戻し、測定データと実験実装の記録は残す。コミットはまだ行っていない。

## 対象実装と変更

- 変更前: `defe7830d2e11b9761f0902e2ad9f0b08acc96b6`。
- 変更対象: `Kotowari/Core/Cursol.cs` の `Utf8CharacterLength` のみ。ASCIIは復号せず幅1を返し、非ASCIIは従来どおりRuneで幅を取得する。
- 公開API、Currentの読み取り、文字数とバイト位置の扱い、入力の別名参照、文書同一性、パーサーの組み合わせ構造は変更していない。
- 変更前Cursol.cs SHA-256: `23F28F7808953A33809A6A85FF9D36A45059DA83A9DBA3ADD9CFB0EDB5839334`。
- 変更後Cursol.cs SHA-256: `01CF86A4D598FA90D9FCFCCDB513671C4EDD57A14C48FF34247F6AE9A902C680`。測定時は未コミット。変更前ソースと変更後ファイルは下記artifactsに保持する。

変更後のメソッド全体:

```csharp
static int Utf8CharacterLength(ReadOnlySpan<byte> bytes)
{
    // 終端は呼び出し元で除外済み。ASCIIの文字幅は復号せずに確定できる。
    if (bytes[0] < 0x80)
        return 1;

    Rune.DecodeFromUtf8(bytes, out _, out var consumed);
    return consumed;
}
```

既存の唯一の呼び出し元は、終端を除外したGoFowardのループ内。UTF-8カーソルは非負のバイト位置から始まるため、このSpanは空でない。不正なUTF-8入力の消費幅を先頭バイトから独自に推測する変更はしていない。

## 入力、環境、測定方法

- 入力: `Kotowari.Benchmarks/Data/test.json`、4,172,062 bytes、SHA-256 `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。BOMなし、ASCIIのみ。入力を縮小・再生成していない。
- 変更前後のホストで文字列・UTF-8両経路の全結果木をSystem.Text.Jsonと比較し、100,001ノードの全値一致を測定前に確認した。
- MemoryStreamから別のMemoryStreamへコピーし、文字列版はUTF-8からstringへ復号、UTF-8版はReadOnlySpan<byte>から解析する。UTF-8解析API内の配列コピー、全JSON解析、結果木生成を含む。物理ディスクI/Oは含まない。
- Windows 11 25H2 (10.0.26200.9457)、Intel Core i7-13700H、.NET SDK 10.0.401、.NET 10.0.12、X64 RyuJIT、Concurrent Workstation GC、BenchmarkDotNet 0.15.8、Release。
- `DOTNET_TieredPGO=1`、Affinity=4 (mask `100`)、LaunchCount=1、WarmupCount=16、IterationCount=8、InvocationCount=4、UnrollFactor=1、OutlierMode=DontRemove。通常測定にプロファイラーは付けていない。
- 8組を直列実行。奇数組は変更前UTF-8→変更後UTF-8→文字列、偶数組は文字列→変更後UTF-8→変更前UTF-8。各経路8プロセス、合計24プロセス。各起動にActualとResultが8反復ずつあり、PGO有効条件と高パフォーマンス電源プランから元のプランへの復帰を確認した。
- 表は8起動平均の算術平均、範囲、標本標準偏差によるCV。時間比は各組の起動平均の比を対数へ変換し、その平均と標準誤差から自由度7の95% t区間を求め、指数で戻した。起動内の反復を独立したプロセスとして数えていない。外れ値の除外、PGO無効化、都合のよい起動だけの選択は行っていない。
- 記載した区間はこの対応付き対数時間比・t区間による推定。任意の入力・環境について効果がこの範囲に収まる保証ではない。
- 最初の測定ホスト起動は、過去のホストとのプロジェクト名重複により実測前に失敗した。固有名へ修正して再開し、実測のないログは `failed-first-attempt/` に分離した。成功した24プロセスの値のみが通常測定データである。

## 生成コードの追加確認

[実装前の調査](2026-10-07-utf8-cursor-next-investigation.md)で見た `SequenceParser<Canon, char>.ParseCore` と同じ箇所を、変更後の別プロセスで確認した。

PGO有効、WarmupCount=16、IterationCount=8、InvocationCount=4、UnrollFactor=1、Affinity=4、DontRemoveの条件で、`DOTNET_JitDisasm=Marimo.Kotowari!*` と `DOTNET_JitStdOutFile` を指定した。終了コード0、Result反復8件、電源設定の復帰を確認した。診断付きの時間は主比較のCSVへ混ぜていない。

最後のTier 1出力には、ASCIIがRune呼び出しを避けて幅1を返す経路があった。

```asm
; G_M000_IG14
cmp byte ptr [rcx], 128
jae G_M000_IG60             ; 非ASCIIはRune処理へ
mov ebp, 1
; G_M000_IG15
add r13d, ebp
```

非ASCII側のG_M000_IG60には `Rune.DecodeFromUtf8` の呼び出しが残っている。意図したASCII分岐が反映されたことは確認できたが、通常測定とは別プロセスなので、全24起動で同じ生成コードだったとは保証しない。ヘッダーは `optimized using Synthesized PGO`。生成コードの変化だけを改善の証拠とせず、通常測定では改善を確認できなかったという判断を維持する。

ユーザーの指摘後に確認した[.NET 10.0.12のRune実装](https://github.com/dotnet/runtime/blob/v10.0.12/src/libraries/System.Private.CoreLib/src/System/Text/Rune.cs#L389-L395)にも、ASCIIなら幅1を返す分岐がある。変更前のGoFoward単体の生成コードでもこの経路はインライン化されていた。呼び出しが残ることだけでは、不要な復号処理や大きなコストが残る証拠にならない。標準実装と重なる最適化だったことは改善を確認できなかった有力な説明だが、通常測定だけで原因を一つに確定したものではない。

## 振る舞い・静的検査

- カーソル対象テスト: 22成功。
- 全テスト: Kotowari 209、JSON 115、合計324成功、失敗・Skipなし。振る舞いを維持する性能変更のため、形式的なRedや実装詳細だけのテストは追加していない。
- 一時的な互換性比較: 全BMPスカラー値、全1バイト・2バイト列、一部の不正な長い列、補助平面文字など129,292入力。Current、1文字進んだ位置と次の文字、例外の型・メッセージの結果列が一致した。両実装の結果列SHA-256は `D60B41DF909AA2DC4E1938D3A24966A8C483ACAC3691C2E05FC1422547F245D4`。不正入力や補助平面文字への対応追加を意味しない。
- 全体ビルド・アナライザー: 成功、21.800秒。指定された非増分ビルドとSARIF確認を実施。既存SpracheJSON 1.0.1のNU1701が1件、変更したヘルパーに診断なし。
- `dotnet format Kotowari.sln --no-restore --verify-no-changes --severity info`: 終了コード2。診断詳細付きでも確認し、既存NU1701のワークスペース警告と既存箇所の書式・コードスタイル指摘がある。Cursolの指摘は既存コンストラクターの22行目で、変更した127〜130行目には指摘なし。無関係な修正は混ぜていない。
- 初回のオフライン復元はNuGet.Configの読み取り権限で失敗した。権限を調整して、既存のローカルパッケージキャッシュを指定した復元と検証を完了した。新しいパッケージ・依存関係は追加していない。
- 取り消し後: Cursol.csのSHA-256が変更前と完全一致し、本体のGit差分がないことを確認した。Releaseの全324テストを再実行し、失敗・Skipなし。同一ソース・設定で確認済みの変更前ビルド・アナライザー結果を再利用し、性能測定と全体スタイル検査は繰り返していない。

## 記録

既存の種類別CSVへ `Experiment=2026-10-07-utf8-cursor-ascii-width` を追記した。summary=24、launches=24、iterations=384（192 Actual + 192 Result）。今回の通常計時や生成コード診断によるstacks行の追加はない。時間単位はms、反復の総時間はns、割り当て単位はMiB。

元ログ・完全JSON・集計結果・スクリプトはGit対象外の `artifacts/utf8-cursor-ascii-width-2026-10-07/` に保持する。`baseline/` は変更前の追跡ソース、`current-source/Kotowari/Core/Cursol.cs` は変更後のファイル。生成コードは `codegen/utf8/jit.asm`、比較対象の抜粋は `codegen/utf8/sequence-canon-char.asm`。

測定記録は未コミット。ASCII分岐を取り消した時点では、Cursol.csに未コミット差分はなかった。その後の[Span借用実装](2026-10-07-utf8-span-borrowing.md)とは別の実験である。変更前コミット、変更対象を限定したメソッド全体、両ソースのSHA-256を本記録へ残し、棄却した実装と測定値の対応を保持する。完全な実験ファイルは上記artifactsにも保持する。非ASCIIの性能は未測定。

レビュー対象ファイル（前回から保持した未コミットの調査記録を含む）:

- `Kotowari.Benchmarks/Results/2026-10-07-utf8-cursor-ascii-width.md`
- `Kotowari.Benchmarks/Results/2026-10-07-utf8-cursor-next-investigation.md`
- `Kotowari.Benchmarks/Results/README.md`
- `Kotowari.Benchmarks/Results/word-parser/summary.csv`
- `Kotowari.Benchmarks/Results/word-parser/iterations.csv`
- `Kotowari.Benchmarks/Results/word-parser/launches.csv`
- `Kotowari.Benchmarks/Results/word-parser/stacks.csv`（前回調査の185行）
