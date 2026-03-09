# Phase 9: スキル＋演出＋インフレ調整＋男女切替

## 前提

- 依存: Phase 8 完了済み
- 参照: `docs/00_PROJECT_SPEC.md`, `docs/12_SCREEN_FLOW.md`

## このPhaseのゴール

ダブルタップスキルシステム、視覚・音声演出、数値バランス調整、男女キャラ切替、タイトル画面を実装し、ゲームを完成状態にする。

## 完了条件チェックリスト

### スキルシステム
- [ ] スキルゲージが下部エリアに円形で表示される
- [ ] スキルゲージは敵撃破・掘削で溜まる
- [ ] ダブルタップ（Enter）でスキル発動
- [ ] スキルは掘削具に紐づく（掘削具ごとに固有スキル）
- [ ] 4種のスキルが実装されている
- [ ] スキル発動時に画面エフェクト
- [ ] ゲージ消費後はゼロから溜め直し

### 演出強化
- [ ] 掘削時にパーティクル（ブロック色の小粒が飛び散る）
- [ ] 敵撃破時にパーティクル
- [ ] レアアイテムドロップ時に派手なエフェクト
- [ ] ボス撃破時に画面揺れ＋爆発
- [ ] コンボ数字の段階演出（10/50/100で変化）
- [ ] フェーズ遷移演出（少年期→青年期→晩年期）
- [ ] 晩年期の黒霧演出
- [ ] 死亡スローモーション演出
- [ ] 掘削幅が広い時の「ゴッソリ掘れる」快感演出
- [ ] BGM/SEのリソースパス定義と再生制御

### インフレ調整
- [ ] BalanceConfig.jsonでパラメータ一元管理
- [ ] 初回プレイで深度500〜1000mが目安
- [ ] 30世代でボス1体撃破が目安
- [ ] 100世代で深度10000m到達が目安
- [ ] 装備のインフレカーブが「気持ちいい」範囲

### 男女切替
- [ ] 世代ごとに50%で男女ランダム
- [ ] 男=青系色、女=赤系色
- [ ] ステータス差なし

### 最終仕上げ
- [ ] タイトル画面実装
- [ ] ポーズ機能
- [ ] 設定画面（BGM/SE音量、振動ON/OFF）
- [ ] チュートリアル（初回プレイ時のみ）
- [ ] 全セーブデータ整合性チェック
- [ ] メモリリーク確認
- [ ] Android実機60fps安定

## 作成・変更するファイル

### 新規作成

```
Scripts/CursedBlood/Skill/
  SkillData.cs               ← スキル定義（4種）
  SkillManager.cs            ← スキルゲージ管理・発動判定
  SkillEffects.cs            ← 各スキルの効果実装
Scripts/CursedBlood/Effects/
  ParticleManager.cs         ← パーティクル生成・管理
  ScreenEffects.cs           ← 画面エフェクト（フラッシュ、揺れ、スロー、黒霧）
Scripts/CursedBlood/Audio/
  AudioManager.cs            ← BGM/SE管理（シングルトン）
Scripts/CursedBlood/UI/
  TitleScreen.cs             ← タイトル画面
  PauseMenu.cs               ← ポーズメニュー
  SettingsUI.cs              ← 設定画面
  TutorialOverlay.cs         ← チュートリアルオーバーレイ
Scripts/CursedBlood/Config/
  BalanceConfig.cs           ← バランスパラメータ読み込み・管理
Scenes/CursedBlood/
  TitleScene.tscn            ← タイトル画面シーン
```

### 変更

```
Scripts/CursedBlood/Core/GameManager.cs        ← スキル・演出・タイトル・ポーズ全統合
Scripts/CursedBlood/Player/PlayerController.cs ← ダブルタップ検出、スキル発動トリガー
Scripts/CursedBlood/Player/PlayerStats.cs      ← 性別フラグ、BalanceConfig参照
Scripts/CursedBlood/UI/HUDManager.cs           ← スキルゲージ描画、演出連携
Scripts/CursedBlood/Camera/GameCamera.cs       ← 画面揺れ実装、スローモーション
Scripts/CursedBlood/Core/ChunkManager.cs       ← BalanceConfig参照で動的パラメータ
Scripts/CursedBlood/Core/TerrainGenerator.cs   ← BalanceConfig参照
Scripts/CursedBlood/Save/SaveData.cs           ← SettingsData追加
```

