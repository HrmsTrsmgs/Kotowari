# JSON文字列化の二重配列コピー削減 2026-10-19

基準 `d2f7aa8` に対し、JSONサンプルの文字列化直前のchar配列コピーを省いた。割り当ては約9.4%減ったが、PGO有効の8組比較では約5.8%遅くなった。速度改善として不採用とし、差分・測定記録を保存して撤回した。

## 変更と削減の確認

DigitsとJStringの `new string([.. chars])` 相当を、次の非公開ヘルパーへ集約した。

```csharp
static string CreateString(IEnumerable<char> chars)
    => new(chars is char[] array ? array : [.. chars]);
```

反復パーサーは既に所有権の独立したchar配列を返すため、配列ならそのままstringへコピーする。別のIEnumerableには従来の具体化を残す。反復結果の配列と最終stringは引き続き生成し、pool配列を外へ返す変更ではない。ライブラリ本体・公開API・文法は変更していない。先に試した[再帰定義案](2026-10-19-recursive-definition.md)は撤回済みで、本候補には混ぜていない。

新たに採取したTier 1では、基準側に `Enumerable.ToArray<char>` が残り、候補のCreateStringでは配列一致枝が直接 `string(char[])` を呼ぶ。ToArrayは非配列/null側のfallbackだけになった。削減対象のコピーが残ったままの比較ではない。

両版のCPU/AllocationTick診断は8 Actual、EventsLost=0、割り当てスタック欠落0。char配列の標本は3,165から1,769へ減り、候補にはCreateStringからのstring生成が見える。標本数は正確な割り当て回数ではなく、全char配列の削除を意味しない。診断時間を通常計時へ混ぜていない。

## 条件と結果

入力は既存 `Data/test.json`、4,172,062 bytes、SHA-256 `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。MemoryStreamの作成・コピー、stringへの復号、全文解析と結果木生成を含み、物理ディスク読み取りは含めない。両版の固定hostで全文消費・100,001ノード・System.Text.Jsonとの木の一致を確認した。専用JSONパーサーとの速度比較ではない。

SDK 10.0.401、.NET 10.0.12、BenchmarkDotNet 0.15.8、Windows 25H2 build 26200.9711、i7-13700H。PGO有効、affinity mask 4、warmup 16、Actual 8、invocation 4、unroll 1、DontRemove。新規プロセス8組をAB/BA交互に起動し、起動ごとの8 Result平均を単位として、対応log時間比の95% t区間（自由度7）を計算した。

| 系列 | 組数 | 基準 ms/解析 | 候補 ms/解析 | 対応時間比 | 95%対応log-t区間 |
|---|---:|---:|---:|---:|---:|
| 探索 | 8 | 133.514 | 141.146 | 1.057574 | [1.031769, 1.084025] |

区間全体が1を超え、この条件では約3.2〜8.4%の悪化を支持する。事前の採用条件を満たさず独立確認は行っていない。平均割り当ては49,844,570.5から45,163,541.25 bytes/解析へ、4,681,029.25 bytes（約9.39%）減少した。コピー・割り当ての削減と速度の改善は一致しなかった。悪化の原因をヘルパー呼び出しやPGOの特定挙動に確定する調査は行っていない。

全16起動はexit 0、各Actual/Resultは8反復、各反復4操作で、入力・DLLハッシュを確認した。実行中は親のビルド・テストを重ねず、既存の期限付きスリープ抑止を継続し、AC接続・省電力無効・heartbeatをブロック前に確認した。

## 検証と保存

空・1文字・長いBMP文字列と数字列の計5ケースを追加し、実装変更前のJSON62件、変更後の全310件（本体248、JSON62）が成功、失敗・Skipなし。Release非増分ビルドとアナライザーは12.34秒で成功。既存NU1701とSARIFの既存提案6件が残る。formatは既存66診断でexit 2、前候補と位置番号を除いた内容・件数は同じで、変更箇所に新規診断はない。

固定hostの28 DLLはJSONサンプルだけが異なる。本体DLLは両版とも `711A6D91E2BF0132ABB50DEC81A5B80555CDDF2BD0F4D3B8F5C849AC2F676BA8`、候補JSON DLLは `E6265A77370B228E24ACCD2E7C08A2682C227B1E9BFF9DD030E8BB9443F6F820`。43ファイルのソースを固定し、JSON.csだけの実装差分と、追加テストの差分を保存した。

JIT準備では複製済み旧出力への上書き拒否と、候補JSONのハッシュを本体ハッシュへ誤適用した検査失敗があった。修正後の基準capture-03・候補candidate-03だけを今回の生成コードの証拠として使い、旧出力・失敗試行を新しい正常採取と区別した。通常計時は準備完了後の別起動である。

[証拠JSON](experiments/2026-10-19-char-materialization.json)に版、全観測、集計、診断、検証、候補差分、再生スクリプトを保存した。原本は `artifacts/string-char-materialization-2026-10-19/`。保存資料だけからのクリーン再構築・再実行は未検証。

今回の実装と追加テストを撤回し、本体・JSONサンプル・テストがHEADに一致することを確認してから次候補へ進んだ。既存の未コミット記録は維持し、コミット・pushはしていない。

後続候補まで継続した共通のスリープ抑止は03:51:06 UTCに正常解除した。最終状態は[空白判定案の証拠JSON](experiments/2026-10-19-whitespace-predicate.json)にも保存した。
