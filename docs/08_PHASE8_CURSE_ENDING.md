# Phase 8: 呪い研究＋寿命延長＋魔王＋エンディング

## 前提

- 依存: Phase 7 完了済み
- 参照: `docs/00_PROJECT_SPEC.md`, `docs/11_SAVE_SYSTEM.md`, `docs/12_SCREEN_FLOW.md`

## このPhaseのゴール

呪い研究度を蓄積して寿命を延長し、最深部の魔王に到達してエンディングを迎える。

## 完了条件チェックリスト

- [ ] 呪い研究度がプレイ中に獲得できる
- [ ] 呪い研究度は世代をまたいで累積される（減らない）
- [ ] 累計100ポイントごとに寿命+1歳（+1.714秒）
- [ ] HUDに呪い研究度の進捗が表示される（次の+1歳までのゲージ）
- [ ] 呪い研究度データはSaveManager経由で永続保存
- [ ] 深度99999mに魔王（30×30セル=480×480px）が配置されている
- [ ] 魔王HP: 999,999
- [ ] 魔王の4フェーズ弾幕が動作する
- [ ] 魔王撃破でエンディングシーンが再生される
- [ ] エンディング演出: 呪い解除 → 家系図ダイジェスト → 総合スコア
- [ ] エンディング後も周回プレイ可能
- [ ] エンディング後は魔王の代わりに「真魔王」が出現（HP ×3）

## 作成・変更するファイル

### 新規作成

```
Scripts/CursedBlood/Curse/
  CurseResearchManager.cs    ← 呪い研究度管理・計算
  CurseResearchUI.cs         ← 研究度進捗表示（HUD内の小ゲージ）
Scripts/CursedBlood/Enemy/
  DemonLordData.cs           ← 魔王データ定義
  DemonLordController.cs     ← 魔王行動管理・弾幕パターン
Scenes/CursedBlood/
  EndingScene.tscn            ← エンディングシーン
Scripts/CursedBlood/UI/
  EndingUI.cs                 ← エンディング画面制御
```

### 変更

```
Scripts/CursedBlood/Player/PlayerStats.cs      ← MaxLifespan に呪い研究ボーナス反映
Scripts/CursedBlood/Core/TerrainGenerator.cs   ← 魔王配置
Scripts/CursedBlood/Core/ChunkManager.cs       ← 魔王描画
Scripts/CursedBlood/Core/GameManager.cs        ← 呪い研究統合・エンディング遷移
Scripts/CursedBlood/UI/HUDManager.cs           ← 研究度ゲージ追加
Scripts/CursedBlood/Enemy/BulletManager.cs     ← 追尾弾の実装
Scripts/CursedBlood/Save/SaveData.cs           ← CurseResearchData追加
```

## 詳細設計

### CurseResearchManager.cs

```
namespace: CursedBlood.Curse
class CurseResearchManager
```

**フィールド**:
- `int TotalPoints` — 累積研究度（減らない）
- `bool EndingCleared` — エンディングを1回以上見たか

**算出プロパティ**:
- `int BonusYears` → TotalPoints / 100（端数切捨て）
- `float BonusSeconds` → BonusYears × 1.714f
- `float ExtendedLifespan` → 60.0f + BonusSeconds
- `int PointsToNextYear` → 100 - (TotalPoints % 100)（次の+1歳までの残りポイント）
- `float ProgressToNextYear` → (TotalPoints % 100) / 100.0f（0.0〜0.99）

**獲得手段と獲得量**:

| 行動 | 獲得ポイント | 備考 |
|---|---|---|
| Cursed装備を装備中 | 0.5pt/秒 | プレイ中に毎秒加算 |
| 深度2000m初到達 | 10pt | 1回限り（世代ごとにリセットしない） |
| 深度5000m初到達 | 30pt | 1回限り |
| 深度10000m初到達 | 100pt | 1回限り |
| ボス撃破 | 5pt/体 | 何度でも |
| 「呪い吸収」効果装備中 | 獲得量 ×(1 + CurseAbsorb%) | 他の獲得に乗算 |
| 「呪いコレクター」実績解除済み | 獲得量 ×1.2 | さらに乗算 |

**深度初到達の管理**:
- `HashSet<int> ReachedDepthMilestones` をSaveDataに保存
- マイルストーン: 2000, 5000, 10000（固定3つ）

**メソッド**:

`void AddPoints(float points)`:
- CurseAbsorb効果と実績ボーナスを乗算して加算
- TotalPointsに反映

