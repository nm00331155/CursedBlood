# Phase 7: 実績＋パッシブ報酬＋ランキング

## 前提

- 依存: Phase 4 完了済み
- 参照: `docs/00_PROJECT_SPEC.md`, `docs/11_SAVE_SYSTEM.md`

## このPhaseのゴール

20個の実績を実装し、解除すると永続パッシブが付与される。ランキングボード（ローカル）で歴代記録を閲覧できる。

## 完了条件チェックリスト

- [ ] 実績20個が定義されている（掘削系5, 戦闘系5, 収集系5, 世代系5）
- [ ] 各実績に解除条件と永続パッシブ報酬が設定されている
- [ ] プレイ中に条件を満たすと実績が解除される
- [ ] 解除時に画面上部に通知ポップアップが表示される（3秒で自動消滅）
- [ ] 解除済み実績のパッシブが以降の全世代に永続適用される（「家訓」システム）
- [ ] 実績一覧画面で進捗と解除状態が確認できる
- [ ] 実績データはSaveManager経由で永続保存される
- [ ] ランキングボード: 歴代最大深度(m)TOP10
- [ ] ランキングボード: 最高スコアTOP10
- [ ] ランキングボード: 最速深度1000m到達（タイム）TOP10
- [ ] ランキングデータはSaveManager経由で永続保存される
- [ ] メニューからランキング画面を開ける

## 実績一覧（20個）

### 掘削系（5個）

| ID | 実績名 | 条件 | パッシブ報酬 |
|---|---|---|---|
| dig_talent | 掘りの才能 | 1プレイで深度500m到達 | 掘削速度+5% |
| ore_nose | 鉱脈の嗅覚 | 鉱石ブロック累計100個破壊 | 鉱石可視範囲+5セル |
| rock_breaker | 岩砕き | 石/硬岩ブロック累計5000個破壊 | 硬ブロック追加ダメージ+10% |
| deep_dweller | 深淵の住人 | 1プレイで深度5000m到達 | 移動速度+5% |
| earth_king | 地底王 | 1プレイで深度10000m到達 | 掘削速度+15% |

### 戦闘系（5個）

| ID | 実績名 | 条件 | パッシブ報酬 |
|---|---|---|---|
| first_battle | 初陣 | 敵を累計10体撃破 | 攻撃力+5% |
| combo_master | コンボマスター | 1プレイで50コンボ達成 | コンボ維持時間+1秒（3秒→4秒） |
| boss_hunter | ボスハンター | ボスを1体撃破 | ボスダメージ+10% |
| iron_wall | 鉄壁 | ガードで弾を累計100回防ぐ | 被ダメ-5% |
| hundred_slayer | 百人斬り | 1プレイで敵100体撃破 | クリティカル率+3% |

### 収集系（5個）

| ID | 実績名 | 条件 | パッシブ報酬 |
|---|---|---|---|
| collector | 拾い屋 | 装備を累計50個拾う | ドロップ率+5% |
| appraiser | 目利き | Rare以上の装備を累計10個入手 | Rare以上ドロップ率+3% |
| rich | 金持ち | 1プレイでゴールド10,000G獲得 | ゴールド取得+10% |
| treasure_hunter | お宝ハンター | Legendary装備を1個入手 | ドロップ率+10% |
| curse_collector | 呪いコレクター | Cursed装備を累計3個入手 | 呪い研究度獲得量+20% |

### 世代系（5個）

| ID | 実績名 | 条件 | パッシブ報酬 |
|---|---|---|---|
| bloodline_start | 血脈の始まり | 第2世代に到達 | 初期HP+10 |
| family_pride | 一族の意地 | 第10世代に到達 | 全ステ+3% |
| debt_free | 借金完済 | 借金を完済する | 全ステ+5%（解放ボーナスとは別） |
| inheritor | 遺産家 | 遺産で累計100,000G引き継ぐ | 遺産率35%に上昇（30%→35%） |
| curse_overcomer | 呪いの克服者 | 寿命を40歳以上にする | 全フェーズの能力低下-50%（Youth0.6→0.8, Twilight0.7→0.85） |

## 作成・変更するファイル

### 新規作成

```
Scripts/CursedBlood/Achievement/
  AchievementData.cs         ← 実績データ定義
  AchievementManager.cs      ← 実績管理・判定・永続化
  AchievementUI.cs           ← 実績一覧画面
  AchievementPopup.cs        ← 解除通知ポップアップ
  RankingManager.cs          ← ランキング管理・永続化
  RankingUI.cs               ← ランキング画面
```

### 変更

```
Scripts/CursedBlood/Core/GameManager.cs        ← 実績・ランキング統合、画面遷移追加
Scripts/CursedBlood/Player/PlayerStats.cs      ← 実績パッシブの適用
Scripts/CursedBlood/UI/HUDManager.cs           ← メニューボタン追加（実績・ランキングへの導線）
Scripts/CursedBlood/Save/SaveData.cs           ← AchievementSaveData, RankingsData追加
```

## 詳細設計

### AchievementData.cs

```
namespace: CursedBlood.Achievement
```