## 詳細設計

### SkillData.cs

```
namespace: CursedBlood.Skill
```

**SkillType enum**: LinearPierce, AreaBreak, InvincibleDash, ScreenAttack

**SkillDefinition class**:
- `SkillType Type`
- `string Name`
- `string Description`
- `float GaugeCost = 100f` — 全スキル共通（ゲージ100%消費）

**4種のスキル**:

| SkillType | 名前 | 効果 | 演出 |
|---|---|---|---|
| LinearPierce | 貫通掘削 | 進行方向に30セルを幅5で一直線に即時破壊。敵もダメージ | 直線上の白い閃光 |
| AreaBreak | 範囲破壊 | プレイヤー中心に15×15セルを即時破壊 | 中心から広がる衝撃波（円形の白い輪） |
| InvincibleDash | 無敵ダッシュ | 3秒間無敵＋移動速度3倍。ブロック硬度無視 | プレイヤーが金色に光る＋残像エフェクト |
| ScreenAttack | 全画面攻撃 | 画面内の全敵に攻撃力×5のダメージ | 画面全体が白くフラッシュ→赤いスラッシュ線が交差 |

**掘削具とスキルの紐づけ**:
```csharp
// EquipmentDataにSkillType追加
public SkillType Skill { get; set; }

// EquipmentGenerator内で決定
item.Skill = rarity switch {
    Rarity.Common or Rarity.Uncommon => SkillType.LinearPierce,  // 序盤は基本スキル
    _ => (SkillType)rng.Next(0, 4)  // Rare以上はランダム
};
```

### SkillManager.cs

```
namespace: CursedBlood.Skill
partial class SkillManager : Node2D
```

**フィールド**:
- `float Gauge` — 0.0〜100.0
- `float MaxGauge = 100f`
- `bool IsReady` → Gauge >= MaxGauge
- `PlayerStats Stats`
- `ChunkManager Chunks`
- `EnemyManager Enemies`

**ゲージ蓄積**:
```csharp
public void OnEnemyKilled()   { Gauge += 20f; }
public void OnBlockDug()      { Gauge += 1f; }
public void OnBossHit()       { Gauge += 5f; }
// Gaugeは MaxGauge でクランプ
```

**ダブルタップ検出（PlayerController側）**:
```csharp
private float _lastTapTime = -1f;
private const float DoubleTapInterval = 0.3f; // 0.3秒以内の2タップ

// タッチ/クリック解放時
if (timeSinceLastTap < DoubleTapInterval) {
    OnDoubleTap();
    _lastTapTime = -1f;
} else {
    _lastTapTime = currentTime;
}

// キーボード（Enter）
if (Input.IsKeyPressed(Key.Enter) && !_enterWasPressed) {
    OnSkillInput();
}
```

**発動メソッド**:

`void ActivateSkill(SkillType type)`:
1. Gauge を 0 にリセット
2. type に応じた効果を実行
3. ScreenEffects でエフェクト再生
4. AudioManager で SE再生

```csharp
switch (type) {
    case SkillType.LinearPierce:
        var digArea = DigHelper.GetLinearPierce(Stats.GridPosition, _moveDirection, 30, 5);
        DigHelper.ExecuteDig(Chunks, digArea);
        // 範囲内の敵にもダメージ
        Enemies.DamageInArea(digArea, Stats.EffectiveAttackPower);
        ScreenEffects.PlayLinearFlash(Stats.GridPosition, _moveDirection, 30);
        break;

    case SkillType.AreaBreak:
        var area = DigHelper.GetSquareArea(Stats.GridPosition, 15);
        DigHelper.ExecuteDig(Chunks, area);
        Enemies.DamageInArea(area, Stats.EffectiveAttackPower);
        ScreenEffects.PlayShockwave(Stats.GridPosition);
        break;

    case SkillType.InvincibleDash:
        Stats.SetInvincible(3.0f);
        Stats.SetSpeedMultiplier(3.0f, 3.0f);
        ScreenEffects.PlayGoldenAura(3.0f);
        break;

    case SkillType.ScreenAttack:
        Enemies.DamageAllVisible(Stats.EffectiveAttackPower * 5);
        ScreenEffects.PlayScreenFlash();
        break;
}
```

