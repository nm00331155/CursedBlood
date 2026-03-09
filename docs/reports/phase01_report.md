| 項目 | 内容 |
|------|------|
| タスク名 | Phase 1 最小プレイアブル再構成 |
| 対象ファイル | CursedBlood.csproj, project.godot, Scripts/CursedBlood/Core/CellType.cs, Scripts/CursedBlood/Core/ChunkData.cs, Scripts/CursedBlood/Core/TerrainGenerator.cs, Scripts/CursedBlood/Core/DigHelper.cs, Scripts/CursedBlood/Core/ChunkManager.cs, Scripts/CursedBlood/Core/GameManager.cs, Scripts/CursedBlood/Player/PlayerStats.cs, Scripts/CursedBlood/Player/PlayerController.cs, Scripts/CursedBlood/Camera/GameCamera.cs, Scripts/CursedBlood/UI/HUDManager.cs, Scripts/CursedBlood/UI/DeathScreen.cs |
| 作業日時 | 2026-03-09 |

**【実施内容】** `docs/01_PHASE1_GRID_MOVEMENT.md` と `docs/00_PROJECT_SPEC.md` を基準に、Phase 1 を最小プレイアブルとして全面再構成した。旧 `GridManager` 系の 7x9 / 4方向 / 多 phase 混在構成を採用せず、`ChunkManager` 主導の 67x87 表示、16x16 セル、8方向継続移動、幅 5 掘削、60 秒寿命、死亡後リスタートを中心に組み直した。あわせて `CursedBlood.csproj` で Phase 2 以降のクラスタを現行ビルド対象から外し、修正指示の「Phase 1 完了を優先」に合わせた。

**【変更点の詳細】** 新規に `CellType.cs`、`ChunkData.cs`、`TerrainGenerator.cs`、`DigHelper.cs`、`ChunkManager.cs` を追加し、byte 配列・16 行チャンク・Empty 非描画・水平連結描画・簡易掘削フラッシュを実装した。`PlayerStats.cs` は 60 秒寿命、少年期 3x3、青年期/晩年期 5x5、0.02 秒/セル基準の速度計算に整理し、`PlayerController.cs` は 8 方向キー入力、スワイプ 8 方向スナップ、先行入力、Guard、掘削幅 5 固定の前面掘削へ刷新した。`GameManager.cs` は Phase 1 のみを束ねる最小構成に置き換え、`project.godot` の `main_scene` は `CursedBloodMain.tscn`、renderer は `mobile` に変更した。

**【動作テスト結果】**
- `get_errors`（変更ファイル一式）: 合格
- `dotnet build CursedBlood.csproj`: 合格
- Godot headless 起動確認: 合格（`D:\code_workspace\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64_console.exe --headless --editor --quit --path D:\code_workspace\CursedBlood`）
- APK 再ビルド: 合格（`--headless --export-debug Android D:\code_workspace\CursedBlood\CursedBlood.apk`）
- APK 署名確認: 合格（`apksigner verify -v D:\code_workspace\CursedBlood\CursedBlood.apk`）
- 実機インストール: 合格（`adb install -r D:\code_workspace\CursedBlood\CursedBlood.apk`）
- 実機起動: 合格（`adb shell am start -n com.example.cursedblood/com.godot.game.GodotAppLauncher`）
- 実機向き確認: 合格（clean reinstall 後の `dumpsys activity activities` で `requestedOrientation=SCREEN_ORIENTATION_PORTRAIT` を確認）

**【UI確認結果】** 実機起動までは確認したが、スクリーンショット取得と目視確認は未実施。コード上では HUD を y:0〜200、下部 UI を y:1600〜1920 に固定し、フィールドは 16x16 セルの 67x87 表示に合わせている。実機での視認性、カメラ追従、DeathScreen 表示は別途画面確認が必要。

**【ビルド・EXE更新状況】** `dotnet build` は成功し、生成物 `D:\code_workspace\CursedBlood\.godot\mono\temp\bin\Debug\CursedBlood.dll` の更新を確認した。Android 向けには `D:\code_workspace\CursedBlood\CursedBlood.apk` を再生成し、更新時刻 `2026/03/09 14:38:02`、サイズ `95664925 bytes`、`apksigner verify -v` 合格を確認した。export 時には `No project icon specified` が表示されるため、今後は project icon 設定を入れる余地がある。

**【既知の課題・注意事項】** Phase 2〜9 のコードはリポジトリ内に残しているが、修正指示に従って `CursedBlood.csproj` から除外しているため現行ビルドでは動作しない。斜め移動時の掘削感、石/硬岩での体感速度、HUD と DeathScreen の最終見た目は実ランタイムでの確認が必要。UI スクリーンショットは未取得。Android export を通すには `project.godot` の `textures/vram_compression/import_etc2_astc=true`、`CursedBlood.csproj` の Android 条件 `TargetFramework=net9.0`、`export_presets.cfg` の `package/signed=true` が必要だった。加えて、`window/handheld/orientation` は文字列 `"portrait"` ではなく整数 enum `1` で保存しないと、Android exporter が `landscape` と解釈する。

**【次のステップ（提案）】** 実機で Phase 1 の操作感と表示を目視確認し、project icon を設定して export warning を解消する。その後は docs の順に Phase 2 以降を再統合していく。