**AchievementId enum**:
```csharp
public enum AchievementId
{
    // 掘削系
    DigTalent, OreNose, RockBreaker, DeepDweller, EarthKing,
    // 戦闘系
    FirstBattle, ComboMaster, BossHunter, IronWall, HundredSlayer,
    // 収集系
    Collector, Appraiser, Rich, TreasureHunter, CurseCollector,
    // 世代系
    BloodlineStart, FamilyPride, DebtFree, Inheritor, CurseOvercomer
}
```

**AchievementDefinition class**（静的データ。マスターデータ的に使用）:
- `AchievementId Id`
- `string Name` — 表示名
- `string Description` — 解除条件テキスト
- `string PassiveDescription` — パッシブ効果テキスト
- `AchievementCategory Category` — Digging / Combat / Collection / Generation

**AchievementState class**（プレイヤーごとの状態）:
- `AchievementId Id`
- `bool IsUnlocked`
- `float Progress` — 0.0〜1.0（進捗率。UI表示用）

### AchievementManager.cs

```
namespace: CursedBlood.Achievement
class AchievementManager
```

**フィールド**:
- `Dictionary<AchievementId, AchievementDefinition> _definitions` — 全20個のマスター定義
- `Dictionary<AchievementId, AchievementState> _states` — 各実績の状態

**累積カウンター**（セーブデータに保存）:
- `int TotalOresBroken`
- `int TotalHardBlocksBroken`
- `int TotalEnemiesKilled`
- `int TotalGuardBlocks`
- `int TotalEquipmentFound`
- `int TotalRarePlusFound`
- `int TotalLegendaryFound`
- `int TotalCursedFound`
- `long TotalGoldInherited`

**メソッド**:

`void Initialize()`:
- 全20個のAchievementDefinitionを登録
- SaveManagerから状態と累積カウンターを読み込み

`List<AchievementId> CheckAndUnlock(PlayerStats stats, FamilyTree tree, DebtManager debt, CurseResearchManager curse)`:
- 全未解除実績の条件を判定
- 条件を満たしたものをUnlockedに変更
- 新規解除された実績IDのリストを返す（ポップアップ表示用）

判定ロジック例:
```csharp
// dig_talent: 1プレイで深度500m
if (!IsUnlocked(DigTalent) && stats.MaxDepthMeters >= 500)
    Unlock(DigTalent);

// ore_nose: 鉱石累計100個
_states[OreNose].Progress = Math.Min(TotalOresBroken / 100f, 1f);
if (!IsUnlocked(OreNose) && TotalOresBroken >= 100)
    Unlock(OreNose);

// combo_master: 1プレイで50コンボ
if (!IsUnlocked(ComboMaster) && stats.MaxCombo >= 50)
    Unlock(ComboMaster);

// debt_free: 借金完済
if (!IsUnlocked(DebtFree) && debt.IsCleared)
    Unlock(DebtFree);

// curse_overcomer: 寿命40歳以上(=MaxLifespan >= 60 + 5×1.714 = 68.57秒)
if (!IsUnlocked(CurseOvercomer) && stats.MaxLifespan >= 68.57f)
    Unlock(CurseOvercomer);
```

`void ApplyAllPassives(PlayerStats stats)`:
- 世代開始時に呼ぶ
- 解除済み全実績のパッシブをPlayerStatsに加算
- 内部でパッシブ値を一時変数に集計し、PlayerStats.AchievementBonuses にセット

パッシブ適用例:
```csharp
if (IsUnlocked(DigTalent))
    bonuses.DigSpeedBonus += 5f; // +5%
if (IsUnlocked(BloodlineStart))
    bonuses.HpBonus += 10;
if (IsUnlocked(CurseOvercomer)) {
    bonuses.YouthMultiplierOverride = 0.8f;  // 0.6→0.8
    bonuses.TwilightMultiplierOverride = 0.85f; // 0.7→0.85
}
```

`void IncrementCounter(CounterType type, int amount = 1)`:
- 累積カウンターを加算（プレイ中にイベント発生時に呼ぶ）

`AchievementSaveData ToSaveData()` / `void LoadFrom(AchievementSaveData data)`:
- SaveManager連携

**チェックタイミング**:
- 毎フレームチェックすると重いので、以下のイベント時にのみチェックする:
  - 敵撃破時
  - ブロック掘削時（10回に1回の間引き）
  - アイテム入手時
  - 世代交代時
  - ボス撃破時
  - 深度更新時（100mごとに1回チェック）

### AchievementPopup.cs

```
namespace: CursedBlood.Achievement
partial class AchievementPopup : CanvasLayer
```

CanvasLayer（Layer=70、最前面）。

**表示**:
- 画面上部中央にスライドインする通知バー（幅800px、高さ100px）
- 背景: レアリティ金色のグラデーション
- テキスト: 「実績解除！」+ 実績名 + パッシブ効果
- 3秒で自動スライドアウト
- タップで即消滅
- 複数同時解除の場合はキューに入れて順番に表示