### ParticleManager.cs

```
namespace: CursedBlood.Effects
partial class ParticleManager : Node2D
```

Godot 4 の CPUParticles2D を使用（GPUParticles2Dはモバイルで不安定な場合があるため）。

**パーティクルプール**:
- プール方式で再利用（最大同時50個）
- 各パーティクルはCPUParticles2Dインスタンス

**メソッド**:

`void SpawnDigParticle(Vector2 worldPos, Color blockColor)`:
- 小さな四角形パーティクル8〜12個
- 掘削方向と逆方向に扇状に飛散
- 寿命0.3秒、重力あり（下に落ちる）
- 色はブロック色

`void SpawnEnemyDeathParticle(Vector2 worldPos, Color enemyColor)`:
- 円形に放射状に16個
- 寿命0.5秒
- 色は敵色

`void SpawnItemDropParticle(Vector2 worldPos, Color rarityColor)`:
- 上方向に小さな光の粒が立ち上る（8個）
- 寿命1.0秒
- Rare以上は粒の数が増える（Rare=12, Epic=16, Legendary=24, Cursed=24+赤い粒追加）

`void SpawnBossExplosion(Vector2 worldPos)`:
- 大量パーティクル（50個）が全方向に飛散
- 複数色（赤、橙、黄）
- 寿命1.0秒

**メモリ管理**:
- パーティクルプールは起動時に50個事前生成
- 使い終わったら非表示にしてプールに返却
- プール枯渇時は最古のパーティクルを強制終了して再利用

### ScreenEffects.cs

```
namespace: CursedBlood.Effects
partial class ScreenEffects : CanvasLayer
```

CanvasLayer（Layer=15、HUDの上、オーバーレイの下）。

**エフェクト**:

`void PlayScreenFlash(Color color, float duration = 0.15f)`:
- 全画面ColorRectを指定色で表示→alpha 1.0→0.0にフェード
- スキル発動、レアドロップ、ボス撃破で使用

`void PlayShockwave(Vector2 center)`:
- 中心から広がる円を_Drawで描画
- 半径0→400pxまで0.5秒で拡大、同時にalpha 1.0→0.0

`void PlayLinearFlash(Vector2 from, Vector2I direction, int length)`:
- 直線上を白い光が0.2秒で走る

`void PlayGoldenAura(float duration)`:
- プレイヤー位置に金色の円を描画し続ける（duration秒間）

`void PlaySlowMotion(float duration = 1.0f, float timeScale = 0.3f)`:
- `Engine.TimeScale = timeScale` で実装
- duration秒後に1.0に戻す
- 死亡演出で使用

`void PlayBlackFog(float intensity)`:
- 画面四辺から黒い半透明のグラデーションを描画
- intensity 0.0〜1.0 で濃さが変わる
- 晩年期で使用（CurrentAge 46〜60秒で intensity = (CurrentAge - 46) / 14）

`void PlayPhaseTransition(LifePhase newPhase)`:
- 少年期→青年期: 緑→青のフラッシュ + 「成長した！」テキスト一瞬表示
- 青年期→晩年期: 青→オレンジのフラッシュ + 「呪いが侵食する…」テキスト

### GameCamera.cs変更

