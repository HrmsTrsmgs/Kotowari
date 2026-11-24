# UTF-8対応の実験履歴と再開地点

2026-10-12にUTF-8対応をいったん終了し、文字列専用実装へ戻すことを決定した。
試作、テスト、測定結果はGit履歴と名前付きタグに保存する。

## まず見る場所

| 用途 | 注釈タグ | 指すコミット |
|---|---|---|
| 調査終了時点の本体・テスト・全測定記録 | `archive/utf8-span-2026-10-12` | `8072982` |
| Word直接照合とASCII消費変更を併用した試作 | `experiment/utf8-char-advance-2026-10-12` | `53a4a7c` |
| 実際にUTF-8を追加する直前の文字列専用実装 | `baseline/string-before-utf8` | `b6c68c3` |

これらのタグはローカルで作成した。remoteへのpushはしていない。

## 比較基準の訂正

後期の測定記録でO／original-stringと呼んだ`defe7830`は、UTF-8対応済みの旧カーソル実装の文字列経路だった。
`Cursol.FromUtf8(ReadOnlyMemory<byte>)`とUTF-8文書クラスを含み、UTF-8導入前の実装ではない。
保存済みの凍結ソースでも、この構成を確認した。

2026-10-12のA2/O時間比1.216181、95%区間[1.129920, 1.309028]は、この旧カーソルの文字列経路との比較である。
実際のUTF-8導入前`b6c68c3`に対する比として使ってはいけない。
入力・DLLハッシュと実測値は有効な記録として残すが、比較基準の説明を訂正する。
従来の記録にある「UTF-8対応前」という説明を使う場合は、実装の版と凍結ソースを確認する。

## 主な実装と測定

| テーマ | 実装・履歴 | 測定・判断の記録 |
|---|---|---|
| 初期UTF-8対応 | `5ed8185`、`81bcec1`、`f4044ec` | [入力経路の比較](../Kotowari.Benchmarks/Results/2026-10-07-utf8-stream-json.md) |
| 入力型別カーソルとSpan借用 | `1422292` | [Span借用](../Kotowari.Benchmarks/Results/2026-10-07-utf8-span-borrowing.md)、[後退の切り分け](../Kotowari.Benchmarks/Results/2026-10-07-utf8-span-regression.md) |
| 実行器の共通化 | `bc0ff11` | [共通化の評価](../Kotowari.Benchmarks/Results/2026-10-09-execution-commonization.md) |
| UTF-8入力保持の最適化 | `d9c35ce` | [入力保持方法の比較](../Kotowari.Benchmarks/Results/2026-10-10-utf8-document-layout.md) |
| 6候補の検証 | `71237af`〜`76a6fef`の記録、保存patch | [測定一覧](../Kotowari.Benchmarks/Results/README.md)、[200起動の集約JSON](../Kotowari.Benchmarks/Results/experiments/2026-10-11-utf8-tuning-evidence.json) |
| Char／Wordの直接バイト照合 | `2bdf408`、`67a741b` | [40起動の評価・追加診断](../Kotowari.Benchmarks/Results/2026-10-11-utf8-byte-match.md) |
| ASCII文字消費 | `5112abe`、`53a4a7c` | [56起動の評価](../Kotowari.Benchmarks/Results/2026-10-12-utf8-character-advance.md) |
| 個別候補の撤回 | `44a60b0`、`1624a15`、`39291d7` | [最終記録](../Kotowari.Benchmarks/Results/2026-10-12-utf8-character-advance.md) |

2026-10-12の測定は既存4,172,062-byteの全ASCII JSON、MemoryStreamから全文解析・結果木構築まで、Dynamic PGO有効、7条件×8組の56新規起動。
ASCII消費変更の増分とWord単独案は個別の事前採用条件を満たさなかった。
複合案では時間比0.960182、95%区間[0.922779, 0.999101]となり、約4.0%短縮を支持する結果は残った。
個別案に効果がゼロと確定したわけではない。

## 文字列専用実装への復帰

本体は `b6c68c3` の文字列専用実装へ戻す。`Cursol`、`ParseResult<T>`、`Parser<T>.Parse(Cursol)` の構成を復元し、UTF-8入力API、入力型別ジェネリック、実行器とキャッシュを除去する。
PGO対策の子パーサー直接呼び出しとremarks、WordParserの遅延配列確保は復元対象に含まれる。
クラスごとのファイル分割、AI作業指示、過去の測定記録、現行の文字蓄積比較7方式は残す。

UTF-8専用テストとJSONテストのUTF-8側パラメーターだけを外し、文字列側の境界値、再利用、例外、派生型、再帰getterのテストを維持した。
Releaseの全テスト279件（Kotowari 222件、JSON 57件）が成功した。
Releaseの非増分ビルド・アナライザーも成功した（13.73秒）。既存のSpracheJSONのNU1701警告は残る。
JSON比較6方式の全文消費・結果生成の検証と、文字蓄積7方式×12ケースの動作確認が成功した。これらは計時ではない。
SARIFの提案6件は既存箇所に残る。format検査は既存の空白・スタイル指摘と復元した旧実装のprimary constructor等の提案により非成功。復帰と別の設計変更を混ぜないため、旧実装の構文は維持した。
この復帰版の性能は再測定していないため、過去のOとの比を復帰版の性能として扱わない。

## 履歴を探すコマンド

作業ツリーを変更せず、保存地点と差分を確認できる。

```powershell
git show archive/utf8-span-2026-10-12
git log --oneline baseline/string-before-utf8..archive/utf8-span-2026-10-12
git show experiment/utf8-char-advance-2026-10-12:Kotowari/Core/CursolOfTDocument.cs
git show baseline/string-before-utf8:Kotowari/Core/Cursol.cs
git diff baseline/string-before-utf8 archive/utf8-span-2026-10-12 -- Kotowari JsonParser
```

Gitには測定記録と集約証跡を保存している。`artifacts/`内の巨大なtrace、完全ログ、凍結ビルドはGit対象外である。
再開時は索引中の版と入力・条件を使い、採用済み実装、撤回した試作、通常計時、診断付き計時を区別する。
