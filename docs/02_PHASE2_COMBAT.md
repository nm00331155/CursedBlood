# Phase 2: 敵＋弾＋ガード＋ゴールドドロップ

## 前提

- 依存: Phase 1 完了済み
- 参照: `docs/00_PROJECT_SPEC.md`, `docs/01_PHASE1_GRID_MOVEMENT.md`

## このPhaseのゴール

グリッド上に複数セルを占有する敵が出現し、8方向に弾を撃つ。プレイヤーは掘り進むことで敵を撃破し、ゴールドを獲得する。ガードで弾を防げる。コンボシステムが動作する。

## 完了条件チェックリスト

- [ ] 敵4種がグリッドに出現する（複数セル占有）
- [ ] スライム型: 3×3セル(48px)、弾なし、硬さ2
- [ ] 射撃型: 4×4セル(64px)、2秒間隔で8方向のうちランダム1方向に1発、硬さ3
- [ ] 拡散型: 5×5セル(80px)、3秒間隔で8方向同時に弾、硬さ4
- [ ] 自爆型: 3×3セル(48px)、隣接で3秒カウントダウン→周囲10セル大ダメージ、硬さ2
- [ ] 敵の占有セルはCellType.Enemyで埋められている
- [ ] プレイヤーが敵の占有セルに掘り進むと敵を撃破できる
- [ ] 敵撃破に必要な通過回数 = 硬さ（掘削力で短縮可能）
- [ ] 弾がセル上を直線移動する（1セル/0.05秒=20セル/秒、16セルで消滅）
- [ ] 弾は8方向に発射される
- [ ] 弾がブロック（Dirt/Stone/HardRock/Bedrock）に当たると消滅する
- [ ] 弾がプレイヤーの占有範囲に当たるとダメージ
- [ ] HPが0になると死亡する
- [ ] ガードで弾を防げる（ダメージ0）
- [ ] ガード中は移動停止
- [ ] 鉱石ブロック(Ore)がグリッドに出現する（金色）
- [ ] 鉱石ブロック通過でゴールド獲得
- [ ] コンボシステム: 敵撃破で+1、3秒間撃破なしでリセット
- [ ] HUDにゴールド、コンボ表示
- [ ] 深度帯ごとに敵の色が変わる（パレットスワップ4色）

## 作成・変更するファイル

### 新規作成

```
Scripts/CursedBlood/Enemy/
  EnemyData.cs               ← 敵データ定義
  EnemyInstance.cs           ← 個別敵インスタンス管理
  EnemyManager.cs            ← 敵の生成・行動・描画管理
  BulletManager.cs           ← 弾の移動・当たり判定・描画
```

### 変更

```
Scripts/CursedBlood/Core/
  CellType.cs                ← Enemy/Ore値の活用開始
  TerrainGenerator.cs        ← 敵・鉱石の配置ロジック追加
  ChunkManager.cs            ← 敵・鉱石・弾の描画追加
  GameManager.cs             ← EnemyManager/BulletManager統合
Scripts/CursedBlood/Player/
  PlayerController.cs        ← 敵撃破・被弾・ゴールド取得
  PlayerStats.cs             ← ゴールド・コンボ管理
Scripts/CursedBlood/UI/
  HUDManager.cs              ← ゴールド・コンボ表示追加
```

## 詳細設計

### 敵の配置

TerrainGeneratorで敵を配置する際、敵のサイズ分のセルをCellType.Enemyで埋め、敵のID（byte上位ビットまたは別配列）で識別する。

敵配置用に、ChunkManagerに `Dictionary<int, EnemyInstance> _enemies` を追加。キーは敵ID。

**敵出現率**（チャンク16行あたり）:
- 深度0〜200m: 0〜1体（スライムのみ）
- 深度200〜500m: 1〜2体（スライム＋射撃型）
- 深度500〜1000m: 2〜3体（＋拡散型）
- 深度1000m〜: 3〜5体（＋自爆型）

### EnemyInstance.cs

```
namespace: CursedBlood.Enemy
class EnemyInstance
```

- `int Id` — 一意ID
- `EnemyType Type`
- `Vector2I TopLeft` — 占有範囲の左上セル座標
- `int Size` — 3〜6（正方形）
- `int MaxHp`, `int CurrentHp`
- `float AttackTimer`
- `bool IsActive` — プレイヤーが近くにいるか（活性化範囲: 30セル以内）
- `bool FuseStarted` — Bomber用
- `float FuseTimer` — Bomber用（3秒）

### 弾のサイズ

弾は2×2セル（32×32px）。小さいが、小ブロックグリッドでは十分に視認できる。

### ゴールド計算

鉱石通過時: 基本ゴールド = 10 + 深度(m) × 0.05。ランダム幅 ×0.8〜×1.2。

## 実装順序

1. EnemyData.cs（定義）
2. EnemyInstance.cs（インスタンス管理）
3. TerrainGenerator.cs変更（敵・鉱石配置）
4. BulletManager.cs（弾処理）
5. EnemyManager.cs（敵行動・描画）
6. PlayerController.cs変更（撃破・被弾・ゴールド）
7. PlayerStats.cs変更（コンボ管理）
8. ChunkManager.cs変更（描画連携）
9. HUDManager.cs変更（ゴールド・コンボ）
10. GameManager.cs変更（統合）
11. 動作確認 → APKビルド