**画面揺れの実装**:
```csharp
private float _shakeTimer = 0f;
private float _shakeIntensity = 0f;
private Vector2 _basePosition;

public void Shake(float intensity = 5f, float duration = 0.2f)
{
    _shakeIntensity = intensity;
    _shakeTimer = duration;
}

public override void _Process(double delta)
{
    // 通常追従
    _basePosition = _basePosition.Lerp(targetPos, (float)delta * 10f);

    // 揺れ
    if (_shakeTimer > 0f)
    {
        _shakeTimer -= (float)delta;
        var offset = new Vector2(
            (float)GD.RandRange(-_shakeIntensity, _shakeIntensity),
            (float)GD.RandRange(-_shakeIntensity, _shakeIntensity)
        );
        Position = _basePosition + offset;
    }
    else
    {
        Position = _basePosition;
    }
}
```

### AudioManager.cs

```
namespace: CursedBlood.Audio
partial class AudioManager : Node
```

シングルトン（Autoload推奨だが、GameManagerから手動管理でも可）。

**フィールド**:
- `AudioStreamPlayer _bgmPlayer` — BGM再生用
- `AudioStreamPlayer _sePlayer` — SE再生用（同時再生のため複数インスタンスをプール）
- `List<AudioStreamPlayer> _sePool` — SE用プール（8個）
- `float BgmVolume = 0.8f`
- `float SeVolume = 1.0f`

**リソースパス定義**:
```csharp
public static class AudioPaths
{
    // BGM
    public const string BgmYouth = "res://Assets/Audio/BGM/bgm_youth.ogg";
    public const string BgmPrime = "res://Assets/Audio/BGM/bgm_prime.ogg";
    public const string BgmTwilight = "res://Assets/Audio/BGM/bgm_twilight.ogg";
    public const string BgmBoss = "res://Assets/Audio/BGM/bgm_boss.ogg";
    public const string BgmDemonLord = "res://Assets/Audio/BGM/bgm_demon_lord.ogg";
    public const string BgmTitle = "res://Assets/Audio/BGM/bgm_title.ogg";
    public const string BgmResult = "res://Assets/Audio/BGM/bgm_result.ogg";
    public const string BgmEnding = "res://Assets/Audio/BGM/bgm_ending.ogg";

    // SE
    public const string SeDig = "res://Assets/Audio/SE/se_dig.ogg";
    public const string SeDigHard = "res://Assets/Audio/SE/se_dig_hard.ogg";
    public const string SeEnemyHit = "res://Assets/Audio/SE/se_enemy_hit.ogg";
    public const string SePlayerHit = "res://Assets/Audio/SE/se_player_hit.ogg";
    public const string SeItemDrop = "res://Assets/Audio/SE/se_item_drop.ogg";
    public const string SeItemPickup = "res://Assets/Audio/SE/se_item_pickup.ogg";
    public const string SeItemRare = "res://Assets/Audio/SE/se_item_rare.ogg";
    public const string SeEquip = "res://Assets/Audio/SE/se_equip.ogg";
    public const string SeSkill = "res://Assets/Audio/SE/se_skill.ogg";
    public const string SeGuard = "res://Assets/Audio/SE/se_guard.ogg";
    public const string SeBullet = "res://Assets/Audio/SE/se_bullet.ogg";
    public const string SeBomb = "res://Assets/Audio/SE/se_bomb.ogg";
    public const string SeBossAppear = "res://Assets/Audio/SE/se_boss_appear.ogg";
    public const string SeBossDeath = "res://Assets/Audio/SE/se_boss_death.ogg";
    public const string SeCombo = "res://Assets/Audio/SE/se_combo.ogg";
    public const string SeDeath = "res://Assets/Audio/SE/se_death.ogg";
    public const string SeGeneration = "res://Assets/Audio/SE/se_generation.ogg";
    public const string SeAchievement = "res://Assets/Audio/SE/se_achievement.ogg";
    public const string SeDebtPay = "res://Assets/Audio/SE/se_debt_pay.ogg";
    public const string SeMenu = "res://Assets/Audio/SE/se_menu.ogg";
}
```

**メソッド**:

`void PlayBgm(string path)`:
- 現在のBGMをフェードアウト（0.5秒）→ 新BGMをフェードイン（0.5秒）
- 同じ曲が指定された場合は何もしない

