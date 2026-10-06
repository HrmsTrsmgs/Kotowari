# UTF-8カーソルの二重復号削除（2026-10-07）

## 結果

4.17 MBの既存JSONをMemoryStreamから読み込み、同じ結果木を生成する条件で、UTF-8版の所要時間が平均224.065 msから180.961 msへ短縮された。8組の起動平均から求めた時間比の幾何平均は0.80760、所要時間の短縮率は19.2%。対応する95% t区間は16.1〜22.3%の短縮だった。

同じ測定バッチの文字列版は平均144.193 ms。変更後のUTF-8版は文字列版より25.5%遅く、時間比の95%区間も+21.6〜+29.6%だった。「UTF-8入力でも読み込みから結果生成まで文字列版と同等以上」という基準はまだ満たしていない。

| 経路 | 独立起動 | 平均ms/JSON | 起動平均の範囲ms | 起動間CV | 割り当てMiB/JSON |
|---|---:|---:|---:|---:|---:|
| UTF-8・変更前 | 8 | 224.065 | 216.064–236.446 | 2.890% | 57.535 |
| UTF-8・二重復号削除後 | 8 | 180.961 | 174.247–189.440 | 3.035% | 57.536 |
| 文字列・同時期の対照 | 8 | 144.193 | 139.285–149.624 | 2.970% | 61.514 |

割り当てはほぼ同じで、今回の改善は所要時間に現れた。起動間CVの低下は観測されておらず、ばらつきを解決したとは判断しない。

## 対象実装と変更

- 基点: `f4044ec25cfec2cb6562078c92350fc4318df0b4`。変更前の追跡ソースをコピーして独立した測定ホストから参照した。
- 変更: `Kotowari/Core/Cursol.cs` の `Utf8Document` インデクサだけ。`Rune.DecodeFromUtf8` が返すBMP文字をそのまま `char` として返し、同じ文字に対する `Encoding.UTF8.GetChars` を省いた。
- 補助平面文字は一つの `char` に収まらないため、元と同じ変換処理で読み取り時に拒否する。公開API、バイト位置の保持、`GoFoward`、文書の同一性、入力のコピー方法は変更していない。
- 変更前のCursol.cs SHA-256: `4DC40064ED5771057029B60E3E2DA044126EB5FD3D6384442BDE18975123715B`。
- 変更後のCursol.cs SHA-256: `23F28F7808953A33809A6A85FF9D36A45059DA83A9DBA3ADD9CFB0EDB5839334`。この測定記録を含むコミットのCursol.csを使う。

変更前の読み取りは、現在のバイト位置で `Rune.DecodeFromUtf8` により文字幅を求め、その範囲を `Encoding.UTF8.GetChars` で再度復号していた。先頭からの走査はない。今回も読み取り位置の計算は変えず、既に得られた復号結果を利用する。

## 変更候補を絞ったCPUプロファイル

変更前の文字列版とUTF-8版をそれぞれ独立した1プロセスでEventPipeプロファイルした。通常の計時とは別に `--profiler EP`、WarmupCount=10、IterationCount=10、InvocationCount=4を使い、BenchmarkDotNetのWorkloadActual開始・終了イベント内の実行スレッドのサンプルだけを集計した。両トレースともEventsLost=0、MissingStacks=0だった。

| 経路 | Actual区間のサンプル | インデクサ本体 | インデクサ・子呼び出し込み | Utf8Utility.TranscodeToUtf16本体 |
|---|---:|---:|---:|---:|
| 文字列 | 214 | 4 (1.87%) | 4 (1.87%) | 2 (0.93%) |
| UTF-8 | 294 | 18 (6.12%) | 45 (15.31%) | 27 (9.18%) |

UTF-8のインデクサ内から復号処理へ入る経路を確認でき、コード上でも復号結果の破棄と再復号が確認できたため、そこを最初の候補にした。サンプル率は壁時計時間の厳密な内訳ではなく、インライン化された処理が親メソッドに現れることもある。Actual区間にもTieredCompilationのイベントがあるため、Tier 1コードだけの比較とは扱わない。プロファイル付きの時間は主比較に含めていない。

