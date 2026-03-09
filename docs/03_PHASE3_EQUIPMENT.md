# Phase 3: 装備＋レアリティ＋付与効果＋掘削幅変化＋装備画面

## 前提

- 依存: Phase 2 完了済み
- 参照: `docs/00_PROJECT_SPEC.md`

## このPhaseのゴール

敵や鉱石から装備がドロップし、レアリティ・掘削幅・掘削形状・付与効果がランダム生成される。装備画面で4カテゴリ（掘削具・防具・アクセサリ・靴）を装着でき、掘削具を変えると掘り方（幅と形状）が目に見えて変わる。

## 完了条件チェックリスト

- [ ] 装備アイテムがドロップする（敵撃破時・鉱石通過時に確率で）
- [ ] 装備カテゴリ4種: 掘削具、防具、アクセサリ、靴
- [ ] レアリティ6段階が正しい確率で生成される（Common 63.12%, Uncommon 25%, Rare 10%, Epic 1.5%, Legendary 0.3%, Cursed 0.08%）
- [ ] 基礎ステータスが深度帯とレアリティ倍率に応じて計算される
- [ ] 掘削具に攻撃力・掘削速度補正・掘削幅・掘削形状パラメータがある
- [ ] 掘削幅がレアリティで変わる（Common=5, Uncommon/Rare=5〜7, Epic=7〜9, Legendary/Cursed=9〜11）
- [ ] 掘削形状が3種類（Square/Diamond/Fan）からランダム決定（Rare以上）
- [ ] 掘削具を変更すると実際の掘削範囲が変わることを目視確認できる
- [ ] 付与効果が0〜3個ランダム付与される（レアリティに応じた個数）
- [ ] 特殊効果「掘削幅+2」が正しく機能する
- [ ] Cursedアイテムにデメリットが1つ付与される
- [ ] ドロップアイテムがフィールド上に表示される（レアリティ色の光る四角形、4×4セル=64×64px）
- [ ] プレイヤーの占有範囲と重なったら自動拾得
- [ ] 装備画面（ポーズ画面）が開ける（下部の装備アイコンタップ）
- [ ] 装備画面で4スロットに装着・交換ができる
- [ ] インベントリ（バッグ）に最大20個保持
- [ ] バッグが満杯のとき、新アイテム拾得で比較画面が出る（入れ替え or 捨てる）
- [ ] 装備中の掘削具に応じてプレイヤーの見た目（色/形状）が変わる
- [ ] 装備ステータスがPlayerStatsに反映される（攻撃力、防御力、移動速度等）
- [ ] 付与効果20種が全て機能する
- [ ] ドロップ時のレアリティ演出（色枠 + テキスト）
- [ ] レアリティ色: Common=灰, Uncommon=緑, Rare=青, Epic=紫, Legendary=金, Cursed=赤黒

## 作成・変更するファイル

### 新規作成

```
Scripts/CursedBlood/Equipment/
  EquipmentData.cs           ← 装備データ定義
  EquipmentGenerator.cs      ← 装備ランダム生成
  EquipmentEffect.cs         ← 付与効果の定義と適用
  Inventory.cs               ← インベントリ管理
  EquipmentUI.cs             ← 装備画面UI
  DroppedItem.cs             ← フィールド上のドロップアイテム管理
```

### 変更

```
Scripts/CursedBlood/Player/PlayerStats.cs     ← 装備ステータス反映、DigWidth/DigShape動的変更
Scripts/CursedBlood/Player/PlayerController.cs ← アイテム拾得、掘削パラメータを装備から取得
Scripts/CursedBlood/Core/GameManager.cs        ← 装備画面統合、ポーズ状態追加
Scripts/CursedBlood/Core/ChunkManager.cs       ← ドロップアイテム描画
Scripts/CursedBlood/UI/HUDManager.cs           ← 装備アイコン表示
```

## 詳細設計

### EquipmentData.cs

