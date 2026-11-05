# 開発の再開地点

作業を区切る際に現在状況を置き換えます。確定仕様・判断は[判断記録](../docs/development-context.md)、調査・未確定事項は[開発候補](development-candidates.md)を正本とし、ここには再開に必要な参照と状態を残します。

## 現在の作業状況

**確認日：2026-11-05。** 再開時にはGitと照合する。

* ブランチ：`codex/optimize-word-parser`、基準HEAD：`a743a93`。この作業からpushはしていない。
* **停止位置：** [R09](development-candidates.md#r09失敗時の消費巻き戻し規則を明文化しoptionalの直接テストを補う)のOptional巻き戻し修正は `a743a93` に保存済み。続くRefactorでは同じ型のコンストラクターだけをprimary constructorへ整理し、人間レビュー待ち。get-onlyプロパティ、公開引数、解析処理・テストは維持した。今回の方針・理由は[判断記録](../docs/development-context.md#optionalの不在成功は開始位置へ戻す)を参照。
* **未コミット差分：** `Kotowari/Core/OptionalParser.cs` と候補一覧・本書の計3ファイル。Refactor差分のコミットは未承認。文書の再配置は `3555769` に保存済み。
* **コード検証済みの版：** `a743a93`＋今回の1コードファイル差分で全469件成功（Core273、JSON原本98・移植98、失敗・Skipなし、11.46秒）。Debug非増分solution解析ビルド成功（10.54秒、既存NU1701のみ）、7 SARIFで対象2ファイルの診断なし。対象formatは終了0（9.10秒）となりIDE0290は解消した。workspace読み込み警告の詳細原因は未確認。Release・性能測定は未実施。詳細はローカル `artifacts/r09-optional-refactor-2026-11-05/refactor/HANDOFF.md`。文書更新による同条件の検証は繰り返していない。
* **残る確認：** ユーザー指定の「既存テストが壊れたら別途検討」に該当する失敗はなかった。ただし、テスト外で旧Optionalの消費動作に依存する利用への互換性までは保証しない。他のコンビネーターの規則の一括変更はしない。
* **次の着手点：** Refactor差分をレビューし、了承後にFコミットする。R09には他のコンビネーターの消費・巻き戻し規則の利用者向け説明が残るが、一律の挙動変更はしない。同期済みJSONの方針・性能比較条件は[判断記録](../docs/development-context.md#最新版同期2026-11-05)から再確認する。巨大JSONの生データ・一時ホストはローカル `artifacts/json-speed-snapshots-2026-11-05/` にあり、Git対象外。
