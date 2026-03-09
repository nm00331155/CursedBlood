| 項目 | 内容 |
|------|------|
| タスク名 | Phase 5 実装報告 |
| 対象ファイル | Scripts/CursedBlood/Debt/*.cs, Scripts/CursedBlood/UI/DeathScreen.cs, Scripts/CursedBlood/Core/GameManager.cs, Scripts/CursedBlood/Enemy/DebtCollectorEnemy.cs |
| 作業日時 | 2026-03-09 |

**【実施内容】** 借金管理、利息加算、返済選択、借金取り敵、完済ボーナスを導入した。世代交代時に返済額を確定し、その結果を遺産計算と次世代開始条件へ反映するようにした。

**【変更点の詳細】** `DebtManager` が残債・利息・返済候補・解放ボーナスを管理し、`DebtUI` が返済オプションを選択できるようにした。`DebtCollectorEnemy` は未返済状況に応じてグリッドへ出現し、接触時にダメージとゴールド没収を行う。

**【動作テスト結果】**
- `dotnet build CursedBlood.csproj`: 合格
- 借金データ構造と返済選択 UI の型整合: 合格
- ランタイムでの返済分岐確認: 未実施

**【UI確認結果】** DeathScreen 内に返済選択を組み込み済み。借金残高は HUD 表示へ接続済み。スクリーンショットは未取得。

**【ビルド・EXE更新状況】** `dotnet build` 成功。Godot EXE/APK 未更新。

**【既知の課題・注意事項】** 借金取りの追跡挙動と没収量の体感バランスは未検証。返済 UI の最終視認性もランタイム確認が必要。

**【次のステップ（提案）】** Phase 6 で深度ボスを追加し、到達目標と報酬の山場を作る。