```
namespace: CursedBlood.Equipment
```

**EquipmentCategory enum**: Pickaxe, Armor, Accessory, Boots

**Rarity enum**: Common, Uncommon, Rare, Epic, Legendary, Cursed

**EquipmentData class**:
- `string Name` — 自動生成名（「鉄のツルハシ+2」等）
- `EquipmentCategory Category`
- `Rarity Rarity`
- `int DropDepth` — ドロップした深度(m)（基礎ステータス算出に使用）
- `float BaseValue` — 基礎値（カテゴリ依存: 掘削具=攻撃力, 防具=防御力, アクセサリ=特殊値, 靴=速度補正）
- `int DigWidth` — 掘削幅（掘削具のみ。他カテゴリは0）
- `DigShape Shape` — 掘削形状（掘削具のみ）
- `float DigSpeedMultiplier` — 掘削速度倍率（掘削具のみ。1.0=標準）
- `List<EquipmentEffect> Effects` — 付与効果（0〜3個）
- `EquipmentEffect Demerit` — Cursed用デメリット（null許容）

**基礎値算出ロジック**:

掘削具（攻撃力）:
```
深度帯から基礎レンジ取得:
  0〜1000m → 10〜30
  1000〜3000m → 30〜100
  3000〜6000m → 100〜500
  6000〜10000m → 500〜2000
  10000m〜 → 2000〜10000

基礎値 = Lerp(rangeMin, rangeMax, random(0,1)) × レアリティ倍率
```

防具（防御力）: 基礎レンジは掘削具の1/3。レアリティ倍率は同じ。

靴（速度補正）: 0.8〜1.5の範囲。レアリティが高いほど1.5に近づく。

アクセサリ: 基礎値は使わず、付与効果のみで価値が決まる。BaseValue=0。

**掘削幅の決定**:
```csharp
int digWidth = rarity switch {
    Rarity.Common => 5,
    Rarity.Uncommon => rng.Next(0, 2) == 0 ? 5 : 7,
    Rarity.Rare => rng.Next(0, 2) == 0 ? 5 : 7,
    Rarity.Epic => rng.Next(0, 2) == 0 ? 7 : 9,
    Rarity.Legendary => rng.Next(0, 2) == 0 ? 9 : 11,
    Rarity.Cursed => rng.Next(0, 2) == 0 ? 9 : 11,
};
```

**掘削形状の決定**:
```csharp
DigShape shape = rarity switch {
    Rarity.Common or Rarity.Uncommon => DigShape.Square,
    _ => (DigShape)rng.Next(0, 3)  // Square/Diamond/Fan均等
};
```

**レアリティ色**:

| Rarity | Color |
|---|---|
| Common | (0.6, 0.6, 0.6) |
| Uncommon | (0.3, 0.8, 0.3) |
| Rare | (0.3, 0.5, 1.0) |
| Epic | (0.7, 0.3, 0.9) |
| Legendary | (1.0, 0.85, 0.2) |
| Cursed | (0.8, 0.1, 0.1) |

**名前自動生成**:

```
素材名プール: 木, 石, 鉄, 鋼, ミスリル, オリハルコン, 闇鉄, 星鋼, 竜骨, 呪鉄
カテゴリ名: ツルハシ, 鎧, 護符, 靴
名前 = "[素材名]の[カテゴリ名]+[付与効果数]"
素材名はドロップ深度で決まる（浅い=木〜石、深い=星鋼〜呪鉄）
```

### EquipmentGenerator.cs

```
namespace: CursedBlood.Equipment
static class EquipmentGenerator
```

**メソッド**:

`EquipmentData Generate(int depthMeters, Rarity? forceRarity = null)`:
1. カテゴリをランダム選択（均等25%ずつ）
2. レアリティ決定（forceRarityがあれば上書き。ボスドロップ用）
3. 基礎値算出（深度帯×レアリティ倍率）
4. 掘削具の場合: 掘削幅・形状・速度倍率を決定
5. 付与効果数をレアリティに応じたレンジからランダム決定
6. 効果プール20種から重複なしで抽選
7. Cursedの場合: デメリット1つを追加抽選
8. 名前自動生成
9. EquipmentDataを返す