`stacks.csv` にSelf全件とInclusive上位50件を保存した。`string-before` と `utf8-before` はどちらも変更前のプロファイルである。子呼び出し込みの値は重なるため合算しない。

## 入力、環境、測定方法

- 入力: `Kotowari.Benchmarks/Data/test.json`、4,172,062 bytes、SHA-256 `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。BOMなし、ASCIIのみ。小さな入力へ置き換えていない。
- 測定前に変更前後のホストで、文字列・UTF-8両経路の全結果木をSystem.Text.Jsonの解析結果と比較し、100,001ノードの全値一致を確認した。
- 測定経路は[初回のUTF-8入力経路比較](2026-10-07-utf8-stream-json.md)と同じ。測定ごとにMemoryStreamから別のMemoryStreamへコピーし、文字列版はUTF-8からstringへ復号、UTF-8版はReadOnlySpan<byte>から解析する。UTF-8解析API内の入力配列コピー、全JSON解析、結果木生成も含む。物理ディスクI/Oは含まない。
- Windows 11 25H2 (10.0.26200.9457)、Intel Core i7-13700H、.NET SDK 10.0.401、.NET 10.0.12、X64 RyuJIT、Concurrent Workstation GC、BenchmarkDotNet 0.15.8、Release。
- `DOTNET_TieredPGO=1`、Affinity=4 (mask `100`)、LaunchCount=1、WarmupCount=16、IterationCount=8、InvocationCount=4、UnrollFactor=1、OutlierMode=DontRemove。各起動で高パフォーマンス電源プランへの変更と元のプランへの復帰を確認した。
- 8組を直列実行した。奇数組は変更前UTF-8→変更後UTF-8→文字列、偶数組は文字列→変更後UTF-8→変更前UTF-8。各経路8プロセス、合計24プロセス。それぞれActualとResultが8反復ずつあることを確認した。
- 表は8起動平均の算術平均、範囲、標本標準偏差から求めたCV。性能差は各組の起動平均の比を対数へ変換し、その平均と標準誤差から自由度7の95% t区間を計算して指数で戻した。反復を独立したプロセスと数えていない。外れ値の除外とPGO無効化は行っていない。

## 振る舞い・静的検査

- 既存全テスト: Kotowari 209、JSON 115、合計324成功、失敗・Skipなし。
- 一時的な互換性検証: 全BMPスカラー値、全1バイト・2バイト列、一部の不正な長い列、補助平面文字など129,292入力について、Currentの値、1文字進んだ位置と次の文字、例外の型・メッセージを比較した。結果列のSHA-256は両実装とも `D60B41DF909AA2DC4E1938D3A24966A8C483ACAC3691C2E05FC1422547F245D4`。不正入力や補助平面文字への対応追加を意味しない。
- 全体ビルド・アナライザー: 成功、17.600秒。既存のSpracheJSON 1.0.1互換性警告NU1701が1件。各プロジェクトのSARIFも確認し、変更したインデクサに指摘なし。
- `dotnet format Kotowari.sln --no-restore --verify-no-changes --severity info`: 既存箇所の指摘で終了コード1。Cursolの既存コンストラクターの空白、他ファイルの書式・primary constructor・readonly等の指摘があり、今回の変更行には指摘なし。範囲外の修正は混ぜていない。

## 記録

既存の種類別CSVへ `Experiment=2026-10-07-utf8-cursor-single-decode` を追記した。summary=24行、launches=24行、iterations=384行（192 Actual + 192 Result）、stacks=171行。時間単位はms、反復の総時間はns、割り当て単位はMiB。

元の完全JSON、ログ、変更前ソース、測定・互換検証スクリプト、計算結果はGit対象外の `artifacts/utf8-cursor-tuning-2026-10-07/` にある。変更前トレースとその集計は `artifacts/utf8-json-end-to-end-2026-10-07/profile/` にある。

今回の短縮を確認した段階で変更を区切る。残る文字列版との差は次のチューニング対象であり、今回の結果だけで原因を断定しない。非ASCII入力の性能は未測定。
