# CursedBlood 実装一覧

更新日: 2026-03-09

## 実装状況

| Phase | 状態 | 主な実装内容 | 主なファイル |
|---|---|---|---|
| 1 | 実装済み | グリッド、移動、寿命、HUD、死亡/再開 | `Core/*`, `Player/*`, `UI/HUDManager.cs`, `UI/DeathScreen.cs` |
| 2 | 実装済み | 敵4種、弾、鉱石、コンボ、HP死亡 | `Enemy/EnemyManager.cs`, `Enemy/BulletManager.cs`, `Core/GridGenerator.cs` |
| 3 | 実装済み | 装備生成、インベントリ、装備UI、ドロップ | `Equipment/*`, `UI/HUDManager.cs`, `Core/GameManager.cs` |
| 4 | 実装済み | 遺品選択、世代継承、家系図、名前生成 | `Generation/*`, `UI/DeathScreen.cs`, `UI/FamilyTreeUI.cs` |
| 5 | 実装済み | 借金、利息、返済選択、借金取り敵 | `Debt/*`, `Enemy/DebtCollectorEnemy.cs`, `Core/GameManager.cs` |
| 6 | 実装済み | 深度ボス、ボスUI、ボス弾幕、撃破報酬 | `Enemy/Boss*.cs`, `Core/GridGenerator.cs`, `Core/GameManager.cs` |
| 7 | 実装済み | 20実績、進捗表示、解除ポップアップ、ランキングTOP10、永続パッシブ、HUD導線 | `Achievement/*`, `Player/PlayerStats.cs`, `Player/PlayerController.cs`, `Enemy/BulletManager.cs`, `Core/GameManager.cs`, `UI/HUDManager.cs`, `UI/TitleScreen.cs` |
| 8 | 実装済み | 呪い研究、寿命延長、魔王処理、エンディングUI | `Curse/*`, `Enemy/DemonLord*.cs`, `UI/EndingUI.cs` |
| 9 | 実装済み | スキル、演出、タイトル、設定、ポーズ、チュートリアル | `Skill/*`, `Effects/*`, `Audio/*`, `UI/TitleScreen.cs`, `UI/SettingsUI.cs` |

## 作成・更新した主要ファイル群

- `Scripts/CursedBlood/Core/`
- `Scripts/CursedBlood/Player/`
- `Scripts/CursedBlood/Enemy/`
- `Scripts/CursedBlood/Equipment/`
- `Scripts/CursedBlood/Generation/`
- `Scripts/CursedBlood/Debt/`
- `Scripts/CursedBlood/Achievement/`
- `Scripts/CursedBlood/Achievement/AchievementData.cs`
- `Scripts/CursedBlood/Achievement/AchievementPopup.cs`
- `Scripts/CursedBlood/Achievement/RankingUI.cs`
- `Scripts/CursedBlood/Curse/`
- `Scripts/CursedBlood/Skill/`
- `Scripts/CursedBlood/Effects/`
- `Scripts/CursedBlood/Audio/`
- `Scripts/CursedBlood/UI/`
- `Scenes/CursedBlood/TitleScene.tscn`
- `Scenes/CursedBlood/EndingScene.tscn`

## 検証状況

- `dotnet build CursedBlood.csproj`: 成功
- Godot 実行テスト: 実行ファイル未配置のため未実施
- UI スクリーンショット: 実行環境不足のため未取得

## 補足

- `docs/07_PHASE7_ACHIEVEMENT.md` の改訂内容に合わせて、Phase 7 実装を再点検し直し、実コードを追従させた
- `project.godot` の `run/main_scene` は最終仕上げに合わせて `TitleScene.tscn` へ変更済み