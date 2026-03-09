# Phase 6: ボス＋3フェーズ弾幕＋アリーナ

## 前提

- 依存: Phase 2 完了済み
- 参照: `docs/00_PROJECT_SPEC.md`

## このPhaseのゴール

深度1000mごとにボス（20×20セル=320×320px）が出現。周囲が空洞化してアリーナを形成。3フェーズ弾幕。Rare以上確定ドロップ。迂回可能。

## 完了条件チェックリスト

- [ ] 深度1000m, 2000m, 3000m...にボス(20×20セル)が生成される
- [ ] ボス周囲40×40セル(640×640px)が空洞化しアリーナ形成
- [ ] 左右に幅10セルの迂回通路あり
- [ ] ボスにHP表示
- [ ] ボスHP: 深度1000m=500, 以降×1.5
- [ ] プレイヤーがボス隣接セルからスワイプでボス方向なら攻撃
- [ ] 3フェーズ弾幕
- [ ] Phase1(100〜60%): 十字4方向に2発ずつ、3秒間隔
- [ ] Phase2(60〜30%): 8方向に1発ずつ、2秒間隔
- [ ] Phase3(30〜0%): 全方向弾 + ランダム列崩落
- [ ] 撃破でRare以上確定ドロップ（Rare70%, Epic20%, Legendary8%, Cursed2%）
- [ ] 迂回した場合はドロップなし
- [ ] 撃破演出（点滅→占有セル一斉Empty化→ドロップ出現）

## 作成・変更するファイル

### 新規作成

```
Scripts/CursedBlood/Enemy/
  BossData.cs
  BossController.cs
  BossArena.cs
  BossUI.cs
```

### 変更

```
Scripts/CursedBlood/Core/TerrainGenerator.cs
Scripts/CursedBlood/Core/ChunkManager.cs
Scripts/CursedBlood/Enemy/BulletManager.cs
Scripts/CursedBlood/Player/PlayerController.cs
Scripts/CursedBlood/Equipment/EquipmentGenerator.cs
Scripts/CursedBlood/Core/GameManager.cs
```

## 詳細設計

### ボス配置

TerrainGeneratorで、チャンク生成時に行番号が深度1000mの倍数（row = 1000m × 16/16 = 行1000相当）に近い場合、ボスを配置する。

具体的には `bossRow = 1000 * n`（n=1,2,3...）の位置に20×20セルのボスを配置。行62,500（=1000m）ごと。

### アリーナ生成 (BossArena.cs)

ボス中心から40×40セル範囲をEmptyに。ボス自体の20×20セルはCellType.Bossのまま。左右に幅10セルの迂回通路を確保。

### 弾幕

弾は8方向に発射。小ブロックグリッドなので弾の速度は20セル/秒（プレイヤーの基本移動50セル/秒より遅い→回避可能）。

崩落警告: ランダムに列5本分を選択→1.5秒間赤く点滅→その列のセルがダメージ付きで崩落（Emptyに変わりつつダメージ判定）。

## 実装順序

1. BossData.cs
2. BossArena.cs
3. BossController.cs
4. BossUI.cs
5. TerrainGenerator.cs変更
6. ChunkManager.cs変更
7. BulletManager.cs変更
8. PlayerController.cs変更
9. EquipmentGenerator.cs変更
10. GameManager.cs変更
11. 動作確認 → APKビルド
