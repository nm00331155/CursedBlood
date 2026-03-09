# Phase 5: 借金＋利息＋返済UI＋借金取り敵

## 前提

- 依存: Phase 4 完了済み
- 参照: `docs/00_PROJECT_SPEC.md`, `docs/11_SAVE_SYSTEM.md`, `docs/12_SCREEN_FLOW.md`

## このPhaseのゴール

初代から100,000Gの借金。世代ごとに利息10%加算。リザルトで返済選択。未返済で借金取り敵が出現。完済で永続ボーナス。

## 完了条件チェックリスト

- [ ] 初代借金100,000G
- [ ] 世代ごとに利息10%加算
- [ ] HUDに借金残高表示
- [ ] リザルト画面（遺品選択前）に返済選択UI
- [ ] 4返済オプション（全額/半額/最低/しない）
- [ ] 所持金不足のオプションはグレーアウト
- [ ] 返済後の残りゴールドから遺産計算
- [ ] 借金取り敵: 赤黒色、プレイヤー追跡AI、5×5セル(80px)
- [ ] 未返済額に比例して出現頻度UP
- [ ] 完済で永続「解放ボーナス: 全ステ×1.2」
- [ ] 借金データはSaveManager経由で永続保存
- [ ] 死亡フロー: Dead → DebtRepay → HeirloomSelect → Playing

## 作成・変更するファイル

### 新規作成

```
Scripts/CursedBlood/Debt/
  DebtManager.cs
  DebtUI.cs
  DebtCollectorEnemy.cs
```

### 変更

```
Scripts/CursedBlood/Core/GameManager.cs
Scripts/CursedBlood/Core/TerrainGenerator.cs    ← 借金取り敵配置
Scripts/CursedBlood/Enemy/EnemyManager.cs       ← 追跡AI
Scripts/CursedBlood/Generation/GenerationManager.cs
Scripts/CursedBlood/UI/HUDManager.cs
Scripts/CursedBlood/UI/DeathScreen.cs
Scripts/CursedBlood/Player/PlayerStats.cs
Scripts/CursedBlood/Save/SaveData.cs            ← DebtData追加
```

## 詳細設計

（DebtManager, DebtUI, DebtCollectorEnemy, フローの設計は旧版と同等。借金取り敵のサイズを5×5セル(80px)に変更。）

### 借金取り敵の追跡AI

毎0.1秒（5セル/秒）でプレイヤー方向に1セル移動。壁があれば迂回。8方向移動対応。

## 実装順序

1. DebtManager.cs + SaveData.cs変更
2. DebtUI.cs
3. DebtCollectorEnemy.cs
4. TerrainGenerator.cs変更
5. EnemyManager.cs変更
6. GenerationManager.cs変更（返済→遺産フロー）
7. DeathScreen.cs変更
8. HUDManager.cs変更
9. PlayerStats.cs変更
10. GameManager.cs変更（Dead→DebtRepay→HeirloomSelect→Playing遷移）
11. 動作確認 → APKビルド