`void StopBgm(float fadeOut = 0.5f)`

`void PlaySe(string path)`:
- SEプールから空きプレイヤーを取得して再生
- 全プレイヤーが使用中の場合は最古のものを停止して再利用

`void SetBgmVolume(float volume)` / `void SetSeVolume(float volume)`:
- 0.0〜1.0。AudioServer.SetBusVolumeDb で設定

**BGM自動切り替え**:
GameManagerの_Process内で、フェーズと状況に応じてBGMを切り替える。
```csharp
string targetBgm = _state switch {
    GameState.Title => AudioPaths.BgmTitle,
    GameState.Playing when _bossActive => AudioPaths.BgmBoss,
    GameState.Playing when _demonLordActive => AudioPaths.BgmDemonLord,
    GameState.Playing => _playerStats.Phase switch {
        LifePhase.Youth => AudioPaths.BgmYouth,
        LifePhase.Prime => AudioPaths.BgmPrime,
        LifePhase.Twilight => AudioPaths.BgmTwilight,
        _ => AudioPaths.BgmPrime
    },
    GameState.Dead => AudioPaths.BgmResult,
    GameState.Ending => AudioPaths.BgmEnding,
    _ => null // ポーズ中等はBGM継続
};
if (targetBgm != null) _audioManager.PlayBgm(targetBgm);
```

**音声ファイルが存在しない場合**:
- エラーにせず、ログ出力のみで無音再生
- 開発中は音声ファイルなしで進められるようにする

### BalanceConfig.cs

```
namespace: CursedBlood.Config
class BalanceConfig
```

`user://balance.json`（存在しなければ`res://Assets/default_balance.json`からコピー）を読み込み、全パラメータを公開する。

**構造**:
```csharp
public class BalanceConfig
{
    // プレイヤー
    public float BaseMoveInterval { get; set; } = 0.02f;
    public float MinMoveInterval { get; set; } = 0.005f;
    public int BaseHp { get; set; } = 100;
    public int PlayerSizeYouth { get; set; } = 3;
    public int PlayerSizePrime { get; set; } = 5;
    public int BaseDigWidth { get; set; } = 5;
    public int MaxDigWidth { get; set; } = 15;

    // 地形
    public float DirtHardness { get; set; } = 1.0f;
    public float StoneHardness { get; set; } = 2.0f;
    public float HardRockHardness { get; set; } = 4.0f;
    public List<DepthTierConfig> DepthTiers { get; set; }

    // 装備
    public Dictionary<string, float> RarityMultipliers { get; set; }
    public Dictionary<string, float> DropRates { get; set; }

    // 寿命
    public float BaseLifespanSeconds { get; set; } = 60f;
    public float SecondsPerBonusYear { get; set; } = 1.714f;

    // 借金
    public long InitialDebt { get; set; } = 100000;
    public float InterestRate { get; set; } = 0.10f;

    // 敵
    public List<EnemySpawnConfig> EnemySpawnRates { get; set; }

    // ボス
    public int BossIntervalMeters { get; set; } = 1000;
    public int BossBaseHp { get; set; } = 500;
    public float BossHpScaling { get; set; } = 1.5f;

    // スキル
    public float SkillGaugePerKill { get; set; } = 20f;
    public float SkillGaugePerDig { get; set; } = 1f;
    public float SkillGaugePerBossHit { get; set; } = 5f;
}

public class DepthTierConfig
{
    public int MaxDepth { get; set; }
    public float Dirt { get; set; }
    public float Stone { get; set; }
    public float HardRock { get; set; }
    public float Bedrock { get; set; }
    public float Empty { get; set; }
}
```

**読み込みメソッド**:
```csharp
public static BalanceConfig Load()
{
    string userPath = "user://balance.json";
    string defaultPath = "res://Assets/default_balance.json";

    if (FileAccess.FileExists(userPath))
        return JsonSerializer.Deserialize<BalanceConfig>(ReadFile(userPath));
    if (FileAccess.FileExists(defaultPath))
        return JsonSerializer.Deserialize<BalanceConfig>(ReadFile(defaultPath));

    GD.PrintErr("No balance config found, using hardcoded defaults");
    return new BalanceConfig();
}
```