`void CheckDepthMilestone(int currentDepthMeters)`:
- 未到達のマイルストーンを超えていたらポイント付与＋記録

`void UpdateCursedEquipBonus(float delta, bool hasCursedEquipped)`:
- Cursed装備中なら delta × 0.5pt を加算

`CurseResearchSaveData ToSaveData()` / `void LoadFrom(CurseResearchSaveData data)`:
- SaveManager連携

### CurseResearchUI.cs

```
namespace: CursedBlood.Curse
partial class CurseResearchUI : Control
```

HUDManager内に組み込む小型UIコンポーネント。

**表示位置**: HUDエリア右側（寿命バーの下）、幅200px × 高さ30px

**表示内容**:
- 小さなゲージバー（紫色）: ProgressToNextYear
- テキスト: 「呪い研究 +X歳」（BonusYears表示）

### DemonLordData.cs

```
namespace: CursedBlood.Enemy
class DemonLordData
```

- `int MaxHp = 999999`
- `int CurrentHp`
- `int Size = 30` — 30×30セル（480×480px）
- `int ArenaSize = 60` — 60×60セル（960×960px）
- `Vector2I CenterPosition` — 配置中心座標
- `DemonLordPhase CurrentPhase` — HP割合で自動判定
- `bool IsTrueDemonLord` — エンディング後の真魔王か（HP ×3）

**DemonLordPhase enum**: Phase1, Phase2, Phase3, Phase4

**フェーズ判定**:
```csharp
public DemonLordPhase CurrentPhase {
    get {
        float ratio = (float)CurrentHp / MaxHp;
        if (ratio > 0.7f) return DemonLordPhase.Phase1;
        if (ratio > 0.4f) return DemonLordPhase.Phase2;
        if (ratio > 0.1f) return DemonLordPhase.Phase3;
        return DemonLordPhase.Phase4;
    }
}
```

### DemonLordController.cs

```
namespace: CursedBlood.Enemy
partial class DemonLordController : Node2D
```

**フィールド**:
- `DemonLordData Data`
- `float AttackTimer`
- `float SpecialTimer` — 特殊攻撃用の別タイマー
- `BulletManager BulletMgr` — 弾生成の参照

**弾幕パターン詳細**:

**Phase1（HP100〜70%）: 全方向弾 + 追尾弾**
```
間隔: 3秒
パターン:
  - 全方向弾16発: 中心から22.5度間隔で16方向に弾を1発ずつ
    弾速: 20セル/秒、16セルで消滅
  - 追尾弾2発: 中心からプレイヤー方向に2発（0.5秒ずらし）
    弾速: 15セル/秒、毎セル移動時にプレイヤー方向へ角度修正、20セルで消滅
```

**Phase2（HP70〜40%）: 螺旋弾幕**
```
間隔: 0.2秒（連射）
パターン:
  - 螺旋弾: 発射角度が毎回+15度ずつ回転する単発弾
    弾速: 25セル/秒、20セルで消滅
  - 3秒ごとに回転方向が反転
  実質的に螺旋状の弾幕が形成される
```

**Phase3（HP40〜10%）: 崩落 + 全方向弾 + 追尾弾**
```
間隔:
  - 全方向弾: 2秒ごとに8方向×2発（計16発）
  - 追尾弾: 3秒ごとに3発
  - 崩落: 4秒ごとにランダム5列を選択→1.5秒警告→崩落ダメージ
崩落の列選択範囲: アリーナ内の全列（60列）からランダム5列
崩落ダメージ: 30（プレイヤーのMaxHpの30%）
```

**Phase4（HP10〜0%）: 高速全方向弾 + 全列崩落ラッシュ**
```
間隔:
  - 高速全方向弾: 1秒ごとに全方向24発
    弾速: 35セル/秒（通常の1.75倍）
  - 全列崩落ラッシュ: 2秒ごとにランダム10列を崩落
    警告時間: 1.0秒（Phase3の1.5秒より短い）
  - 追尾弾: 1.5秒ごとに4発
```

**追尾弾の実装**:

BulletManagerに追尾弾タイプを追加。

```csharp
public class BulletData
{
    // ...既存フィールド
    public bool IsHoming;        // 追尾弾か
    public float HomingStrength; // 毎セル移動時の角度修正量（ラジアン）
}
```

