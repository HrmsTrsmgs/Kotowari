# 巨大JSONのパーサーコンビネーター比較（2026-10-05）

## 結果

用意済みの `Kotowari.Benchmarks/Data/test.json` を変更せず、現在のKotowariとPidgin・Sprache・Superpowerで全文を解析し、結果の木を生成した。
System.Text.JsonなどのJSON専用パーサーは測定していない。
SpracheJSONはSpracheで構築された旧JSON実装として残すが、生成ノード数が異なるため主比較から分ける。

時間はBenchmarkDotNetの平均・99.9%信頼区間半幅。各3独立起動×8測定反復（N=24）であり、24回の独立起動ではない。
割り当ては1回の解析あたりの累積マネージド割り当て。表示のMiBは1,048,576 bytesで、CSVには元の精度のnsとbytesを残した。

| 主比較 | Mean (ms) | Error (ms) | StdDev (ms) | 割り当て (MiB) |
|---|---:|---:|---:|---:|
| Kotowari | 176.87 | 24.013 | 31.224 | 57.54 |
| Pidgin | 39.78 | 0.328 | 0.427 | 13.81 |
| Sprache | 588.20 | 79.730 | 103.672 | 2069.35 |
| Superpower | 203.33 | 4.798 | 6.239 | 173.89 |

| 旧実装・仕事量が異なる参考比較 | Mean (ms) | Error (ms) | StdDev (ms) | 割り当て (MiB) |
|---|---:|---:|---:|---:|
| SpracheJSON 1.0.1 | 926.33 | 115.867 | 150.659 | 2543.35 |

今回のサンプル・既存JSONアダプターでは、Pidginの3起動平均すべてがKotowariの最小起動平均を下回り、Spracheの3起動平均すべてがKotowariの最大起動平均を上回った。
Kotowariの平均はSuperpowerより低いが、信頼区間・起動別平均が重なるので、小さな差や安定した順位としては断定しない。
割り当てはKotowariがSprache・Superpowerより少なく、Pidginより多かった。

| 対象 | 起動1の平均 (ms) | 起動2の平均 (ms) | 起動3の平均 (ms) |
|---|---:|---:|---:|
| Kotowari | 133.99 | 197.52 | 199.10 |
| Pidgin | 39.59 | 39.82 | 39.93 |
| Sprache | 521.12 | 518.80 | 724.67 |
| Superpower | 202.73 | 208.83 | 198.43 |
| SpracheJSON | 904.60 | 1010.72 | 863.67 |

Kotowariに多峰性の警告（MValue=3）が1件あった。起動間の差の原因は未調査。
Pidginの2反復、SpracheJSONの3反復に外れ値の検出があるが、OutlierMode=DontRemoveなので除外していない。最短反復時間の警告はない。
同じサンプルでもJSON文法の組み立て・結果型は異なるため、ライブラリ全般の速度順位や任意のJSONへの仕様準拠を保証する比較ではない。
今回は元List版を同条件で測り直していないので、WordParserの遅延配列化によるJSON全体の改善率は不明。
SDK・Runtime・ウォームアップなどが異なる過去の絶対値から、その変更の効果を計算しない。

## 入力・版・環境

