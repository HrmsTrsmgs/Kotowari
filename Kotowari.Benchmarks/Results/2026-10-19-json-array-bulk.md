# JSONArrayの一括構築の評価 2026-10-19

基準は `e9be3daba67610437bd3d1b6e19004279f95a775`。JSONサンプルのJSONArrayを逐次Addから一括構築へ変更し、約0.58 MB/解析の割り当てを削減したが、速度改善の事前基準を満たさなかった。候補差分と追加テストを証拠へ保存して撤回した。速度が同じ、または確実に悪化したという結論ではない。

## 変更・根拠・契約

```csharp
// 変更前
public JSONArray(IEnumerable<IJSONValue> elements)
{
    Elements = [];
    if (elements is not null) foreach (var e in elements) Elements.Add(e);
}

// 候補（撤回済み）
public JSONArray(IEnumerable<IJSONValue> elements)
    => Elements = elements is null ? [] : [.. elements];
```

[候補調査](2026-10-19-next-candidates-investigation.md)では、JSONArrayのList.AddWithResizeに由来する割り当てが341標本あった。標本数は呼び出し回数ではない。現在の基準版のTier 1コードでも、入力配列の直接走査に続く逐次Add・容量判定・条件付きAddWithResizeを確認した。配列用経路は既に列挙子を使っていないため、列挙子削除を効果の根拠にはしない。

候補のTier 1コードではICollectionのCountを利用した容量確保とCopyToの経路になった。JSON文法のJElementsはDelimitedSequenceParserから確定済みのIJSONValue配列を返すため、この経路が対象になる。一般のIEnumerable用fallbackには逐次Addが残る。入力構造に基づく算術モデルでは、各JSON配列を一度構築すると仮定すると成長用配列5,372個と旧配列からの28,404要素のコピーを避けられる。これらは動的カウンターによる実測ではない。

値の参照・順序・独立した可変格納先、空列と既存のnull受理、イテレーターの一回の走査・解放・例外伝播を検証した。既存null受理を開発者の意図したAPI仕様とは認定せず、nullable注釈は変えていない。

一方、公開されるElements.Capacityは変わる。長さ1/3/5/10で基準は4/4/8/16、候補は1/3/5/10だった。その後に要素を追加する場合の再確保や、独自ICollectionのCount/CopyToと列挙のどちらが呼ばれるかも変わり得る。すべての観測可能な動作が同じとは扱わず、追記の多い用途の高速化も主張しない。本体・文法・公開メンバーの宣言は変更しなかった。

## 測定条件と結果

入力は既存 `Data/test.json`、4,172,062 bytes、SHA-256 `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。MemoryStreamの作成・コピー、UTF-8からstringへの復号、全文解析と結果木生成を含む。物理ディスク読み取りは含めない。両版とも全文消費・100,001ノード・System.Text.Jsonとの木の一致を確認した。専用JSONパーサーとの速度比較ではない。

SDK 10.0.401、.NET 10.0.12、BenchmarkDotNet 0.15.8、Windows 25H2 build 26200.9711、i7-13700H。PGO有効、affinity mask 4、warmup 16、Actual 8、invocation 4、unroll 1、DontRemove。8組16新規起動をAB/BA交互に実施。各起動の8 Result平均を単位として、8個の対応log時間比の95% t区間（自由度7、t=2.364624251）で判定した。ActualとResultを別々の独立標本として数えず、外れ値の除外・組数延長・別系列との合算は行わない。

| 系列 | 組数 | 基準 ms/解析 | 候補 ms/解析 | 対応時間比 | 95%対応log-t区間 |
|---|---:|---:|---:|---:|---:|
| 探索 | 8 | 119.589 | 121.183 | 1.013182 | [0.988733, 1.038236] |

区間は約1.13%短縮から約3.82%悪化まで含む。上限が1未満という条件を満たさず、独立確認には進まなかった。平均割り当ては49,844,350.0から49,265,540.5 bytes/解析へ、578,809.5 bytes（約1.16%）減少した。割り当て削減と速度改善を同一視せず、今回は速度改善として不採用とする。個々の処理やPGOが時間差へ与えた寄与は分離していない。

固定hostの28 DLLで異なるのはJSONサンプルのMarimo.Parser.dllだけ。本体DLLは両版とも `711A6D91E2BF0132ABB50DEC81A5B80555CDDF2BD0F4D3B8F5C849AC2F676BA8` を再使用した。Luna medium担当が直列実行し、親のビルド・テストを重ねていない。各組の起動前ログはすべて取得でき、AC接続・省電力無効・helper稼働と、3秒窓で別dotnetのCPU活動がないことを確認した。測定中の全外部負荷が常時ゼロだったことの保証ではない。

## 検証・保存

変更前に追加5テストが成功した。その後テスト名と解放回数の検証表現を静的診断に合わせて整理し、候補の最終ソースで全316テスト（本体248、JSON68）が成功した。失敗・Skipなし。整理後のテストを変更前の版で再実行したとは扱わない。

非増分Releaseビルド・アナライザーは10.36秒で成功。既存NU1701とSARIFの提案6件が残る。初回formatで新規2指摘を検出・修正し、最終formatは既存66指摘でexit 2。位置番号を除く指摘内容と件数は前回に一致し、成功や指摘ゼロとは扱わない。

[証拠JSON](experiments/2026-10-19-json-array-bulk.json)に候補差分・追加テスト全文・ソースとDLLのハッシュ・全観測・集計・環境・生成コード抜粋・再生スクリプトを保存した。保存したテスト全文と実ファイル、ソースハッシュの一致を確認してから今回の候補だけを撤回し、JSONArray.csのHEAD一致を確認した。既存テストを削除したのではない。

生ログ・固定DLLはローカル `artifacts/string-json-array-bulk-2026-10-19/`、親検証と継続作業のスリープ抑止記録は `artifacts/string-after-normal-character-2026-10-19/` にある。巨大な生ログ・DLLはGitへ追加しない。保存資料だけでのクリーン再構築・再実行は未検証。撤回後は次の別候補へ進んだ。
