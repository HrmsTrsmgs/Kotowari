# UTF-8入力のSpan借用・巨大JSON比較（2026-10-07）

## 結果と判断

Spanからの同期借用と解析入口の全文コピー撤去は実装できた。しかし既存4.17 MB JSONで、UTF-8版は181.445→367.434 ms、文字列版も144.355→322.828 msとなった。変更後UTF-8は変更前の約2.02倍、文字列は約2.24倍の時間を要し、95%区間でも大幅な速度低下が残る。全文コピー撤去は成立したが、「UTF-8対応を導入しても元の文字列版に劣らない」という性能条件を満たしていない。この形の採用は勧めない。

時間は巨大JSON一つの読み込み準備・解析・全結果木生成あたり。個別文字や小さなJSONの時間ではない。

| 経路 | 独立起動 | 平均ms/JSON | 起動平均の範囲ms | 標本SD ms | 起動間CV | 割り当てMiB/JSON |
|---|---:|---:|---:|---:|---:|---:|
| UTF-8・変更前 | 8 | 181.445 | 175.634–186.816 | 4.144 | 2.284% | 57.535 |
| UTF-8・Span借用後 | 8 | 367.434 | 354.092–405.236 | 18.942 | 5.155% | 53.557 |
| 文字列・変更前 | 8 | 144.355 | 137.042–150.708 | 3.997 | 2.769% | 61.514 |
| 文字列・Span借用後 | 8 | 322.828 | 315.292–333.627 | 7.086 | 2.195% | 61.516 |

各組の対応から求めた幾何平均時間比（分子/分母、1より大きいと分子が遅い）:

| 比較 | 時間比 | 95% t区間（df=7） |
|---|---:|---:|
| 変更後UTF-8 / 変更前UTF-8 | 2.02324 | 1.91818–2.13406 |
| 変更後文字列 / 変更前文字列 | 2.23664 | 2.15317–2.32334 |
| 変更後UTF-8 / 変更後文字列 | 1.13714 | 1.08586–1.19084 |
| 変更後UTF-8 / 変更前文字列 | 2.54337 | 2.45003–2.64025 |

UTF-8の平均割り当ては60,330,242.5→56,158,436.25 bytes、約3.979 MiB減った。文字列は64,502,614.5→64,503,797.5 bytesで実質的には同程度。割り当て削減を速度改善の証拠として扱わない。変更後同士の時間差だけで評価すると、元の文字列版も遅くした事実を見落とすため、元の文字列版との比較も残す。

今回の変更は全文コピー撤去に加え、両経路で共有するカーソル・結果型とDocumentへの入力受け渡しも変えている。コピー撤去単独の効果は切り分けていない。速度低下の処理箇所、引数・戻り値の受け渡し、インライン化などの生成コード上の原因は未調査。文字列版も遅くなった事実は共通部分を次に調べる理由になるが、Span一般やvirtual呼び出し一般が遅いという結論にはしない。

追跡調査: [Span借用導入後の速度低下・原因切り分け](2026-10-07-utf8-span-regression.md)。本記録の計時と判断は当時の結果として保持し、後続の生成コード・対照実験は別記録へ分ける。

32起動の通常測定、既存データの保持検査、結果整理まで完了した。実装は未コミットのまま保持し、勝手な取り消し、次の最適化、Refactor、コミット、pushは行っていない。

## 実装と公開型への影響

変更前は `defe7830d2e11b9761f0902e2ad9f0b08acc96b6`。撤回したASCII分岐ではなく、二重復号削除後の実装を基準とする。変更後は本記録と対応する未コミットのSpan借用実装。

- `Cursol` と `ParseResult<T>` を `readonly ref struct` に変更した。文法パーサーのクラス・コンビネーター構成は維持する。
- `Cursol.FromUtf8` の入力を `ReadOnlyMemory<byte>` から `ReadOnlySpan<byte>` へ変更し、カーソルが入力を直接借用する。Utf8Documentには入力を保持せず、カーソルからDocumentのvirtual readerへSpanを渡す。
- JSONのSpan入口を `JObject.Parse(Cursol.FromUtf8(utf8))` へ直結し、解析入口の `(byte[])[.. utf8]` による全文コピーを撤去した。全文のstring変換も行わない。
- 文字・文字幅のキャッシュ、独自のASCII復号、パーサーの手動展開は追加していない。Runeによる復号と既存の例外経路を維持する。
- 文書の同一性と位置による等価性を型付きEquals・演算子で明示実装した。GetHashCodeもこの二つから計算する。