**アニメーション**:
```csharp
// スライドイン
var tween = CreateTween();
tween.TweenProperty(panel, "position:y", 30f, 0.3f)
    .From(-120f)
    .SetEase(Tween.EaseType.Out);
// 3秒待機
tween.TweenInterval(3.0);
// スライドアウト
tween.TweenProperty(panel, "position:y", -120f, 0.3f)
    .SetEase(Tween.EaseType.In);
tween.TweenCallback(Callable.From(() => OnPopupFinished()));
```

### AchievementUI.cs

```
namespace: CursedBlood.Achievement
partial class AchievementUI : CanvasLayer
```

CanvasLayer（Layer=20）。

**レイアウト**:
```
[半透明黒背景]
┌──────────────────────────────────────┐
│ [×閉じる]            実績一覧         │ y: 100
├──────────────────────────────────────┤
│ [掘削] [戦闘] [収集] [世代]  ← タブ  │ y: 200
├──────────────────────────────────────┤
│                                      │
│ ■ 掘りの才能         [解除済み✓]    │ y: 280〜
│   深度500m到達                       │
│   報酬: 掘削速度+5%                  │
│   ██████████ 100%                   │
│                                      │
│ □ 鉱脈の嗅覚         [未解除]       │
│   鉱石累計100個破壊                  │
│   報酬: 鉱石可視範囲+5セル           │
│   ████░░░░░░ 45%                   │
│                                      │
│ ...（スクロール可能）                │
└──────────────────────────────────────┘
```

- 解除済みは金枠＋チェックマーク
- 未解除はグレー枠＋プログレスバー
- タブ切り替えでカテゴリフィルタ

**シグナル**: `CloseRequested`

### RankingManager.cs

```
namespace: CursedBlood.Achievement
class RankingManager
```

**RankingEntry class**:
- `string Name` — プレイヤー名
- `int Generation` — 世代番号
- `long Value` — スコアまたは深度(m)またはタイム(ミリ秒)
- `string DateString` — 記録日時

**フィールド**:
- `List<RankingEntry> DepthRanking` — 最大深度TOP10
- `List<RankingEntry> ScoreRanking` — 最高スコアTOP10
- `List<RankingEntry> SpeedRanking` — 最速深度1000m到達TOP10（ミリ秒単位。到達未経験は記録なし）

**メソッド**:

`bool TryRegister(PlayerStats stats, string playerName)`:
- プレイ終了時に呼ぶ
- 各ランキングにエントリーを追加（TOP10に入る場合のみ）
- ランキングが更新された場合true

`RankingsSaveData ToSaveData()` / `void LoadFrom(RankingsSaveData data)`:
- SaveManager連携

### RankingUI.cs

```
namespace: CursedBlood.Achievement
partial class RankingUI : CanvasLayer
```

CanvasLayer（Layer=20）。

**レイアウト**:
```
┌──────────────────────────────────────┐
│ [×閉じる]           ランキング       │
├──────────────────────────────────────┤
│ [最大深度] [最高スコア] [最速1000m]  │  ← タブ
├──────────────────────────────────────┤
│  1位  深掘 タケル (第47代)   12,345m │
│  2位  地底 サクラ (第23代)   10,892m │
│  3位  ...                            │
│  ...                                 │
│ 10位  ...                            │
└──────────────────────────────────────┘
```

- 自分の最新記録はハイライト
- タブ切り替えで3種のランキング

**シグナル**: `CloseRequested`

### PlayerStats変更

**AchievementBonuses struct**追加:
```csharp
public struct AchievementBonuses
{
    public float DigSpeedBonus;       // %加算
    public float AttackBonus;         // %加算
    public float BossDamageBonus;     // %加算
    public float DamageReductionBonus; // %加算
    public float MoveSpeedBonus;      // %加算
    public float CritRateBonus;       // %加算
    public float GoldBonus;           // %加算
    public float DropRateBonus;       // %加算
    public float CurseResearchBonus;  // %加算
    public int HpBonus;               // フラット加算
    public int OreVisionBonus;        // セル加算
    public float ComboTimerBonus;     // 秒加算
    public float InheritanceRateOverride; // 0なら無効、0.35なら35%
    public float YouthMultiplierOverride; // 0なら無効
    public float TwilightMultiplierOverride;
    public float AllStatsMultiplier;  // 1.0 + 各種全ステ%の合算
}
```

`AchievementBonuses AchBonuses` フィールドを追加。世代開始時にAchievementManager.ApplyAllPassives()で設定。

各Effective系プロパティにAchBonusesの値を加算する。

## 実装順序

1. AchievementData.cs（ID・定義）
2. AchievementManager.cs（判定・パッシブ適用・SaveManager連携）
3. AchievementPopup.cs（通知UI）
4. AchievementUI.cs（一覧画面）
5. RankingManager.cs（ランキング管理・SaveManager連携）
6. RankingUI.cs（ランキング画面）
7. SaveData.cs変更（AchievementSaveData, RankingsData構造追加）
8. PlayerStats.cs変更（AchievementBonuses適用）
9. GameManager.cs変更（実績チェック呼び出し、ランキング登録、画面遷移追加）
10. HUDManager.cs変更（実績・ランキングへのメニューボタン）
11. 動作確認 → APKビルド
