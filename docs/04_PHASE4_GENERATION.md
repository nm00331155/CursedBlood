# Phase 4: 世代継承＋リザルト＋遺品選択＋家系図

## 前提

- 依存: Phase 3 完了済み
- 参照: `docs/00_PROJECT_SPEC.md`, `docs/11_SAVE_SYSTEM.md`, `docs/12_SCREEN_FLOW.md`

## このPhaseのゴール

死亡時に遺品（装備1つ）を選択し、ゴールドの30%を遺産として次世代に託す。家系図に各世代の記録が蓄積される。セーブシステムを統合し、永続データを管理する。

## 完了条件チェックリスト

- [ ] リザルト画面で「遺品選択」UIが表示される
- [ ] 装備中4枠 + バッグ内から1つを遺品選択
- [ ] 遺産としてゴールドの30%が次世代に引き継がれる
- [ ] 次世代が遺品と遺産を持ってスタート
- [ ] 世代番号インクリメント
- [ ] 男女ランダム（色変化で表現）
- [ ] 家系図に全世代の記録が蓄積
- [ ] 名前はランダム生成（日本語風、男女別）
- [ ] SaveManagerによる統合セーブが動作する
- [ ] セーブデータの読み書き・バックアップ・リカバリーが機能する
- [ ] アプリ終了時・世代交代時に自動保存
- [ ] 家系図画面で過去の世代を閲覧可能

## 作成・変更するファイル

### 新規作成

```
Scripts/CursedBlood/Generation/
  GenerationManager.cs
  FamilyTree.cs
  FamilyTreeUI.cs
  NameGenerator.cs
  InheritanceData.cs
Scripts/CursedBlood/Save/
  SaveManager.cs
  SaveData.cs
  SaveMigrator.cs
```

### 変更

```
Scripts/CursedBlood/UI/DeathScreen.cs
Scripts/CursedBlood/Core/GameManager.cs
Scripts/CursedBlood/Player/PlayerStats.cs
Scripts/CursedBlood/UI/HUDManager.cs
```

## 詳細設計

（NameGenerator, FamilyTree, GenerationManager, InheritanceData, DeathScreen変更の設計は旧版と同等。）

### SaveManager統合

このPhaseでSaveManager（`docs/11_SAVE_SYSTEM.md`参照）を実装する。

各マネージャーは `LoadFrom()` / `ToSaveData()` パターンで SaveManager と連携する。

**家系図の深度表示**: 全て メートル(m)単位。

## 実装順序

1. SaveData.cs + SaveMigrator.cs
2. SaveManager.cs
3. NameGenerator.cs
4. InheritanceData.cs
5. FamilyTree.cs（SaveManager連携）
6. GenerationManager.cs
7. PlayerStats.cs変更
8. DeathScreen.cs変更（遺品選択UI + 「次へ」ボタン）
9. FamilyTreeUI.cs
10. GameManager.cs変更（SaveManager初期化 + 世代継承フロー + 画面遷移）
11. HUDManager.cs変更
12. 動作確認 → APKビルド
