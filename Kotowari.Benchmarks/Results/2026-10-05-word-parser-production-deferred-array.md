# WordParser本体のList版と遅延配列版の比較（2026-10-05）

## 結果

[候補比較](2026-10-05-word-parser-accumulation.md)で見つかった遅延配列を本体へ反映し、元のList版と実際の `WordParser` 同士で測定した。
再利用の成功・先頭失敗・末尾失敗では、変更後の全3起動平均が元版の最小起動平均を下回った。
起動平均の中央値で、平均時間はそれぞれ29.6%、37.0%、12.1%短縮した。

下表の時間は、各起動8反復の平均を3起動分集めた中央値（ns/op）。割り当ては全3起動で一致したB/op。
24反復を24回の独立した起動として扱っていない。中央値に対する信頼区間は作成していない。

| 呼び出し | 入力・経路 | 元List | 遅延配列 | 時間短縮 | 割り当てB/op（元→変更後） |
|---|---|---:|---:|---:|---:|
| ParseReused | `false,` 成功 | 70.79 | 49.87 | 29.6% | 176 → 72 |
| ParseReused | `0,` 先頭失敗 | 22.50 | 14.17 | 37.0% | 32 → 0 |
| ParseReused | `falsX,` 末尾失敗 | 46.80 | 41.15 | 12.1% | 64 → 40 |
| ConstructAndParse | `false,` 成功 | 287.11 | 270.55 | 5.8% | 600 → 496 |
| ConstructAndParse | `0,` 先頭失敗 | 234.88 | 226.02 | 3.8% | 456 → 424 |
| ConstructAndParse | `falsX,` 末尾失敗 | 263.33 | 258.77 | 1.7% | 488 → 464 |

生成込みの中央値の差は小さく、起動内の99.9%信頼区間が重なる。先頭・末尾失敗では起動別平均も元版と変更後の範囲が重なり、一部で逆転している。
したがって、生成込みの速度改善は再利用時と同じ強さでは断定しない。割り当ての減少は全経路で観測した。
これは5文字の単語パーサーの比較であり、JSON全体、他の単語長、コールド実行の改善を確認した結果ではない。
遅延配列は最初の成功時に単語全長を確保するため、長い単語が途中で早期失敗する場合の割り当て優位は保証しない。

## 採用判断と、他の方式を使わない理由

この処理では成功時の文字数が `Parsers.Length` に一致し、蓄積先に可変長の機能は必要ない。
読み取った文字を保持する責務は残し、必要な長さの配列へ順に格納する。最初の文字の成功まで確保を遅らせることで、先頭失敗と空単語では配列を作らない。
既存の `CharParser` への委譲や空白処理を崩さず、本体同士の比較でも再利用時の時間・割り当てが改善したため、遅延配列を採用した。

