# Sequence3の中間結果削減の評価 2026-10-20

基準は `be9d14b8be22a658df891a212bf978c4be7e65ed`。Sequence2の解析と結果変換を内部メソッドへ集約し、Sequence3で外側の入れ子のParseResultを作らず、最終結果を直接返す案を試した。対象経路の中間結果と24 byteのコピーは削減できたが、通常計時の時間比は1.0072、95%区間は[0.9759, 1.0395]で速度改善の採用条件を満たさなかった。本体2ファイルは撤回し、テストの修正・補強と再現用差分を残す。

## 変更単位と契約

[事前の設計提案](2026-10-19-next-candidates-investigation.md#2026-10-19の続行時に具体化したsequence3案)を、2026-10-20のユーザーの「お願いします」により実装・検証した。

```text
変更前：Converter(Sequence2(p1, Sequence2(p2, p3)), nested => flat)
候補版：Sequence2(p1, Sequence2(p2, p3)).ParseAndConvert(cursol, (first, rest) => flat)
```

本体の変更は `Core/SequenceParser2.cs` と `Core/SequenceParser3.cs` だけ。Sequence2へinternalの `ParseAndConvert<TResult>` を追加し、二つの子を順に解析する制御フローを一か所に保った。通常のSequence2は同じ入口へtupleを作るdelegateを渡す。Sequence3は自分で生成した正確なSequence2型だけにこの非virtual入口を使い、利用者が渡した子パーサーのoverrideを迂回しない。

suffixのSequence2、子の評価順序・回数、失敗時の短絡と開始カーソルへの復帰、成功値、例外伝播を維持する。公開API、一般のParserConverter、arity 4～10のソースは変えない。他arityへの間接影響は全テストで確認した。解析処理の手書き複製、新しいクラス・interfaceの導入、既存の内部ParseCore呼び出しの方針変更は行っていない。

通常のSequence2にもdelegateが増えることは実装前からのリスクであり、費用がJITで消えるとは仮定しなかった。今回の変更は性能候補の検証であって、振る舞いが同じという理由でRefactor工程へ振り分けたものではない。

## 生成コードで確認できたこと

同じ固定host・入力から、PGO有効の診断専用プロセスを両版で採取した。以下の行番号は証拠JSONに記録した各原本 `jit.asm` のもの。通常計時とは別であり、診断付きの時間は速度比較に混ぜない。

| 経路 | 基準版 | 候補版 |
|---|---|---|
| Sequence3の `char, 参照型, char` | 外側Sequence2を呼び、nested結果を書き出す（42549～42557行）。Converterでtupleを24 byteコピー（42240～42243行）して平坦化 | ParseAndConvertがSequence3へ展開され、nested結果の書き出しとコピーが消えた。suffix呼び出し（40381行）と最終flat結果は残る |
| 通常のSequence2の `char, char` | 直接tupleを構築 | delegateキャッシュ確認（46043～46049行）と `Func.Invoke`（46225行）が残った |
| JPairの `参照型, char, 参照型` | 既に直接最終結果を書く枝がある（50355～50415行） | 同じ中間コピー削減は得られず、projectionのguardなどが加わった（50521～50590行） |

対象のnested結果は値型であり、コピー削減をヒープ割り当て削減とは扱わない。基準の平坦化delegateの一致枝も既にinline化されていた。候補の `char, 参照型, char` ではgeneric dictionary／static delegateキャッシュ参照が加わり、入口の `CORINFO_HELP_GET_GCSTATIC_BASE`（40168行）も残る。通常のSequence2のdelegate呼び出しと合わせ、削減だけでなく追加処理も確認した。ただし、それぞれの全体時間への寄与は分離していない。

比較したTier 1ヘッダーには `Synthesized PGO` と表示されるものがあり、一部inlineeにはPGOデータが使われている。PGO有効という実行条件を、対象の全命令が動的profileから生成された証明とはしない。コードサイズや命令の見た目だけから速度を推定せず、次の通常計時で採否を判断した。

正式な比較は基準 `diagnostics/jit/baseline/capture-02`、候補 `diagnostics/jit/candidate/capture-01`、filterは `Marimo.*!*`。基準の最初のfilterでは出力が得られず、より狭いfilterのprobe後に、既存の有効なfilterで取り直した。最初の失敗とprobeもローカルに保存したが、正式な比較・計時の標本には加えていない。

## 通常計時の条件と結果

既存 `Data/test.json`、4,172,062 bytes、SHA-256 `DC87DD0FB85CB6F875AC336B70FEFC1E32BB1F284C8F6E011E0CB0256F9A354D`。MemoryStreamの作成・コピー、UTF-8からstringへの復号、全文解析・結果木生成を含み、物理ディスク読み取りを含めない。両版で全文消費・100,001ノード・System.Text.Jsonとの結果木一致を確認した。System.Text.Jsonは検証用で、速度比較対象ではない。

SDK 10.0.401、.NET 10.0.12、BenchmarkDotNet 0.15.8、Windows 25H2 build 26200.9711、i7-13700H。PGO有効、affinity mask 4、warmup 16、Actual 8、invocation 4、unroll 1、DontRemove。8組16新規起動をAB/BA交互に直列実行した。

各起動の8 Resultの平均を比較単位とし、8個の対応log時間比から95% t区間（自由度7、t=2.364624251）を求めた。ActualとResultは別の独立標本ではない。探索で区間上限が1未満の場合だけ、別の8組で独立確認する事前手順である。

| 系列 | 組数 | 基準 ms/解析 | 候補 ms/解析 | 対応時間比 | 95%対応log-t区間 |
|---|---:|---:|---:|---:|---:|
| 探索 | 8 | 119.304 | 120.187 | 1.007194 | [0.975890, 1.039503] |

この条件では約2.41%の短縮から約3.95%の増加までを含む区間であり、改善・悪化・同等のいずれも確定しない。事前の速度改善条件は満たさず、独立確認へは進まなかった。都合のよい組の除外、組数の延長、過去の別候補・診断値との合算は行っていない。中間処理が減っても、全体の速度改善は保証されないため、候補の本体変更は不採用とした。

基準と候補で本体DLLだけを交換し、他27 DLLは同一。基準本体SHA-256は `B71C3665C7B6333A538AEB1D614E7F6ADB803DFD5055981F9D35FA235A1F44EF`、候補本体は `145AFADA57850ADF0F6F637FCA9E6CB68DA22535672E32787C891F09B0371C2A`、共通JSONサンプルは `B5E2131BC46287DA2C8173620F11D206085E5354FBE6B5F929587F14F03EA712`。直前の古い本体DLLを流用せず、同じHEADから作った基準・候補を使った。

全16起動の終了コードとActual/Result数、設定を検証した。各起動前のスリープ抑止helperの鮮度、AC接続、省電力無効と、各組前の3秒窓に他dotnetのCPU活動がないことを確認した。測定中ずっと外部負荷がゼロだった保証ではない。親は計時中にビルド・ソース編集を重ねていない。PGO OFFの測定は行っていない。

## テスト・保存する変更

既存の2・3要素Sequenceテストには、テスト名の位置で失敗しない入力があった。`publica ...` は最初の `public` が接頭辞として成功して次の子で失敗するため、`publiac ...` へ直した。「三つ目の失敗」は二つ目も失敗する `public statiac innt` から `public static innt` へ直した。WordParserの受理条件を変える修正ではない。

同じ2クラスに、子の消費後の失敗でも開始位置に戻ること、失敗・例外後に後続を呼ばないこと、同じ例外を伝えること、派生overrideの結果と回数を尊重することを12ケース追加した。既存の補助パーサーを使い、内部メソッドの直接テストやテスト専用公開APIは追加していない。このテスト修正・補強は本体候補の採否と独立して有用なため、撤回後も残す。

実装前の対象24ケース、候補の対象24ケースと全323ケース（本体260、JSON63）は成功し、失敗・Skipなし。候補の非増分Releaseビルド・アナライザーは19.22秒で成功。既存NU1701とSARIFの提案6件が残った。formatは既存66指摘でexit 2、位置番号を除いた内容・件数が直前の記録と一致し、新規指摘なし。既存のprimary constructorなどをこの性能試行へ混ぜていない。

撤回後も、本体はHEAD一致のまま上記テストを残して、非増分Releaseビルド（12.37秒）と全323テストを確認した。再生成した本体DLLも基準版のSHA-256と一致した。formatは既存66指摘でexit 2、SARIFも既存6件で新規なし。最終検証の詳細は証拠JSONに記録した。

[証拠JSON](experiments/2026-10-20-sequence3-projection.json)に、基準コミット・候補実装とテストの再現差分、ハッシュ、通常計時の全観測・集計、JIT比較箇所、検証・環境記録、実行スクリプトを保存した。本体を戻す前に、4ファイルのソースハッシュと保存差分が作業ツリーに一致することを親でも確認した。原本・固定DLLはローカル `artifacts/string-sequence3-projection-2026-10-20/` にのみ存在する。今回の保存資料には生成済みMeasurement/ChildHarnessのソースは含めておらず、このhostによる再計時は固定ローカル成果物に依存する。候補の本体差分はGitの基準コミットから再適用できるが、保存資料だけを使ったhostを含むクリーン再構築・再実行は未検証。

Luna medium担当が準備・診断採取・通常計時・機械的集計を行い、親は実装・仮説・条件・生成コードの解釈・採否を担当した。作業全体の一時的なスリープ抑止は同じartifacts内のstatusとheartbeatへ記録する。

## 次に検討できる範囲

今回新しく確認したのは、候補の解析経路にgenericなstatic delegateキャッシュ参照とdelegate呼び出しが残ること。delegateを文法生成時に保持し、解析ごとのキャッシュ参照を外す形は、追加処理に対応する未実装の検討候補となる。ただし通常のSequence2のdelegate呼び出し自体を消す案ではなく、全体時間への寄与も未確認である。速度改善が確定した案とは扱わず、構造と変更範囲をレビューしてから扱う。同じFunc方式の無制限な記法・配置の総当たり、arityの一括展開は行わない。