追尾弾の移動処理:
```csharp
if (bullet.IsHoming)
{
    Vector2 toPlayer = (playerPos - bulletPos).Normalized();
    Vector2 currentDir = new Vector2(bullet.Direction.X, bullet.Direction.Y).Normalized();
    float angle = currentDir.AngleTo(toPlayer);
    float maxTurn = bullet.HomingStrength; // 0.3ラジアン程度
    angle = Mathf.Clamp(angle, -maxTurn, maxTurn);
    Vector2 newDir = currentDir.Rotated(angle);
    // newDirを最近接の8方向にスナップ
    bullet.Direction = SnapToGrid(newDir);
}
```

**TakeDamage(int damage)**:
- HP減算
- フェーズ遷移時に演出（画面揺れ + 一瞬停止）
- HP0で OnDefeat() 呼び出し

**OnDefeat()**:
1. 弾を全消去
2. 魔王が点滅（0.5秒間、5回）
3. 爆発エフェクト（画面全体フラッシュ）
4. 占有セルを全てEmptyに
5. GameManagerに `DemonLordDefeated` シグナル送信

### EndingUI.cs

```
namespace: CursedBlood.UI
partial class EndingUI : CanvasLayer
```

CanvasLayer（Layer=50）。GameState.Endingのときに表示。

**演出シーケンス**（全自動、各段階の間にフェードトランジション）:

**1. 呪い解除演出（5秒）**
- 画面全体が暗転
- 中央にテキスト:「呪いは解かれた」（1文字ずつ表示、2秒）
- 画面端から黒霧が引いていく演出（3秒）
- 背景が暗色から明るい色にグラデーション遷移

**2. 家系図ダイジェスト（1世代あたり0.5秒、最大30秒）**
- 各世代の名前、深度、スコアが下から上にスクロール
- 各世代表示時にその世代の色（男=青、女=赤）でフラッシュ
- 世代数が60以上の場合は、初代・最終世代・主要マイルストーン世代のみ表示

**3. 総合スタッツ表示（5秒）**
```
┌──────────────────────────────────┐
│                                  │
│      呪われし血脈の記録          │
│                                  │
│  総世代数:        47世代         │
│  総プレイ時間:    2時間34分      │
│  最大深度:        99,999m        │
│  総撃破数:        15,230体       │
│  総獲得ゴールド:  5,842,000G     │
│  最終スコア:      123,456,789    │
│                                  │
│  「しかし地底にはまだ          │
│    秘密が眠っている…」         │
│                                  │
│        [続ける]                  │
│                                  │
└──────────────────────────────────┘
```

**4. 「続ける」ボタン押下**
- エンディングフラグをSaveDataに保存
- タイトル画面へ遷移
- 以降のプレイでは深度99999mに「真魔王」（HP ×3 = 2,999,997）が出現

**シグナル**: `EndingCompleted`

### TerrainGenerator.cs変更

- 深度99999m付近（行番号99999）にDemonLord配置
- 30×30セルのBoss領域 + 60×60セルのアリーナ（Empty化）
- 左右に幅15セルの迂回通路（ただし魔王は迂回しても次プレイで再戦可能）

エンディング後の真魔王フラグは CurseResearchManager.EndingCleared で判定。

### PlayerStats.cs変更

```csharp
public float MaxLifespan {
    get {
        float base_ = 60f;
        float curseBonus = _curseResearchManager?.BonusSeconds ?? 0f;
        return base_ + curseBonus;
    }
}
```

CurseResearchManagerの参照をPlayerStatsに渡す（GameManagerで設定）。

### GameManager.cs変更

**魔王撃破時の遷移**:
```csharp
_demonLordController.DemonLordDefeated += OnDemonLordDefeated;

private void OnDemonLordDefeated()
{
    _curseResearchManager.EndingCleared = true;
    SaveManager.Save();
    TransitionTo(GameState.Ending);
    _endingUI.StartSequence(_familyTree, _playerStats, _curseResearchManager);
}

_endingUI.EndingCompleted += () => {
    // タイトルへ（Phase 9でタイトル実装後。それまではリスタート）
    TransitionTo(GameState.Title);
};
```

## 実装順序

1. CurseResearchManager.cs + SaveData.cs変更
2. CurseResearchUI.cs
3. DemonLordData.cs
4. DemonLordController.cs（4フェーズ弾幕）
5. BulletManager.cs変更（追尾弾追加）
6. EndingUI.cs
7. EndingScene.tscn
8. TerrainGenerator.cs変更（魔王配置）
9. ChunkManager.cs変更（魔王描画）
10. PlayerStats.cs変更（寿命延長反映）
11. HUDManager.cs変更（研究度ゲージ）
12. GameManager.cs変更（呪い研究統合 + エンディング遷移 + 真魔王フラグ）
13. 動作確認 → APKビルド
