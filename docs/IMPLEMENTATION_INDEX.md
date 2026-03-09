# CursedBlood 実装一覧

更新日: 2026-03-09

## 現行ビルド方針

- `docs/修正指示001.md` に従い、現行ビルドは Phase 1 最小プレイアブルを唯一の目標に再構成している
- `CursedBlood.csproj` では Phase 2〜9 のディレクトリ群と旧 `GridManager` 系をビルド対象から除外している
- Phase 2〜9 のソースはリポジトリに残っているが、現行ビルドで有効化はしていない

## 実装状況

| Phase | 状態 | 主な内容 | 主なファイル |
|---|---|---|---|
| 1 | 完了 | 16x16 セル、67x87 表示、16 行チャンク、8方向移動、幅5掘削、60秒寿命、DeathScreen 再開、世代加算 | `Core/CellType.cs`, `Core/ChunkData.cs`, `Core/TerrainGenerator.cs`, `Core/DigHelper.cs`, `Core/ChunkManager.cs`, `Core/GameManager.cs`, `Player/PlayerStats.cs`, `Player/PlayerController.cs`, `Camera/GameCamera.cs`, `UI/HUDManager.cs`, `UI/DeathScreen.cs` |
| 2 | 保留 | リポジトリ内に旧コードは残るが、修正指示001対応のため現行ビルド対象外 | `Enemy/*`, `Core/GridGenerator.cs` |
| 3 | 保留 | リポジトリ内に旧コードは残るが、修正指示001対応のため現行ビルド対象外 | `Equipment/*` |
| 4 | 保留 | リポジトリ内に旧コードは残るが、修正指示001対応のため現行ビルド対象外 | `Generation/*` |
| 5 | 保留 | リポジトリ内に旧コードは残るが、修正指示001対応のため現行ビルド対象外 | `Debt/*` |
| 6 | 保留 | リポジトリ内に旧コードは残るが、修正指示001対応のため現行ビルド対象外 | `Enemy/Boss*.cs` |
| 7 | 保留 | リポジトリ内に旧コードは残るが、修正指示001対応のため現行ビルド対象外 | `Achievement/*` |
| 8 | 保留 | リポジトリ内に旧コードは残るが、修正指示001対応のため現行ビルド対象外 | `Curse/*`, `UI/EndingUI.cs` |
| 9 | 保留 | リポジトリ内に旧コードは残るが、修正指示001対応のため現行ビルド対象外 | `Skill/*`, `Effects/*`, `Audio/*`, `UI/TitleScreen.cs`, `UI/SettingsUI.cs` |

## 現行ビルド対象

- `CursedBlood.csproj`
- `project.godot`
- `Scripts/CursedBlood/Core/CellType.cs`
- `Scripts/CursedBlood/Core/ChunkData.cs`
- `Scripts/CursedBlood/Core/TerrainGenerator.cs`
- `Scripts/CursedBlood/Core/DigHelper.cs`
- `Scripts/CursedBlood/Core/ChunkManager.cs`
- `Scripts/CursedBlood/Core/GameManager.cs`
- `Scripts/CursedBlood/Player/PlayerStats.cs`
- `Scripts/CursedBlood/Player/PlayerController.cs`
- `Scripts/CursedBlood/Camera/GameCamera.cs`
- `Scripts/CursedBlood/UI/HUDManager.cs`
- `Scripts/CursedBlood/UI/DeathScreen.cs`
- `Scenes/CursedBlood/CursedBloodMain.tscn`

## 検証状況

- `get_errors`（現行ビルド対象ファイル）: 成功
- `dotnet build CursedBlood.csproj`: 成功
- Godot 実行テスト: 指定実行ファイルが存在しないため未実施
- UI スクリーンショット: 実行環境不足のため未取得

## 補足

- `project.godot` の `run/main_scene` は `CursedBloodMain.tscn` に戻している
- renderer は Phase 1 指示に合わせて `mobile` を採用した