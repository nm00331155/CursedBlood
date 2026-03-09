| 項目 | 内容 |
|------|------|
| タスク名 | Phase 3 実装報告 |
| 対象ファイル | Scripts/CursedBlood/Equipment/*.cs, Scripts/CursedBlood/Player/PlayerStats.cs, Scripts/CursedBlood/Core/GameManager.cs |
| 作業日時 | 2026-03-09 |

**【実施内容】** レアリティ付き装備、付与効果、呪いデメリット、インベントリ、装備 UI を追加した。敵撃破・鉱石通過・ボス撃破時のドロップが装備データへ接続されるようにした。

**【変更点の詳細】** `EquipmentGenerator` で深度・カテゴリ・レアリティ・効果数・デメリットを自動生成するようにし、`Inventory` で 4 スロット装備と 20 個バッグを管理するようにした。`EquipmentUI` では装備/解除/バッグ操作を行い、`PlayerStats` が装備補正を移動速度・掘削力・HP・取得率へ反映する。

**【動作テスト結果】**
- `dotnet build CursedBlood.csproj`: 合格
- 装備データ生成・インベントリ型整合: 合格
- UI 操作の実機確認: 未実施

**【UI確認結果】** 装備画面は CanvasLayer とボタン群で実装済み。比較演出は最小構成で、バッグ満杯時は低価値アイテム置換とメッセージ表示で代替している。

**【ビルド・EXE更新状況】** `dotnet build` 成功。Godot EXE/APK 未更新。

**【既知の課題・注意事項】** 詳細比較ポップアップやスクロールの操作感は未検証。ドロップ演出の派手さは Phase 9 側の演出で補完している。

**【次のステップ（提案）】** Phase 4 で遺品選択と家系図保存を実装し、周回動機を作る。