| 項目 | 内容 |
|------|------|
| タスク名 | Phase 9 実装報告 |
| 対象ファイル | Scripts/CursedBlood/Skill/*.cs, Scripts/CursedBlood/Effects/*.cs, Scripts/CursedBlood/Audio/*.cs, Scripts/CursedBlood/UI/TitleScreen.cs, Scripts/CursedBlood/UI/PauseMenu.cs, Scripts/CursedBlood/UI/SettingsUI.cs, Scripts/CursedBlood/UI/TutorialOverlay.cs, Scenes/CursedBlood/TitleScene.tscn, project.godot |
| 作業日時 | 2026-03-09 |

**【実施内容】** スキル、フラッシュ/パーティクル演出、オーディオ設定、ポーズ、設定、チュートリアル、タイトル画面、バランス設定ファイルを追加した。最終導線として `project.godot` の main scene をタイトル画面へ切り替えた。

**【変更点の詳細】** `SkillManager` と `SkillEffects` で 4 種のスキルを処理し、`ScreenEffects` と `ParticleManager` で簡易演出を追加した。`AudioManager` は BGM/SE パス定義と音量/振動設定の保存を担当し、`TitleScreen`、`PauseMenu`、`SettingsUI`、`TutorialOverlay` が最終 UX を補完する。

**【動作テスト結果】**
- `dotnet build CursedBlood.csproj`: 合格
- 設定保存/ロード型整合: 合格
- タイトル遷移・スキル発動の実機確認: 未実施

**【UI確認結果】** タイトル画面シーン、設定 UI、ポーズ UI、チュートリアル UI を追加済み。スクリーンショットは未取得。

**【ビルド・EXE更新状況】** `dotnet build` 成功。Godot EXE/APK 未更新。Godot コンソール実行ファイルは `Test-Path` 結果 `False` で、起動テスト不可。

**【既知の課題・注意事項】** 実音源ファイルは未同梱のため、`AudioManager` はパス管理と設定保存のみ。Android 実機 60fps 確認、メモリリーク長時間試験は未実施。

**【次のステップ（提案）】** Godot 実行環境を用意し、プレイフィール、UI 視認性、演出強度、バランスを実機ベースで詰める。