採用しなかった候補の根拠は [全10方式の比較](2026-10-05-word-parser-accumulation.md#最終比較) にある。
以下の時間・割り当ては候補比較の値であり、今回の本体比較と混ぜて集計しない。

- `List<char>`: 可変長の蓄積機能が不要で、元実装は最後に `[.. returnValue]` で別の配列を作る。遅延配列はこの具体化を省ける。本体比較の成功時は176→72 B/opで、再利用の3経路すべてで平均時間が短縮した。ただし、時間差のすべてを特定の割り当てやコピーの費用と断定する分析はしていない。
- 容量指定List: 拡張を避けてもListと最後の配列化は残る。候補比較の成功時は144 B/op、67.45 nsで、遅延配列の72 B/op、48.85 nsより多く、遅かった。末尾失敗は信頼区間が重なるので、全経路で明確に劣るとは断定しない。
- StringBuilder（既定容量・容量指定）: 候補比較で成功・先頭失敗・末尾失敗の平均時間が遅延配列より長く、割り当ても多かった。今回の固定長の文字蓄積で選ぶ根拠がなかった。
- ジェネレーター＋Concat／配列化: 候補比較では両方とも3経路で平均時間と割り当てが遅延配列を上回った。ジェネレーターにすれば中間データの費用を必ず減らせる、とは扱わない。
- 結果ジェネレーター＋再帰ラムダ: 成功した文字をキャプチャに積み、全成功後に `string.Create` で書き込む案も実際に比較した。成功時624 B/op、131.81 nsで、遅延配列より割り当てが多く、遅かった。
- 最初から確保する配列: 候補比較では遅延配列より3経路の平均時間が長かった。先頭失敗にも40 B/opを確保するため、その経路で確保を省ける遅延版を選んだ。成功・末尾失敗の時間差の原因は未分析。
- `stackalloc`＋`Span<char>`: 小さくメソッド内で使い終わる一時バッファなので、有望な候補として比較した。候補版は256文字以下ならスタック、それより長ければ配列を使う。成功時の割り当ては32 B/opと遅延配列の72 B/opより少なかったが、時間は81.79 ns対48.85 nsで遅く、先頭・末尾失敗の平均時間も長かった。今回の速度目的では採用しない。遅くなった原因やJIT生成コードは未調査であり、Spanやstackalloc一般の優劣とは解釈しない。

これはListなどを一般に禁止する方針ではなく、この固定長の蓄積処理と測定条件での選択である。
今後この箇所をListなどへ整理し直す場合は、上記の削減対象と測定記録を参照し、同じ仕事量で時間・割り当てを比較する。
長い単語の途中失敗、JSON全体、異なるランタイムについては別の確認が必要。

## 起動別の結果

元版は `01-list / 04-list / 05-list`、変更後は `02-deferred / 03-deferred / 06-deferred` の順。単位はns/op。
表示のみ小数第2位に丸め、CSVには元の数値精度を残した。

| 呼び出し・入力 | 元版の3起動平均 | 変更後の3起動平均 |
|---|---:|---:|
| ParseReused / `false,` | 70.79 / 74.32 / 70.56 | 49.87 / 49.58 / 53.19 |
| ParseReused / `0,` | 23.23 / 18.75 / 22.50 | 14.26 / 14.17 / 12.53 |
| ParseReused / `falsX,` | 47.08 / 46.80 / 44.20 | 40.05 / 41.74 / 41.15 |
| ConstructAndParse / `false,` | 287.11 / 299.70 / 283.55 | 270.55 / 270.74 / 258.87 |
| ConstructAndParse / `0,` | 230.92 / 234.88 / 237.37 | 237.72 / 225.87 / 226.02 |
| ConstructAndParse / `falsX,` | 264.20 / 256.63 / 263.33 | 257.75 / 258.77 / 265.49 |

多峰性の警告はなかった。最短反復時間の警告が1件あり、`04-list / ParseReused / Text=falsX,` のActual反復は最短67.663msだった。
この反復もOutlierMode=DontRemoveのまま残した。末尾失敗の一部には大きな反復誤差があり、ばらつきなしとは扱わない。
実行エラー、実測0件、欠測はなく、6runすべて6ケース・N=8だった。

## 実装と比較方法

[WordParser.cs](../../Kotowari/Core/WordParser.cs) の蓄積処理を、Listから必要長の配列へ変更した。
最初の文字が成功した時点で配列を確保し、読み取った文字を順に格納して、全成功後に文字列を作る。
空単語は `string.Empty`、先頭失敗は配列を確保しない。
`CharParser` への委譲、空白処理、カーソル、実際に読み取った文字の保持、公開APIは維持した。
文字照合の手書きインライン化や、入力文書の切り出しはしていない。

元実装は `15dbfedbb6df18d816e8264d66f7798d8338ade7:Kotowari/Core/WordParser.cs` のGit blobそのもの。
他のソース・設定は両版で現在の作業ツリーを使い、了承済みのnet10.0移行差分も共通にした。
固定コピーのライブラリ・共有設定・測定・互換性テスト計29ファイルを対にしてSHA-256を確認し、差がWordParserだけであることを測定前後に確認した。
比較専用の継承階層、sealed化、配列経由の追加ディスパッチなどは入れていない。
実際の型名・公開範囲とライブラリのAssemblyName `Marimo.Kotowari` を両版で維持した。

ベンチマークは既存の [WordParserBenchmarks.cs](../WordParserBenchmarks.cs) 全文を変更せず使用した。
`ParseReused` は構築済みの本体、`ConstructAndParse` は毎回 `new WordParser("false", true)` を生成して解析する。
入力とカーソルの生成は測定区間外。GlobalSetupで成功・失敗、値、カーソルを検証し、測定メソッドからParse結果を返す。
前回候補比較の絶対値を今回の元版として流用せず、両版を測り直した。

## 環境・条件・入力

- CPU: Intel Core i7-13700H、1 CPU / 14 physical cores / 20 logical cores。
- OS: Windows 11、10.0.26200.9457 / 25H2。
- SDK: 10.0.401。Runtime: .NET 10.0.12（10.0.1226.42308）、X64 / RyuJIT。
- BenchmarkDotNet: 0.15.8。Release、MemoryDiagnoser、デバッガー接続なし。
- 通常のランタイム設定。段階的JIT無効の環境変数を渡していない。
- 各run: Job=Short、LaunchCount=1、WarmupCount=10、IterationCount=8、IterationTime=500ms（目標値）、InvocationCount自動、UnrollFactor既定16。
- Affinity=4（ビットマスク `00000000000000000100`）、OutlierMode=DontRemove、JSON exporter、join、keepFiles。
- 同時の開発・ビルド・テストを止め、下記6runを直列に実行。BDN自身の測定ホスト生成buildは含む。
- 実行順はList→変更後→変更後→List→List→変更後（ABBAAB）。各版・各ケースで3独立起動。
- 全測定は約9分48秒。前後で実装を変更していない。

先頭と末尾の空白には既定パーサーを使用し、`ignoreCase=true`。
入力末尾のカンマによりEOF例外の費用を比較へ入れない。失敗経路は意図した失敗を測っている。
入力はUTF-8、BOMなし。ASCIIなので文字数とbytesは一致する。

| Text | 経路 | bytes | SHA-256 |
|---|---|---:|---|
| `false,` | 成功 | 6 | `3721E7F2DB98C23F7E5BFCA0220E57FF4A8F3EAF59BBE3A7D579EC0720BAE680` |
| `0,` | 先頭失敗 | 2 | `0A5A2432405B31B9031C2254D5B84445FC1B2D9513C6E16370607C909F6FD210` |
| `falsX,` | 末尾失敗 | 6 | `4E977F8C300B43D2F44D9B510136AE3A5C160A5924C2F7EA979594300BA8D1F6` |

## ソースの版

基点は上記コミット。変更後の本体とこの記録は一緒に保存する。未コミット時は次の作業ツリーSHA-256で特定する。
元WordParserはGit blobのBOM付きUTF-8/LF、変更後は作業ツリーのBOM付きUTF-8/CRLF。
測定後、本体に採用理由とこの記録への参照のコメント4行を追加した。測定時の固定コピーは変更せず、測定時とコメント追加後のSHA-256を分けて記録する。

| ファイル | SHA-256 |
|---|---|
| 元WordParser.cs | `A3AE30CEB5CCB7EF44FFC05D313706BA9900C202E95FF6A37380DD0D7FBFAA3A` |
| 変更後WordParser.cs（測定時の本体・固定コピー） | `5AD7434AA233F59002E3A7AF78976B08A02DCC63BE0EAF1EFF64C7570AFB80AF` |
| 変更後WordParser.cs（採用理由コメント追加後の本体） | `F5787D24D135E47D33BFD697661444B8D86A98E6DC958B6ABCF07614FDED415A` |
| 共通WordParserBenchmarks.cs | `29C9C5E87C29D15AAAA277E26285043E83E74EE656CF200273250502BB74517B` |
| 共通ライブラリcsproj（net10.0移行差分込み） | `9E3F391585776526DECFDE4FA4B9FCE79A7530A2E1136732ACB093C7F9774798` |
| 測定hostの共通csprojテンプレート | `D157E3CD228645E98D15927364801F2B384061EA9249D1B320A9EE68C3D089CA` |
| 測定hostの共通Program.cs | `A2D40478F6332EE0E96B4C211E487E79CB2EF0B58415E4EFB68AE3F60998DC57` |
| source-manifest.json | `6999F511F00FAE9F7C5A382C4FC6C58BCE2C50BC10302B004BB344727F7CCD9C` |

開始時にあった未コミットConcat試行も、書き換え前にsnapshotへ保存した（SHA-256 `9C302B3BFCD48D89EEE9DE6E4594B6FB9C994CCC4E5D2245B20FBE0BFC6A4034`）。
両ホストと両ライブラリのRelease属性・JIT最適化有効を確認した。
ホストの `DisableOptimizationsValidator` は既存の入口と同じ設定であり、最適化を無効にするものではない。

## 再現方法とraw記録

一時測定用の固定コピー・テンプレート・準備スクリプトはGit対象外の `artifacts/word-parser-production-deferred-array-2026-10-05/` に保持する。
`snapshots/list/` と `snapshots/deferred/` に、現在のライブラリ、共有設定（global.json、.editorconfig、Directory.Packages.props、README）、既存測定クラス、WordParserの既存テストを同じ相対位置でコピーした。
List側のWordParserだけを基点Git blobのbytesで置き換えた。生成物はコピーせず、それぞれの固定ソースからビルドした。
記録を将来再現するときも、このソース版を使い、WordParser以外の差がないことを先に検証する。

ホストは各 `snapshots/<variant>/host/` に置き、同名csprojの再帰探索衝突を避けるためファイル名だけを `WordParserProduction.list.Measurement.csproj` / `WordParserProduction.deferred.Measurement.csproj` と分けた。
内容は次の共通テンプレート。中央管理のBenchmarkDotNet 0.15.8を継承する。

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <RootNamespace>Marimo.Kotowari.Benchmarks</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" />
    <ProjectReference Include="../Kotowari/Marimo.Kotowari.csproj" />
    <Compile Include="../Kotowari.Benchmarks/WordParserBenchmarks.cs" Link="WordParserBenchmarks.cs" />
  </ItemGroup>
</Project>
```

共通Program.cs:

```csharp
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Marimo.Kotowari.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        var config = DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator);
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}
```

各ホストをReleaseでビルドして、リポジトリrootから次のコマンドを実行する。`<variant>` は `list` / `deferred`、`<run>` は下記順序に対応する。
実測では既存のローカルNuGetキャッシュを復元元に指定し、新しい依存追加や外部パッケージ取得はしていない。

```powershell
dotnet artifacts/word-parser-production-deferred-array-2026-10-05/snapshots/<variant>/host/bin/Release/net10.0/WordParserProduction.<variant>.Measurement.dll --filter '*WordParserBenchmarks*' --job Short --launchCount 1 --warmupCount 10 --iterationCount 8 --iterationTime 500 --affinity 4 --outliers DontRemove --exporters json --artifacts artifacts/word-parser-production-deferred-array-2026-10-05/<run> --keepFiles --join
```

実行順は `01-list / 02-deferred / 03-deferred / 04-list / 05-list / 06-deferred`。
既存のrunディレクトリがある場合は実行を停止し、再測定で上書きしない。各runの終了コードだけでなく、完全JSONに6件・N=8の結果があることを検証した。

各runの完全JSONは `<root>/<run>/results/Marimo.Kotowari.Benchmarks.WordParserBenchmarks-report-full-compressed.json`。`<root>` は上記artifact root。

| Run | 完全JSONのSHA-256 |
|---|---|
| 01-list | `94C9884411537D19A1D5A07EEC0D025C230A89A233E56AF33F8A745917EDC86C` |
| 02-deferred | `355DBCA505F9DAB63BD9E5ACB3970CFF2F4D186A147837E8D5843B4EC6CAA448` |
| 03-deferred | `3A8E0804A454AB0037CD6405BB962E074D5B52B1694FD7A37F3AB35606A9B0CE` |
| 04-list | `776739672DA0DE6E6D9E3B385A7E40A458B25B0E021A6E6F2752687569511326` |
| 05-list | `87FD0B1F9A88D4FEEFF6E77D691F6B6D81E4E710645CD0ACDA864467FCC435B7` |
| 06-deferred | `20AA8201E1894CDBCAF1502659120265D86C52B1B071482D7119DDA077593FC0` |

## CSV保存と検証

`Experiment=2026-10-05-production-deferred-array` として既存の [summary.csv](word-parser/summary.csv)、[iterations.csv](word-parser/iterations.csv)、[launches.csv](word-parser/launches.csv) に追記した。CSVを新設せず、stacksは変更していない。
各runのVariantはList / DeferredArray。summaryは36行、iterationsはWorkload Actual/Resultの576行（288反復）、launchesは36行。
各runはLaunchCount=1 / N=8。3回分を架空のLaunchCount=3ジョブとして記録せず、Runで独立実行を識別する。
summaryのErrorは各実行のBDN 99.9%信頼区間半幅であり、前述の中央値に対する誤差ではない。
起動別統計はResultの時間/操作数から計算し、この実験では1起動だけのメモリ診断を対応するlaunch行にも保存した。

測定前の検査:

- 実本体の既存WordParserテスト25件成功（8.53秒）。元・変更後の固定コピーにも既存25件を適用し、両方成功（復元込み10.05 / 10.10秒）。
- `dotnet test Kotowari.sln -c Release --no-restore -p:UseSharedCompilation=false`: 全242件成功、失敗・Skip 0、7.77秒。
- Releaseソリューションbuild成功、3.87秒。既存SpracheJSON 1.0.1のNU1701警告あり。
- 規定の非増分Analyzer build成功、16.54秒。5プロジェクトのSARIFでは既存7診断のみ、WordParserのParseCoreに新規診断なし。
- 全体の `dotnet format Kotowari.sln --no-restore --verify-no-changes --severity info` は失敗（exit 2）、10.68秒。既存73指摘で、WordParserは未変更constructorのIDE0290のみ。ParseCoreへの指摘はない。無関係な指摘を一括修正していない。
- 最終returnの改行だけを反映した後、25件のテストを意味なく繰り返さず、残りの検査と測定ホストビルドを最終ソースで実行した。
- 振る舞い維持の変更なので形式的なRedは作らず、既存契約を弱めていない。

測定終了時はソースhash不変を確認した。その後、本体へ採用理由コメント4行だけを追加し、追加行を除いたソースが測定時の固定コピーと一致することを機械的に確認した。
実行コードは変更していないため、コメント追加だけを理由としたbuild/test・性能測定の再実行はしていない。
CSVは読み取り専用で再インポートし、summary 26列・246行、iterations 15列・6,148行、launches 19列・386行を確認した。
今回の36ケースについて、summaryとlaunchの平均・割り当て・GCが一致し、Actual/Result各288行の反復数・単位・時間計算が整合した。
追記前のCSV部分はSHA-256がすべて一致し、既存のbytesを変更していない。
最終差分検査は成功。測定時の固定コピーは元のSHA-256を維持し、コメント追加後の本体も上記SHA-256で特定した。既存のnet10.0移行・候補比較の未コミット差分は保持した。
ステージング・コミット・pushはしていない。