**レアリティ倍率**:
| Rarity | 倍率 |
|---|---|
| Common | ×1 |
| Uncommon | ×3 |
| Rare | ×10 |
| Epic | ×50 |
| Legendary | ×500 |
| Cursed | ×5,000 |

**付与効果数**:
| Rarity | 効果数 |
|---|---|
| Common | 0〜1 |
| Uncommon | 0〜2 |
| Rare | 1〜2 |
| Epic | 2〜3 |
| Legendary | 2〜3 |
| Cursed | 3 |

### EquipmentEffect.cs

```
namespace: CursedBlood.Equipment
```

**EffectType enum（20種）**:

攻撃系5種:
- `DigSpeed` — 掘削速度+X%
- `CritRate` — クリティカル率+X%
- `CritDamage` — クリティカルダメージ+X%
- `HardBlockDamage` — 硬ブロック追加ダメージ+X%
- `BossDamage` — ボスダメージ+X%

防御系4種:
- `DamageReduction` — 被ダメージ-X%
- `HpBonus` — HP+X
- `InvincibleOnHit` — 被弾時無敵+X秒
- `BulletSlowAura` — 弾速低下オーラ（周囲20セル内の弾の速度-X%）

移動系2種:
- `MoveSpeed` — 移動速度+X%
- `DirectionChangeBoost` — 方向転換時に2セル瞬間移動

収集系4種:
- `GoldBonus` — ゴールド取得+X%
- `DropRateBonus` — ドロップ率+X%
- `OreVisionRange` — 鉱石可視範囲+Xセル（鉱石が光って見える範囲拡大）
- `DebtRepayBonus` — 借金返済ボーナス+X%

特殊系5種:
- `DigWidthPlus2` — 掘削幅+2セル
- `ChainExplosion` — ブロック破壊時に隣接ブロックも10%の確率で連鎖破壊
- `GoldMagnet` — 周囲15セル内のゴールドを自動回収
- `CurseAbsorb` — 呪い研究度獲得量+X%
- `LastSpurt` — 残り寿命10秒以下で全ステ+50%

**DemeritType enum（4種）**:
- `HpDrain` — 常時HP減少(-1/秒)
- `GoldPenalty` — ゴールド取得-50%
- `SpeedPenalty` — 移動速度-20%
- `DamagePenalty` — 被ダメ+30%

**EquipmentEffect class**:
- `EffectType Type`（デメリットの場合はDemeritTypeをキャストして使用、またはisDemeritフラグ）
- `float Value` — 効果量
- `bool IsDemerit`

**効果量の基準値と範囲**:

| EffectType | 基準値 | 単位 |
|---|---|---|
| DigSpeed | 10 | % |
| CritRate | 5 | % |
| CritDamage | 20 | % |
| HardBlockDamage | 15 | % |
| BossDamage | 10 | % |
| DamageReduction | 5 | % |
| HpBonus | 20 | flat |
| InvincibleOnHit | 0.5 | 秒 |
| BulletSlowAura | 20 | % |
| MoveSpeed | 10 | % |
| DirectionChangeBoost | 2 | セル |
| GoldBonus | 15 | % |
| DropRateBonus | 10 | % |
| OreVisionRange | 5 | セル |
| DebtRepayBonus | 10 | % |
| DigWidthPlus2 | 2 | セル（固定） |
| ChainExplosion | 10 | %（発動率） |
| GoldMagnet | 15 | セル（範囲） |
| CurseAbsorb | 20 | % |
| LastSpurt | 50 | %（固定） |