これは公開型の互換性変更を伴う。文字列入力の場合もCursol・ParseResultをクラスのフィールド、boxing、クロージャ、ref struct非対応のジェネリック型やiteratorの状態へ保存できない。入力を借用するカーソル・解析結果は入力の有効期間内で使用する。JSONの公開入口が返すJSONObjectの型は変えていない。利用時の寿命制約と同期解析中の入力不変更はXMLコメントに記載した。

「入力コピーなし」は解析入口での全文コピーがないことを意味する。測定ホストのMemoryStream間の読み込みコピー、文書同一性を持つDocumentの生成、JSON結果木・文字列・コレクションの割り当ては残る。無割り当ての解析という意味ではない。

## 入力と比較条件

- 入力: `Kotowari.Benchmarks/Data/test.json`、4,172,062 bytes、SHA-256 `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。ASCIIのみ、BOMなし。縮小・再生成していない。
- 変更前後の文字列・UTF-8両経路で、全100,001ノードの結果木と全値がSystem.Text.Jsonと一致することを測定外で確認した。
- MemoryStreamから別のMemoryStreamへコピーし、文字列版はUTF-8からstringへ復号して解析、UTF-8版はSpanを解析する。同じ読み込み処理、解析、結果木生成を測定内に含める。物理ディスクI/Oは測定外。
- 変更前UTF-8、変更後UTF-8、変更前文字列、変更後文字列の4経路。各8回の新規プロセス起動、計32起動を直列に行う。カーソル型変更が文字列版にも影響するため、変更後の文字列版だけを対照にして改善を判定しない。
- 8組、奇数組は変更前UTF-8→変更後UTF-8→変更前文字列→変更後文字列、偶数組は逆順。
- Release、Dynamic PGO有効、LaunchCount=1、WarmupCount=16、IterationCount=8、InvocationCount=4、UnrollFactor=1、OutlierMode=DontRemove、Affinity=4 (mask `100`)。通常計時にプロファイラーや生成コード診断を付けない。
- Windows 11 25H2 (10.0.26200.9457)、Intel Core i7-13700H、.NET SDK 10.0.401、.NET 10.0.12 (10.0.1226.42308)、X64 RyuJIT、Concurrent Workstation GC、BenchmarkDotNet 0.15.8。
- 起動平均を独立試行の単位とし、各組の時間比の対数から自由度7の95% t区間を求める。起動内の反復を独立プロセスとして数えない。
- 32起動すべてでActual・Result各8反復、PGO=1、Release/Optimize=true、測定後の電源プラン復帰を確認した。ソースは測定前に固定し、測定したコピーと終了後の本体3ファイルのハッシュが一致した。外れ値除去、PGO無効化、速い起動だけの選択はしていない。
- 初回の起動は、生成ホストからのNuGet.Config読み取り・WMI/電源設定の権限不足で実測前に失敗した。権限調整後、条件を変えず32起動を行った。実測のないログは `failed-first-attempt/` へ分離し、主比較へ混ぜていない。
- 大幅な差が出たため、変更前後のRelease/Optimize設定、別名ホスト、分離した生成ランナー、測定対象DLLの対応を確認した。設定違いは見つからなかったが、これを速度低下の原因特定とは扱わない。
- 変更前コピーの本体・JSON全24個のC#ソースは、BOM・改行・末尾改行を正規化した内容が基点コミットと24/24一致した。プロジェクト参照・出力の分離と、測定対象コードの変更を区別している。
- この区間は対応付き対数時間比・t区間による推定であり、別入力・別環境まで保証するものではない。既存巨大JSONはASCIIのみで、非ASCIIの性能は未測定。

## 振る舞い・ビルド・静的検査

- Red: stackallocのスライスをReadOnlySpanとして既存FromUtf8へ渡すテストを追加し、CS1503による意図したコンパイル失敗を確認した。
- Green: 追加テスト1件成功。元バッファの変更が元・前進後のカーソルへ反映され、スライスの先頭基準の位置と終端を維持する。
- 全テストRelease: Kotowari 210、JSON 115、合計325成功。失敗・Skipなし。既存テストはboxing・捕捉不可への機械的適応のみ行い、成否、カーソル、成功値、失敗時のParsed例外の検証を維持した。
- 指定ソリューション非増分アナライザービルド: 17.99秒、失敗。旧WordParser測定コードのCursolフィールド2箇所でCS8345。ジェネレーター・ラムダの旧比較候補にも借用型との互換性問題がある。既存SpracheJSONのNU1701も残る。
- 本体・JSONを参照する両テストプロジェクトの個別非増分アナライザービルド: Kotowari.Tests 7.73秒、JSONテスト10.81秒、両方成功、警告・エラー0。本体・JSONも成功。
- SARIF: 本体IDE0090×2、テストIDE0200×1、JSON IDE0066・CA1854・CA1862各1。既存の未変更箇所のみで、新規診断なし。
- 指定dotnet format verify: 13.97秒、終了コード2。既存のWHITESPACE・IDE0290・IDE0044等が残る。無関係な修正は混ぜていない。
- 変更ファイルのUTF-8/BOM/CRLF確認、git diff checkは成功。Refactor、ステージ、コミット、pushは行っていない。
- 追加の129,292入力の互換性比較は、比較用DLLの配置衝突などで成立しなかった。成功した検証として数えず、この網羅的比較は未確認として残す。既存テストと全結果木検証の成功を、その代わりに網羅的互換性が証明されたものとは扱わない。

旧WordParser比較コードの取り扱いはユーザー確認待ちで、ソースを削除・書き換え・ビルドから除外していない。今回の独立JSON計測ホストはこのコードを参照せず、変更前後ともReleaseビルドが成功した。

## 実装・テストの変更ファイル

- `Kotowari/Core/Cursol.cs`: 入力のSpan借用、ref struct化、Documentへの入力受け渡し、明示的な等価性。
- `Kotowari/ParseResult.cs`: 借用カーソルを保持するためのref struct化。
- `JsonParser/JSON.cs`: Span入口の全文コピー撤去。
- `Kotowari.Tests/Core/Cursolのテスト.cs`: stackallocスライスのコピーなし借用を検証するRed/Greenテスト1件。
- `Kotowari.Tests/ParseResultのテスト.cs` および `Kotowari.Tests/Core/` の `CharParserのテスト.cs`、`OrParserのテスト.cs`、`WordParserのテスト.cs`、`ZeroOrMoreParserのテスト.cs`、`DebugParserのテスト.cs`、`RecursiveParserのテスト.cs`: 既存検証を維持したboxing・捕捉不可への適応。

旧WordParserベンチマーク3ファイルとベンチマークプロジェクトは未変更。独立測定ホストや一時スクリプトを通常製品のビルドへ追加していない。

## 記録

実験IDは `2026-10-07-utf8-span-borrowing`。通常測定データは既存の `word-parser/summary.csv`、`launches.csv`、`iterations.csv` に整理する。元ログ、完全JSON、版のmanifest、測定ソース、実行・集計スクリプトはGit対象外の `artifacts/utf8-span-borrowing-2026-10-07/` に保存する。

- 追記: summary=32、launches=32、iterations=512（256 Actual + 256 Result）。stacksへの追記なし。時間はms、反復の総時間はns、割り当てはMiB。
- 最終データ行数: summary=568、launches=734、iterations=13,492、stacks=529。今回より前のsummary=536、launches=702、iterations=12,980は、元データ領域のバイト比較でヘッダー・既存セルの不変を確認した。stacksはハッシュも不変。
- 証拠・版: `evidence.json`、`version-manifest.json`、`tracked-source.patch`、`Run.ps1`、`Analyze.ps1`。この実験ディレクトリ直下の `Kotowari/`・`JsonParser/` は変更前、`current/` 配下は変更後。`host/baseline/`・`host/current/` の測定メソッド本体は同一ハッシュ。
- 機械的な準備・実行・検証・集計はLuna mediumへ委任した。条件選定と採用判断は進行役が行った。

変更後の本体SHA-256（測定コピーと作業ツリーが一致）:

| ファイル | SHA-256 |
|---|---|
| `Kotowari/Core/Cursol.cs` | `31A3681E6765EE0A01553500E4EF7113A70107D8991BE0FE70665A19E3562F09` |
| `Kotowari/ParseResult.cs` | `D2B0FE51DF1511A11044C591CE44E4BF2FF4609B1ED3B57AEC9A09C9E8F87ABC` |
| `JsonParser/JSON.cs` | `740F2C0ADB797C2291BB73E5FFFE62A716FF66288F9B57B4E4F551D55670E259` |


## 測定した本体ソースの保存

本体変更はまだ未コミット。実装を保存する場合は、対応する測定記録と同じ作業単位でコミットする。後に取り消す場合も測定値の対応が失われないよう、基点 `defe7830d2e11b9761f0902e2ad9f0b08acc96b6` からの本体3ファイルの差分をここに残す。ソースハッシュはUTF-8 BOM・CRLFのファイル内容に対する値である。

<details>
<summary>本体3ファイルの測定時差分</summary>

```diff
diff --git a/JsonParser/JSON.cs b/JsonParser/JSON.cs
index e31b9d5..bb6f776 100644
--- a/JsonParser/JSON.cs
+++ b/JsonParser/JSON.cs
@@ -240,12 +240,13 @@ public class JSON
     /// <returns>解析したJSONオブジェクト。</returns>
     /// <exception cref="ParseException">入力がJSONオブジェクトの文法に一致しない場合。</exception>
     /// <remarks>
