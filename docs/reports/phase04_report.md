| 項目 | 内容 |
|------|------|
| タスク名 | Phase 4 実装報告 |
| 対象ファイル | Scripts/CursedBlood/Generation/*.cs, Scripts/CursedBlood/UI/DeathScreen.cs, Scripts/CursedBlood/Generation/FamilyTreeUI.cs, Scripts/CursedBlood/Core/GameManager.cs |
| 作業日時 | 2026-03-09 |

**【実施内容】** 死亡時の次世代フローを拡張し、遺品選択、遺産引き継ぎ、名前/性別生成、家系図保存を実装した。家系図 UI から過去世代の概要を閲覧可能にした。

**【変更点の詳細】** `GenerationManager` が遺品・遺産・次世代データを組み立て、`FamilyTree` が JSON 永続化を担当する。`DeathScreen` は単純なリスタート画面から、返済と遺品を選択して次世代へ進む結果画面へ変更した。

**【動作テスト結果】**
- `dotnet build CursedBlood.csproj`: 合格
- 永続化用 JSON クラスの型整合: 合格
- 実際の保存/ロード動作は Godot ランタイム未確認

**【UI確認結果】** 家系図表示パネルと遺品選択ボタン群は配置済み。スクリーンショット取得は未実施。

**【ビルド・EXE更新状況】** `dotnet build` 成功。Godot 実行環境がないためビジュアル確認用 EXE/APK 未更新。

**【既知の課題・注意事項】** 家系図件数が多くなった場合のスクロール最適化は未実装。現状は概要表示中心。

**【次のステップ（提案）】** Phase 5 で借金フローを接続し、周回時のリスクとリターンを加える。