- 入力: `Kotowari.Benchmarks/Data/test.json`。4,172,062 bytes / 4,172,062 UTF-16コード単位。
- 入力SHA-256: `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。既存サンプルと一致し、縮小・再生成・置換なし。
- 測定ソース: コミット `8562efc0757e9c936693b3cb2ced3aef1febe511`。本体・JSONアダプター・既存ベンチマークは変更していない。
- CPU: Intel Core i7-13700H 2.90GHz、1 CPU / 14物理 / 20論理コア。
- OS: Windows 11、10.0.26200.9457 / 25H2。
- SDK: 10.0.401。Runtime: .NET 10.0.12（10.0.1226.42308）、X64 RyuJIT x86-64-v3。
- GC: Concurrent Workstation。Release、MemoryDiagnoser、通常のランタイム設定。
- Affinity: CLI値4 / ビットマスク `00000000000000000100`。段階的JITを無効にする環境変数なし。
- BenchmarkDotNetが高パフォーマンス電源プランへ切り替え、終了時にバランスへ復元した。
- ユーザーが他の重い作業を停止してよいと回答した後に直列実行。測定中の開発・検証ビルドは行っていない（BDN自身の測定ホストbuildを除く）。

中央管理設定と復元済み `project.assets.json` によりバージョンを確認した。依存追加・更新なし。

| 対象 | バージョン |
|---|---|
| Kotowari / 自作JSONアダプター | 上記コミット |
| BenchmarkDotNet | 0.15.8 |
| Pidgin | 3.5.1 |
| Sprache | 2.3.1 |
| Superpower | 3.2.1 |
| SpracheJSON | 1.0.1（lib/net47の旧アセンブリを明示参照） |

SpracheJSONは既存のNU1701警告を伴う。既存入口と同じ `DisableOptimizationsValidator` 設定で測定しており、測定ホストの生成buildはRelease / Optimize=trueだった。

## 測定前後の正しさの確認

測定前に全5実装の解析成功とノード生成を確認した。
Kotowariと旧SpracheJSONは、測定外の検証で内部JObjectパーサーの消費位置が4,172,062 / 4,172,062であることを確認した。
PidginのDocumentはEnd、SpracheのDocumentはEnd()、SuperpowerのValueはAtEnd()を要求しており、それらの既存実装を確認した上で同じサンプルの成功を検証した。

測定後、主比較4実装の全キー・値・配列順序も検証した。
オブジェクトのキーをOrdinal順へ並べ、配列順序は保持し、各ノードの種類・要素数と文字列長付きの値をBinaryWriterで記録する。
数値は生成された文字列表現、真偽値は小文字表現、nullは種類だけを比較し、この正規化結果のSHA-256が4実装で一致した。
この検証・並べ替え・ハッシュ計算は測定区間に含めない。

| 対象 | ノード数 | 正規化した木のSHA-256 |
|---|---:|---|
| Kotowari / Pidgin / Sprache / Superpower | 各100,001 | `5ABDB9015EF96B7E79FADB54B636AF0C5AB9D0AA1864C5AF21FF8669C2A4233C` |
| SpracheJSON | 104,120 | 主比較と同じ木とは扱わない |

検証コードは `artifacts/json-combinators-2026-10-05/validation/` に保持する。途中の検証補助コードに列挙型のキャスト誤りがあり、修正した。また失敗した補助プロセスの実行ファイルロックを解消し、UseAppHost=falseでbuildした最終版の検証はexit 0で成功した。
これらは測定終了後の補助検証であり、本体・比較アダプター・測定データを変更していない。

## 条件と再現方法

全5対象を同じジョブ・同じ入力・同じ環境で測った。
Job=Short、LaunchCount=3、WarmupCount=10、IterationCount=8、IterationTime=500ms（目標値）、InvocationCount自動、UnrollFactor=1、OutlierMode=DontRemove。
1反復の操作数は各起動4で、実際の反復時間は500msを超える場合がある。
通常のSystem.Text.Json測定メソッドはfilterから除外した。ファイル読み込みはGlobalSetupにあり、計測区間は解析と結果生成のみ。
5対象×3起動の15プロセスがexit 0、各対象N=24。欠測・実行失敗なし。BDN全体は545.95秒（約9分6秒）。

最初の通常ホスト実行は、以前保存したコピー内の同名csprojと衝突し、生成失敗・実測0件だった。
その失敗は `artifacts/json-combinators-2026-10-05/run/` に残し、主結果へ混ぜていない。
固有名の一時ホスト `artifacts/json-combinators-2026-10-05/host/JsonCombinators.Measurement.csproj` から既存ソースをリンクして再実行した。
本体や測定クラスを複製・書き換えず、ホストのAssemblyNameだけが通常入口と異なる。

一時ホストの内容（既存中央パッケージ設定を継承）:

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
    <PackageReference Include="Pidgin" />
    <PackageReference Include="Sprache" />
    <PackageReference Include="SpracheJSON" GeneratePathProperty="true" ExcludeAssets="compile;runtime" />
    <PackageReference Include="Superpower" />
    <Reference Include="SpracheJSON">
      <HintPath>$(PkgSpracheJSON)\lib\net47\SpracheJSON.dll</HintPath>
      <Private>true</Private>
    </Reference>
    <ProjectReference Include="../../../JsonParser/JsonParser.csproj" />
    <Compile Include="../../../Kotowari.Benchmarks/Program.cs" Link="Program.cs" />
    <Compile Include="../../../Kotowari.Benchmarks/JsonParserBenchmarks.cs" Link="JsonParserBenchmarks.cs" />
    <Compile Include="../../../Kotowari.Benchmarks/ComparisonJsonParsers.cs" Link="ComparisonJsonParsers.cs" />
    <None Include="../../../Kotowari.Benchmarks/Data/test.json" Link="Data/test.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

リポジトリrootから一時ホストをReleaseでbuildした後:

```powershell
dotnet artifacts/json-combinators-2026-10-05/host/bin/Release/net10.0/JsonCombinators.Measurement.dll --filter '*JsonParserBenchmarks.Kotowari' '*JsonParserBenchmarks.Pidgin' '*JsonParserBenchmarks.Sprache' '*JsonParserBenchmarks.Superpower' '*JsonParserBenchmarks.SpracheJsonPackage' --job Short --launchCount 3 --warmupCount 10 --iterationCount 8 --iterationTime 500 --unrollFactor 1 --affinity 4 --outliers DontRemove --exporters json --artifacts artifacts/json-combinators-2026-10-05/run-unique-host --keepFiles --join
```

実測ではRestoreSourcesを既存ローカルNuGetキャッシュに設定し、外部パッケージ取得を行っていない。
再測定時は既存のrunディレクトリを上書きせず、空の保存先を使う。

## 保存と検証

rawログ・完全JSON・一時ホスト・検証補助コードは上記Git対象外のartifact rootに保持する。
完全JSON: `run-unique-host/results/Marimo.Kotowari.Benchmarks.JsonParserBenchmarks-report-full-compressed.json`。
SHA-256: `A86527C44B58462874D9BAAA0554CE253D5698FE39AE8AE96868B98647005AE5`。
ログ: `run-unique-host/Marimo.Kotowari.Benchmarks.JsonParserBenchmarks-20261005-070012.log`。

| 測定関連ソース | SHA-256 |
|---|---|
| 一時ホストcsproj | `59DD7A303970B7E92CAA92255F10F12A5B2A527B9334CA8A7B19BC48373553B1` |
| JsonParserBenchmarks.cs | `F045C5B691F28840C5DCDCDB5F2BCE275DC82FA4A1E60751D8FB35A134189846` |
| ComparisonJsonParsers.cs | `885EFC98E1FD93C6D93AF1E0FF07459A678D8EACEE312926C0C92E6E401A4CB8` |
| 既存Program.cs | `A246F4B4423165B9E2DE10578BB2E34E80A3106C244F06EB29B0D56B5EBB21FB` |

`Experiment=2026-10-05-json-combinators`、`Run=normal-warmed-comparison` として、既存の [summary.csv](word-parser/summary.csv) に5行、[iterations.csv](word-parser/iterations.csv) に240行、[launches.csv](word-parser/launches.csv) に15行を追記した。
WordParser変更後のJSON全体の確認も既存データ群へ含め、新しいCSVを増やしていない。過去の各実験と混ぜて平均を作っていない。
Actual/Result各120行は120測定反復を示す。同じ反復の補正前後を240回の独立試行とは扱わない。
起動別統計はResultの総時間/操作数から計算した。全体のメモリ診断を各起動へ複製せず、launchesの割り当て・GC欄は空欄とした。

ベンチマークのRelease build、固有名ホストのRelease build、最終補助検証は成功。既存NU1701警告あり。
本体・アダプター・既存テストのソース変更はなく、直前の全242テスト・Analyzer検証を測定のためだけに繰り返していない。
CSV読取監査は列数26/15/19、実験別行数と時間単位を確認した。追記前のbytesのSHA-256一致、各実装の3起動×Actual/Result各8反復、操作数による時間換算、起動平均と全体平均の一致、起動別メモリ欄の空欄を確認した。専用JSONパーサーの行は含まない。`git diff --check` は成功し、変更は記録と既存CSV・索引のみ（Gitの既存改行変換警告あり）。
コミット・pushは行っていない。