-    /// 指定範囲をbyte配列へコピーし、全文をstringへ変換せずに解析します。
+    /// 指定範囲を同期解析中に借用し、byte配列へのコピーや全文のstring変換を行いません。
+    /// 呼び出し中は入力の基になるメモリを有効に保ち、変更しないでください。
     /// 補助平面文字および不正なUTF-8入力には対応していません。
     /// </remarks>
     public static JSONObject Parse(ReadOnlySpan<byte> utf8)
     {
-        var result = JObject.Parse(Cursol.FromUtf8((byte[])[.. utf8]));
+        var result = JObject.Parse(Cursol.FromUtf8(utf8));
         return result.IsSuccess ? result.Parsed : throw new ParseException();
     }
 }
diff --git a/Kotowari/Core/Cursol.cs b/Kotowari/Core/Cursol.cs
index b24dbe7..b4f7bfd 100644
--- a/Kotowari/Core/Cursol.cs
+++ b/Kotowari/Core/Cursol.cs
@@ -5,7 +5,14 @@ using System.Text;
-
+
 namespace Marimo.Kotowari.Core;
-
+
-public readonly record struct Cursol
+/// <summary>
+/// 文書と読み取り位置を表す、同期解析用のカーソル。
+/// </summary>
+/// <remarks>
+/// UTF-8入力を借用するため、ヒープへの格納、boxing、ラムダでの捕捉はできません。
+/// カーソルとそのコピーは、借用した入力の有効期間内で使用してください。
+/// </remarks>
+public readonly ref struct Cursol
 {
     /// <summary>
     /// 範囲外の文字読み取りで返すヌル文字（U+0000）。
@@ -13,8 +20,9 @@ public readonly record struct Cursol
     public const char NullCharacter = '\0';
-
+
     readonly Document document;
+    readonly ReadOnlySpan<byte> utf8;
-
+
-    public char Current => document[Index];
+    public char Current => document.Read(Index, utf8);
-
+
     public int Index { get; private init; }
     public ReadOnlySpan<char> Text => document is null ? default : document.Text;
@@ -23,41 +31,65 @@ public readonly record struct Cursol
     Cursol(string text, int index)
     {
         document = new StringDocument(text);
+        utf8 = default;
         Index = index;
     }
-
+
-    Cursol(ReadOnlyMemory<byte> utf8)
+    Cursol(ReadOnlySpan<byte> utf8)
     {
-        document = new Utf8Document(utf8);
+        document = new Utf8Document();
+        this.utf8 = utf8;
         Index = 0;
     }
-
+
     /// <summary>
     /// 有効なUTF-8のASCII・BMP文字を読む先頭位置のカーソルを作成します。
     /// </summary>
-    /// <param name="utf8">コピーせず保持するUTF-8バイト列。</param>
+    /// <param name="utf8">コピーせず借用するUTF-8バイト列。</param>
     /// <returns>Indexが入力先頭からのバイト位置を表すカーソル。</returns>
     /// <remarks>
     /// 正のGoFowardのstepは読み取る文字数を指定し、負のstepでは位置が変わりません。
     /// Textは空のSpanを返します。入力の基になるメモリを呼び出し側で変更すると読み取りに反映されます。
+    /// 返されたカーソルと解析結果は、入力の有効期間を越えて保持できません。
+    /// 入力を同期解析中に変更しないでください。
     /// 補助平面文字および不正なUTF-8入力の読み取りには対応していません。
     /// </remarks>
-    public static Cursol FromUtf8(ReadOnlyMemory<byte> utf8)
+    public static Cursol FromUtf8(ReadOnlySpan<byte> utf8)
         => new(utf8);
-
+
     public Cursol GoFoward(int step)
-        => this with { Index = document.GoFoward(Index, step) };
+        => this with { Index = document.GoFoward(Index, step, utf8) };
-
+
     public Cursol Copy() => this with { };
-
+
+    /// <summary>
+    /// 同じ文書の同じ位置を指すかを比較します。入力内容そのものは比較しません。
+    /// </summary>
+    public bool Equals(Cursol other)
+        => ReferenceEquals(document, other.document) && Index == other.Index;
+
+    /// <summary>
+    /// boxingできないため、objectとの比較は常にfalseを返します。
+    /// </summary>
+    public override bool Equals(object? obj) => false;
+
+    /// <summary>
+    /// 文書の同一性と位置に基づくハッシュ値を返します。
+    /// </summary>
+    public override int GetHashCode()
+        => HashCode.Combine(document, Index);
+
+    public static bool operator ==(Cursol left, Cursol right) => left.Equals(right);
+    public static bool operator !=(Cursol left, Cursol right) => !left.Equals(right);
+
     /// <summary>
     /// 入力の読み取り・位置計算と、Cursol間で共有する文書の同一性を担う。
     /// </summary>
     abstract class Document
     {
         public abstract ReadOnlySpan<char> Text { get; }
-        public abstract char this[int index] { get; }
-        public abstract int GoFoward(int index, int step);
+        public abstract char Read(int index, ReadOnlySpan<byte> utf8);
+        public abstract int GoFoward(int index, int step, ReadOnlySpan<byte> utf8);
     }
-
+
     sealed class StringDocument(string text) : Document
@@ -69,55 +101,45 @@ public readonly record struct Cursol
         // 生成時に長さを取得し、null入力の既存の例外発生時点を保つ。
         public int Length { get; } = text.Length;
-
+
-        public override char this[int index]
+        public override char Read(int index, ReadOnlySpan<byte> utf8)
         {
-            get
+            try
+            {
+                return input[index];
+            }
+            catch (IndexOutOfRangeException)
             {
-                try
-                {
-                    return input[index];
-                }
-                catch (IndexOutOfRangeException)
-                {
-                    return NullCharacter;
-                }
+                return NullCharacter;
             }
         }
-
+
-        public override int GoFoward(int index, int step)
+        public override int GoFoward(int index, int step, ReadOnlySpan<byte> utf8)
             => Math.Min(index + step, Length);
     }