**使用方法**:
GameManagerの_ReadyでBalanceConfig.Load()を呼び、各マネージャーに参照を渡す。TerrainGenerator, EnemyManager, PlayerStats等が参照する。

### TitleScreen.cs

```
namespace: CursedBlood.UI
partial class TitleScreen : CanvasLayer
```

CanvasLayer（Layer=50）。

**レイアウト**:
```
┌──────────────────────────────────────┐
│                                      │
│                                      │
│                                      │
│         CURSED BLOOD                 │  y: 500 （タイトルロゴ or テキスト）
│                                      │
│    〜呪われし血脈の物語〜           │  y: 620
│                                      │
│                                      │
│         [タップでスタート]           │  y: 900 （点滅テキスト）
│                                      │
│                                      │
│                                      │
│  [家系図]  [実績]  [ランキング]      │  y: 1400
│                                      │
│           [設定]                     │  y: 1550
│                                      │
│                                      │
│  第X世代  借金残高: XXX,XXXG        │  y: 1750 （現在の状態サマリー）
│                                      │
└──────────────────────────────────────┘
```

**シグナル**: `StartGame`, `OpenFamilyTree`, `OpenAchievements`, `OpenRanking`, `OpenSettings`

### PauseMenu.cs

```
namespace: CursedBlood.UI
partial class PauseMenu : CanvasLayer
```

CanvasLayer（Layer=30）。

**レイアウト**:
```
┌──────────────────────────────────────┐
│ [半透明黒背景]                       │
│                                      │
│         ─── PAUSE ───               │  y: 500
│                                      │
│         [続ける]                     │  y: 700
│         [装備]                       │  y: 820
│         [家系図]                     │  y: 940
│         [実績]                       │  y: 1060
│         [設定]                       │  y: 1180
│         [タイトルへ]                 │  y: 1300
│                                      │
└──────────────────────────────────────┘
```

各ボタンは幅600px × 高さ100px。中央揃え。
「タイトルへ」は確認ダイアログを表示（「現在のプレイデータは失われます。よろしいですか？」→ はい/いいえ）。

**シグナル**: `ResumeRequested`, `RequestEquipmentScreen`, `RequestFamilyTree`, `RequestAchievements`, `RequestSettings`, `RequestTitle`

### SettingsUI.cs

```
namespace: CursedBlood.UI
partial class SettingsUI : CanvasLayer
```

CanvasLayer（Layer=40）。

**レイアウト**:
```
┌──────────────────────────────────────┐
│ [×閉じる]              設定          │
├──────────────────────────────────────┤
│                                      │
│  BGM音量    [━━━━━━●━━━]  80%     │  y: 300
│                                      │
│  SE音量     [━━━━━━━━━●]  100%    │  y: 450
│                                      │
│  振動       [ON] / OFF               │  y: 600
│                                      │
│  ─────────────────────────────      │
│                                      │
│  [データリセット]                    │  y: 900 （赤ボタン、確認ダイアログ付き）
│                                      │
└──────────────────────────────────────┘
```

- スライダーはHSlider（Godot Control）で実装
- 値変更時にAudioManager.SetBgmVolume/SetSeVolume即時反映
- 設定値はSaveManager経由で永続保存
- データリセット: 確認ダイアログ → SaveManager.DeleteAll() → アプリ再起動

**シグナル**: `CloseRequested`

### TutorialOverlay.cs

```
namespace: CursedBlood.UI
partial class TutorialOverlay : CanvasLayer
```

CanvasLayer（Layer=60）。初回プレイ時のみ表示。

**表示条件**: SaveData.PlayStats.TotalGenerations == 0

**内容（3ページ、タップで進む）**:

Page 1:
```
┌──────────────────────────────────┐
│ [半透明黒背景]                   │
│                                  │
│  ▲ スワイプで8方向に移動        │
│  ← →  地面を自動で掘り進みます  │
│  ▼                              │
│                                  │
│  [画面中央にスワイプアニメ]      │
│                                  │
│        タップで次へ (1/3)        │
└──────────────────────────────────┘
```

