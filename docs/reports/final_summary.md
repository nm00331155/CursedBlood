# CursedBlood 現行作業サマリ

## 実施結果

- `docs/修正指示001.md` に従い、Phase 1 を唯一の正として現行ビルドを再構成した
- `GridManager` ベースの旧構成を採用せず、`ChunkManager` 主導の 16x16 / 67x87 / 16 行チャンク構成へ切り替えた
- `GameManager`、`PlayerController`、`PlayerStats`、`GameCamera`、`HUDManager`、`DeathScreen` を Phase 1 最小プレイアブル向けに全面更新した
- `project.godot` の `run/main_scene` を `CursedBloodMain.tscn` へ戻し、renderer を `mobile` に変更した
- `CursedBlood.csproj` で Phase 2〜9 のクラスタを現行ビルド対象から除外した
- `docs/reports/phase01_report.md` と `docs/IMPLEMENTATION_INDEX.md` を現行状態に合わせて更新した

## 最終検証

- `get_errors`（現行ビルド対象ファイル）: エラーなし
- `dotnet build CursedBlood.csproj`: 成功
- Godot headless 起動確認: 成功
- APK ビルド: `Godot_v4.6.1-stable_mono_win64_console.exe --headless --export-debug Android` で成功
- APK 署名確認: `apksigner verify -v CursedBlood.apk` で成功（v2 署名あり）
- 実機インストール: `adb install -r CursedBlood.apk` で成功
- 実機起動: `adb shell am start -n com.example.cursedblood/com.godot.game.GodotAppLauncher` で成功
- UI スクリーンショット: 未取得

## 現行状態の注意点

- 現在の実行可能状態は Phase 1 最小プレイアブルのみ
- Phase 2〜9 のコードは履歴として残るが、修正指示001対応のため `CursedBlood.csproj` から除外している
- そのため、旧報告書にある「全 phase 実装済み」は現行ビルド状態としては成立しない
- Android export では `project.godot` に `textures/vram_compression/import_etc2_astc=true`、`export_presets.cfg` に `package/signed=true` が必要だった

## 主要な残課題

- 実機起動までは確認済みだが、操作感、カメラ追従、HUD、DeathScreen の目視確認とスクリーンショット取得は未完了
- スワイプ感度、斜め掘削時の体感、石/硬岩通過の速度差は実プレイ確認が必要
- Phase 2 以降を進める場合は docs の順に再統合し、各 phase ごとにビルドとランタイム検証をやり直す必要がある

## 参照先

- `docs/修正指示001.md`
- `docs/IMPLEMENTATION_INDEX.md`
- `docs/reports/phase01_report.md`