-
+
-    sealed class Utf8Document(ReadOnlyMemory<byte> input) : Document
+    sealed class Utf8Document : Document
     {
-        readonly ReadOnlyMemory<byte> utf8 = input;
-
         public override ReadOnlySpan<char> Text => default;
-
+
-        public int Length { get; } = input.Length;
-
-        public override char this[int index]
+        public override char Read(int index, ReadOnlySpan<byte> utf8)
         {
-            get
-            {
-                if (index < 0 || index >= Length) return NullCharacter;
+            if (index < 0 || index >= utf8.Length) return NullCharacter;
-
+
-                var remaining = utf8.Span[index..];
-                Rune.DecodeFromUtf8(remaining, out var current, out var consumed);
-                if (current.IsBmp) return (char)current.Value;
+            var remaining = utf8[index..];
+            Rune.DecodeFromUtf8(remaining, out var current, out var consumed);
+            if (current.IsBmp) return (char)current.Value;
-
+
-                // 補助平面文字は一文字のcharに収まらないため、UTF-16への変換で拒否する。
-                Span<char> destination = stackalloc char[1];
-                Encoding.UTF8.GetChars(remaining[..consumed], destination);
-                return destination[0];
-            }
+            // 補助平面文字は一文字のcharに収まらないため、UTF-16への変換で拒否する。
+            Span<char> destination = stackalloc char[1];
+            Encoding.UTF8.GetChars(remaining[..consumed], destination);
+            return destination[0];
         }
-
+
-        public override int GoFoward(int index, int step)
+        public override int GoFoward(int index, int step, ReadOnlySpan<byte> utf8)
         {
-            while (step > 0 && index < Length)
+            while (step > 0 && index < utf8.Length)
             {
-                index += Utf8CharacterLength(utf8.Span[index..]);
+                index += Utf8CharacterLength(utf8[index..]);
                 step--;
             }
             return index;
diff --git a/Kotowari/ParseResult.cs b/Kotowari/ParseResult.cs
index 6ecf466..669e1aa 100644
--- a/Kotowari/ParseResult.cs
+++ b/Kotowari/ParseResult.cs
@@ -3,7 +3,14 @@ using System;
-
+
 namespace Marimo.Kotowari;
-
+
-public readonly struct ParseResult<T>
+/// <summary>
+/// 同期解析の成否、読み取り位置、および成功時の値を保持します。
+/// </summary>
+/// <remarks>
+/// カーソルが入力を借用するため、ヒープへの格納、boxing、ラムダでの捕捉はできません。
+/// 結果はカーソルが借用した入力の有効期間内で使用してください。
+/// </remarks>
+public readonly ref struct ParseResult<T>
     where T : notnull
 {
     readonly T? parsed;
```

</details>