Page 2:
```
┌──────────────────────────────────┐
│                                  │
│  ■ 茶色 = 土（すぐ掘れる）      │
│  ■ 灰色 = 石（遅くなる）        │
│  ■ 黒色 = 壁（通れない）        │
│                                  │
│  長押しでガード！弾を防げます    │
│                                  │
│        タップで次へ (2/3)        │
└──────────────────────────────────┘
```

Page 3:
```
┌──────────────────────────────────┐
│                                  │
│  あなたの一族は呪われている      │
│  35歳で命が尽きる               │
│                                  │
│  60秒の命を掘り進め              │
│  遺品を次世代に託すのだ          │
│                                  │
│        タップでスタート (3/3)    │
└──────────────────────────────────┘
```

**シグナル**: `TutorialCompleted`

完了時に SaveData に `TutorialShown = true` を保存。

### 男女キャラ切替

PlayerStatsに以下を追加:
```csharp
public bool IsMale { get; set; }

// 描画色
public Color PlayerColor => IsMale
    ? Phase switch {
        LifePhase.Youth => new Color(0.3f, 0.7f, 1.0f),    // 青系
        LifePhase.Prime => new Color(0.2f, 0.5f, 0.9f),
        LifePhase.Twilight => new Color(0.4f, 0.4f, 0.7f),
        _ => Colors.White
    }
    : Phase switch {
        LifePhase.Youth => new Color(1.0f, 0.5f, 0.5f),    // 赤系
        LifePhase.Prime => new Color(0.9f, 0.3f, 0.4f),
        LifePhase.Twilight => new Color(0.7f, 0.3f, 0.4f),
        _ => Colors.White
    };
```

世代交代時に `IsMale = rng.Next(0, 2) == 0` でランダム決定。

### コンボ数字の段階演出

HUDManager内:
```csharp
// コンボ数に応じたフォントサイズと色
(int fontSize, Color color) GetComboStyle(int combo) {
    if (combo >= 100) return (64, new Color(1f, 0.2f, 0.2f));     // 赤、巨大
    if (combo >= 50)  return (48, new Color(1f, 0.6f, 0.1f));     // オレンジ、大
    if (combo >= 10)  return (36, new Color(1f, 1f, 0.2f));       // 黄、中
    return (28, Colors.White);                                      // 白、通常
}
```

コンボ数が増えるたびに数字が一瞬拡大して戻るバウンスアニメーション:
```csharp
var tween = CreateTween();
tween.TweenProperty(comboLabel, "scale", new Vector2(1.3f, 1.3f), 0.05f);
tween.TweenProperty(comboLabel, "scale", Vector2.One, 0.1f);
```

## 実装順序

1. BalanceConfig.cs + default_balance.json
2. SkillData.cs + SkillManager.cs + SkillEffects.cs
3. ParticleManager.cs
4. ScreenEffects.cs
5. AudioManager.cs
6. PlayerController.cs変更（ダブルタップ検出、スキル発動）
7. HUDManager.cs変更（スキルゲージ円形描画、コンボ演出強化）
8. GameCamera.cs変更（画面揺れ実装）
9. TitleScreen.cs + TitleScene.tscn
10. PauseMenu.cs
11. SettingsUI.cs
12. TutorialOverlay.cs
13. PlayerStats.cs変更（男女フラグ、BalanceConfig参照、PlayerColor）
14. ChunkManager.cs変更（BalanceConfig参照）
15. TerrainGenerator.cs変更（BalanceConfig参照）
16. GameManager.cs変更（全統合: スキル、演出、タイトル、ポーズ、設定、チュートリアル、BGM自動切替）
17. SaveData.cs変更（SettingsData, TutorialShownフラグ追加）
18. バランス調整（default_balance.jsonの数値チューニング）
19. メモリリーク確認（100世代連続プレイテスト）
20. Android実機テスト → APKビルド