**レアリティ別の効果量倍率**:
| Rarity | 倍率 |
|---|---|
| Common | ×0.5〜×1.0 |
| Uncommon | ×1.0〜×1.5 |
| Rare | ×1.5〜×2.5 |
| Epic | ×2.5〜×4.0 |
| Legendary | ×4.0〜×8.0 |
| Cursed | ×8.0〜×15.0 |

実際の効果値 = 基準値 × Lerp(倍率下限, 倍率上限, random(0,1))

### Inventory.cs

```
namespace: CursedBlood.Equipment
class Inventory
```

**フィールド**:
- `EquipmentData EquippedPickaxe` — 掘削具スロット
- `EquipmentData EquippedArmor` — 防具スロット
- `EquipmentData EquippedAccessory` — アクセサリスロット
- `EquipmentData EquippedBoots` — 靴スロット
- `List<EquipmentData> Bag` — バッグ（最大20個）

**メソッド**:

`void Equip(EquipmentData item)`:
- itemのカテゴリに応じたスロットに装着
- 旧装備がある場合はBagに移動
- Bagが満杯の場合は旧装備を破棄（警告ログ出力）

`bool AddToBag(EquipmentData item)`:
- Bag.Count < 20 ならリストに追加してtrue
- 満杯ならfalse

`void RemoveFromBag(int index)`:
- Bag[index]を削除

`EquipmentData GetEquipped(EquipmentCategory category)`:
- カテゴリに応じたスロットの装備を返す

**ステータス集計**:

`EquipmentStats CalculateTotalStats()`:
- 全装備スロットの基礎値と全付与効果を集計
- 返り値の EquipmentStats struct:
  - `float AttackPower` — 掘削具の基礎攻撃力
  - `float DefensePower` — 防具の基礎防御力
  - `float SpeedMultiplier` — 靴の基礎速度補正
  - `int DigWidth` — 掘削具の掘削幅 + DigWidthPlus2効果
  - `DigShape DigShape` — 掘削具の掘削形状
  - `float DigSpeedMultiplier` — 掘削具の掘削速度倍率
  - `Dictionary<EffectType, float> EffectTotals` — 全効果の合算値

### DroppedItem.cs

```
namespace: CursedBlood.Equipment
class DroppedItem
```

- `EquipmentData Equipment` — アイテムデータ
- `Vector2I Position` — フィールド上のセル座標（中心）
- `int Size = 4` — 表示サイズ（4×4セル=64×64px）
- `float SpawnTime` — 出現時刻（点滅アニメ用）
- `bool IsPickedUp`

**フィールド上の管理**:
- GameManagerまたはChunkManagerが `List<DroppedItem> _droppedItems` を保持
- 毎フレーム、プレイヤーの占有範囲とドロップアイテムの範囲の重なりをチェック
- 重なっていたら拾得処理 → Bagに追加（満杯ならItemCompare画面へ遷移）

**描画**:
- レアリティ色の四角形
- Rare以上は外枠が点滅
- Legendary以上は周囲に光の放射線エフェクト（DrawLine数本で簡易表現）

### EquipmentUI.cs

```
namespace: CursedBlood.Equipment
partial class EquipmentUI : CanvasLayer
```

CanvasLayer（Layer=20）で実装。GameState.Equipmentのときに表示。

**レイアウト（1080×1920全画面オーバーレイ）**:

```
[半透明黒背景]
┌──────────────────────────────────────┐
│ [×閉じる]            装備画面         │ y: 100
├──────────────────────────────────────┤
│                                      │
│  [掘削具スロット]  [防具スロット]     │ y: 200〜600
│  [アクセスロット]  [靴スロット]       │   4スロット（各200×200px）
│                                      │
│  ─── 装備中ステータス概要 ───       │ y: 620
│  攻撃力: XXX  防御: XXX  速度: X.Xx  │
│  掘削幅: X  形状: Square             │
│                                      │
├──────────────────────────────────────┤
│  バッグ (X/20)                       │ y: 750
│  [アイテム1] [アイテム2] [アイテム3]  │
│  [アイテム4] [アイテム5] [アイテム6]  │  4列×5行のグリッド
│  ...                                 │  各アイテム200×200px
│  (スクロール可能)                    │
└──────────────────────────────────────┘ y: 1920
```

**操作**:
- スロットタップ → その装備の詳細ポップアップ（外す/捨てるボタン）
- バッグ内アイテムタップ → ItemCompare画面（装備する/捨てる/戻るボタン）
- ×ボタンまたはバックキーで閉じる → GameState.Playingに復帰

**シグナル**:
- `CloseRequested` — 閉じるボタン押下時
- `ItemCompareRequested(EquipmentData item)` — アイテムの比較画面要求

### ItemCompare画面（EquipmentUI内のポップアップ）

```
┌──────────────────────────┐
│    アイテム比較           │
├────────────┬─────────────┤
│ [現在装備]  │ [新アイテム] │
│ 名前       │ 名前        │
│ 攻撃力: XX │ 攻撃力: XX  │  ← 上がる数値は緑、下がるは赤
│ 掘削幅: X  │ 掘削幅: X   │
│ 効果1      │ 効果1       │
│ 効果2      │ 効果2       │
├────────────┴─────────────┤
│ [装備する] [捨てる] [戻る]│
└──────────────────────────┘
```

### PlayerStats変更点

```csharp
public Inventory Inventory { get; set; } = new Inventory();

public int EffectiveDigWidth {
    get {
        int baseWidth = Inventory.EquippedPickaxe?.DigWidth ?? 5;
        int bonus = (int)(Inventory.CalculateTotalStats()
            .EffectTotals.GetValueOrDefault(EffectType.DigWidthPlus2, 0f));
        return Math.Clamp(baseWidth + bonus, 5, 15);
    }
}

public DigShape EffectiveDigShape =>
    Inventory.EquippedPickaxe?.Shape ?? DigShape.Square;

public float EffectiveMoveInterval {
    get {
        float baseInterval = BaseMoveInterval / PhaseMultiplier;
        float speedBonus = 1f + Inventory.CalculateTotalStats()
            .EffectTotals.GetValueOrDefault(EffectType.MoveSpeed, 0f) / 100f;
        float bootsMultiplier = Inventory.EquippedBoots?.BaseValue ?? 1.0f;
        return Math.Max(baseInterval / (speedBonus * bootsMultiplier), 0.005f);
    }
}

public float EffectiveAttackPower {
    get {
        float base_ = Inventory.EquippedPickaxe?.BaseValue ?? DigPower;
        return base_;
    }
}

public float EffectiveDamageReduction {
    get {
        float armorBase = Inventory.EquippedArmor?.BaseValue ?? 0f;
        float effectBonus = Inventory.CalculateTotalStats()
            .EffectTotals.GetValueOrDefault(EffectType.DamageReduction, 0f);
        return Math.Min(armorBase + effectBonus, 80f); // 最大80%軽減
    }
}
```

## 実装順序

1. EquipmentEffect.cs（効果定義、依存なし）
2. EquipmentData.cs（掘削幅・形状パラメータ含む）
3. EquipmentGenerator.cs（幅・形状・効果のランダム生成）
4. Inventory.cs（インベントリ管理）
5. DroppedItem.cs（フィールドドロップ管理）
6. PlayerStats.cs変更（EffectiveDigWidth/Shape/MoveInterval/AttackPower/DamageReduction）
7. PlayerController.cs変更（掘削パラメータを装備から動的取得、拾得処理）
8. EquipmentUI.cs（装備画面 + ItemCompareポップアップ）
9. ChunkManager.cs変更（ドロップアイテム描画）
10. GameManager.cs変更（装備画面統合、GameState.Equipment/ItemCompare追加）
11. HUDManager.cs変更（装備アイコン表示）
12. 動作確認 → APKビルド
