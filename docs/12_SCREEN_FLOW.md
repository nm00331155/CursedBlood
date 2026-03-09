# 画面遷移フロー

## 概要

CursedBloodの全画面とその遷移を定義する。Copilotが画面遷移を実装する際はこのドキュメントを参照すること。

## 画面一覧

| 画面ID | 画面名 | 実装Phase | 実装方式 |
|---|---|---|---|
| TITLE | タイトル画面 | Phase 9 | シーン切替 |
| GAME | ゲームプレイ画面 | Phase 1 | メインシーン |
| HUD | HUDオーバーレイ | Phase 1 | CanvasLayer（GAME上に常時表示） |
| DEATH | 死亡リザルト | Phase 1 | CanvasLayer（GAME上にオーバーレイ） |
| DEBT_REPAY | 借金返済選択 | Phase 5 | CanvasLayer（DEATH内のサブ画面） |
| HEIRLOOM | 遺品選択 | Phase 4 | CanvasLayer（DEATH内のサブ画面） |
| EQUIP | 装備画面 | Phase 3 | CanvasLayer（GAME上にオーバーレイ） |
| ITEM_COMPARE | アイテム比較 | Phase 3 | CanvasLayer（EQUIP内ポップアップ） |
| FAMILY_TREE | 家系図 | Phase 4 | CanvasLayer（GAME上にオーバーレイ） |
| ACHIEVEMENT | 実績一覧 | Phase 7 | CanvasLayer（GAME上にオーバーレイ） |
| RANKING | ランキング | Phase 7 | CanvasLayer（GAME上にオーバーレイ） |
| PAUSE | ポーズメニュー | Phase 9 | CanvasLayer（GAME上にオーバーレイ） |
| SETTINGS | 設定 | Phase 9 | CanvasLayer（PAUSE内のサブ画面） |
| TUTORIAL | チュートリアル | Phase 9 | CanvasLayer（GAME上にオーバーレイ） |
| ENDING | エンディング | Phase 8 | シーン切替 |

## 画面遷移図（テキスト表記）

```
[アプリ起動]
    │
    ▼
[TITLE] タイトル画面
    │  タップ → ゲーム開始
    │  家系図ボタン → [FAMILY_TREE]
    │  実績ボタン → [ACHIEVEMENT]
    │  ランキングボタン → [RANKING]
    │  設定ボタン → [SETTINGS]
    │
    ▼  ゲーム開始
[GAME] + [HUD] ゲームプレイ画面
    │
    ├── プレイ中の操作 ──────────────────────────────
    │   │
    │   ├── スワイプ上（またはポーズボタン） → [PAUSE]
    │   │       ├── 「続ける」 → [GAME]に戻る
    │   │       ├── 「装備」 → [EQUIP]
    │   │       ├── 「家系図」 → [FAMILY_TREE]
    │   │       ├── 「実績」 → [ACHIEVEMENT]
    │   │       ├── 「設定」 → [SETTINGS]
    │   │       └── 「タイトルへ」 → [TITLE]（現在のプレイは破棄）
    │   │
    │   ├── 下部の装備アイコンタップ → [EQUIP]
    │   │       ├── アイテムタップ → [ITEM_COMPARE]
    │   │       │       ├── 「装備する」 → 装備変更 → [EQUIP]に戻る
    │   │       │       ├── 「捨てる」 → 削除確認 → [EQUIP]に戻る
    │   │       │       └── 「戻る」 → [EQUIP]に戻る
    │   │       └── 「閉じる」 → [GAME]に戻る
    │   │
    │   ├── バッグ満杯でアイテム拾得 → [ITEM_COMPARE]
    │   │       ├── 「入れ替え」 → 既存アイテム選択 → 交換 → [GAME]に戻る
    │   │       └── 「捨てる」 → 新アイテム破棄 → [GAME]に戻る
    │   │
    │   └── 実績解除時 → [HUD]上に通知ポップアップ（3秒で自動消滅、タップで即消滅）
    │
    ▼  死亡（寿命 or HP0）
[DEATH] 死亡リザルト画面
    │
    │  ステップ1: リザルト表示（統計情報）
    │  「次へ」タップ
    │
    ▼
[DEBT_REPAY] 借金返済選択（Phase 5以降。それ以前はスキップ）
    │
    │  4択から選択: 全額/半額/最低/しない
    │  選択後
    │
    ▼
[HEIRLOOM] 遺品選択（Phase 4以降。それ以前はスキップ）
    │
    │  装備一覧から1つ選択 or 「何も残さない」
    │  選択後
    │
    ▼
[GAME] + [HUD] 次世代でゲーム再開
    │
    │  （通常のプレイループに戻る）
    │
    ...

[魔王撃破時]（Phase 8）
    │
    ▼
[ENDING] エンディング画面
    │
    │  演出再生 → 家系図ダイジェスト → 総合スコア
    │  「続ける」ボタン
    │
    ▼
[TITLE] タイトル画面（周回プレイ可能）
```

## GameState と画面の対応

```
namespace: CursedBlood.Core
enum GameState
```

| GameState | 表示画面 | ゲーム時間 | 入力受付 |
|---|---|---|---|
| Title | TITLE | 停止 | タイトルUIのみ |
| Playing | GAME + HUD | 進行 | ゲーム操作（スワイプ、ガード、スキル） |
| Paused | GAME + HUD + PAUSE | 停止 | ポーズUIのみ |
| Equipment | GAME + HUD + EQUIP | 停止 | 装備UIのみ |
| ItemCompare | GAME + HUD + ITEM_COMPARE | 停止 | 比較UIのみ |
| Dead | GAME + HUD + DEATH | 停止 | リザルトUI |
| DebtRepay | GAME + HUD + DEBT_REPAY | 停止 | 返済UI |
| HeirloomSelect | GAME + HUD + HEIRLOOM | 停止 | 遺品選択UI |
| ViewingFamilyTree | (前画面) + FAMILY_TREE | 停止 | 家系図UI |
| ViewingAchievement | (前画面) + ACHIEVEMENT | 停止 | 実績UI |
| ViewingRanking | (前画面) + RANKING | 停止 | ランキングUI |
| Settings | (前画面) + SETTINGS | 停止 | 設定UI |
| Tutorial | GAME + HUD + TUTORIAL | 停止 | チュートリアルUI |
| Ending | ENDING | 停止 | エンディングUI |

「前画面」は遷移元によって変わる（GAME+HUD or PAUSE or TITLE）。

## 遷移管理の実装方針

### CanvasLayerの管理

全オーバーレイ画面はCanvasLayerとして実装し、GameManagerが参照を保持する。画面の表示/非表示は各画面の `Show()` / `Hide()` メソッドで制御する。

```csharp
// GameManager のフィールド
private HUDManager _hud;
private DeathScreen _deathScreen;
private DebtUI _debtUI;           // Phase 5
private HeirloomUI _heirloomUI;   // Phase 4
private EquipmentUI _equipmentUI; // Phase 3
private FamilyTreeUI _familyTreeUI; // Phase 4
private AchievementUI _achievementUI; // Phase 7
private RankingUI _rankingUI;     // Phase 7
private PauseMenu _pauseMenu;    // Phase 9
private SettingsUI _settingsUI;  // Phase 9
private TutorialOverlay _tutorial; // Phase 9
```

### 状態遷移メソッド

GameManagerに画面遷移メソッドを集約する。

```csharp
public void TransitionTo(GameState newState)
{
    // 1. 現在の状態に応じた画面を閉じる
    // 2. 新状態に応じた画面を開く
    // 3. _state を更新
    // 4. ゲーム時間の停止/再開
}
```

全ての画面遷移はこのメソッドを経由する。各UI画面はGameManagerへの直接参照を持たず、シグナル（イベント）で遷移要求を通知する。

```csharp
// 例: PauseMenu から装備画面を開く
// PauseMenu.cs
[Signal] public delegate void RequestEquipmentScreenEventHandler();

// ボタン押下時
EmitSignal(SignalName.RequestEquipmentScreen);

// GameManager.cs（接続）
_pauseMenu.RequestEquipmentScreen += () => TransitionTo(GameState.Equipment);
```

### 戻る操作の管理

オーバーレイ画面は「戻る」ボタンまたはAndroidのバックキーで閉じる。戻り先を管理するために簡易的な画面スタックを使用する。

```csharp
private Stack<GameState> _stateStack = new();

public void PushState(GameState newState)
{
    _stateStack.Push(_state);
    TransitionTo(newState);
}

public void PopState()
{
    if (_stateStack.Count > 0)
    {
        TransitionTo(_stateStack.Pop());
    }
}
```

これにより「タイトル→家系図→戻る→タイトル」と「ゲーム中→ポーズ→家系図→戻る→ポーズ」の両方が正しく動作する。

### Androidバックキー対応

```csharp
public override void _UnhandledInput(InputEvent @event)
{
    if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Back)
    {
        if (_state == GameState.Playing)
        {
            PushState(GameState.Paused);
        }
        else if (_state != GameState.Title)
        {
            PopState();
        }
        GetViewport().SetInputAsHandled();
    }
}
```

## 死亡→次世代の遷移フロー詳細

最も複雑な画面遷移チェーンを詳細に記述する。

```
1. GameState.Playing
   │ プレイヤー死亡検出（IsAlive == false）
   │
2. GameState.Dead
   │ DeathScreen.Show() → リザルト表示
   │ ユーザーが「次へ」タップ
   │
3. GameState.DebtRepay（Phase 5以降）
   │ DebtUI.Show() → 4択表示
   │ ユーザーが選択 → DebtManager.Repay()
   │ DebtUI が DebtRepayCompleted シグナル発行
   │   ※Phase 4以前はこのステップをスキップ
   │
4. GameState.HeirloomSelect（Phase 4以降）
   │ HeirloomUI.Show(inventory) → 装備一覧表示
   │ ユーザーが遺品を選択 or 「何も残さない」
   │ HeirloomUI が HeirloomSelected(EquipmentData?) シグナル発行
   │   ※Phase 3以前はこのステップをスキップ
   │
5. 継承処理（GameManager内）
   │ GenerationManager.ProcessInheritance()
   │ FamilyTree.AddRecord()
   │ SaveManager.Save()
   │ PlayerStats.Reset()
   │ GridManager.Reset()
   │ PlayerController.Reset()
   │
6. GameState.Playing
   │ 次世代開始
```

各ステップ間の遷移はシグナルチェーンで接続する。

```csharp
// GameManager内の接続
_deathScreen.NextRequested += OnDeathNextRequested;

private void OnDeathNextRequested()
{
    if (_debtManager != null && _debtManager.RemainingDebt > 0)
    {
        TransitionTo(GameState.DebtRepay);
    }
    else if (_inventory != null)
    {
        TransitionTo(GameState.HeirloomSelect);
    }
    else
    {
        StartNextGeneration(null, null);
    }
}

_debtUI.DebtRepayCompleted += OnDebtRepayCompleted;

private void OnDebtRepayCompleted(long repaidAmount)
{
    if (_inventory != null)
    {
        TransitionTo(GameState.HeirloomSelect);
    }
    else
    {
        StartNextGeneration(null, repaidAmount);
    }
}

_heirloomUI.HeirloomSelected += OnHeirloomSelected;

private void OnHeirloomSelected(EquipmentData heirloom)
{
    StartNextGeneration(heirloom, _lastRepaidAmount);
}
```

## Phase別の画面遷移対応表

各Phaseで実装すべき画面遷移を明示する。

### Phase 1

| 遷移 | 実装 |
|---|---|
| Playing → Dead | 寿命/HP0で自動遷移 |
| Dead → Playing | タップでリスタート |

GameState: Playing, Dead の2状態のみ。

### Phase 2

Phase 1と同じ。追加の画面遷移なし。

### Phase 3

| 遷移 | 実装 |
|---|---|
| Playing → Equipment | 下部アイコンタップ |
| Equipment → Playing | 閉じるボタン |
| Playing → ItemCompare | バッグ満杯で拾得時 |
| ItemCompare → Playing | 選択完了 |
| Equipment → ItemCompare | アイテムタップ |
| ItemCompare → Equipment | 戻るボタン |

GameState に Equipment, ItemCompare を追加。

### Phase 4

| 遷移 | 実装 |
|---|---|
| Dead → HeirloomSelect | リザルトの「次へ」タップ |
| HeirloomSelect → Playing | 遺品選択完了 |
| Playing → ViewingFamilyTree | 家系図ボタンタップ |
| ViewingFamilyTree → Playing | 閉じるボタン |

GameState に HeirloomSelect, ViewingFamilyTree を追加。
DeathScreen に「次へ」ボタンを追加（タップ即リスタートから変更）。

### Phase 5

| 遷移 | 実装 |
|---|---|
| Dead → DebtRepay | リザルトの「次へ」タップ |
| DebtRepay → HeirloomSelect | 返済選択完了 |

GameState に DebtRepay を追加。
死亡フローが Dead → DebtRepay → HeirloomSelect → Playing に拡張。

### Phase 6

追加の画面遷移なし。ボス戦はGAME画面内で処理。ボスHP表示はHUDに追加。

### Phase 7

| 遷移 | 実装 |
|---|---|
| Playing → ViewingAchievement | 実績ボタンタップ |
| ViewingAchievement → Playing | 閉じるボタン |
| Playing → ViewingRanking | ランキングボタンタップ |
| ViewingRanking → Playing | 閉じるボタン |

GameState に ViewingAchievement, ViewingRanking を追加。

### Phase 8

| 遷移 | 実装 |
|---|---|
| Playing → Ending | 魔王撃破時 |
| Ending → Title | 「続ける」ボタン |

GameState に Ending を追加。

### Phase 9

| 遷移 | 実装 |
|---|---|
| アプリ起動 → Title | 初期画面 |
| Title → Playing | タップでゲーム開始 |
| Title → ViewingFamilyTree | 家系図ボタン |
| Title → ViewingAchievement | 実績ボタン |
| Title → ViewingRanking | ランキングボタン |
| Title → Settings | 設定ボタン |
| Playing → Paused | スワイプアップ/ポーズボタン |
| Paused → Playing | 「続ける」ボタン |
| Paused → Equipment | 「装備」ボタン |
| Paused → ViewingFamilyTree | 「家系図」ボタン |
| Paused → Settings | 「設定」ボタン |
| Paused → Title | 「タイトルへ」ボタン |
| Playing → Tutorial | 初回プレイ時自動 |
| Tutorial → Playing | チュートリアル完了 |

GameState に Title, Paused, Settings, Tutorial を追加。
main_scene を TitleScene.tscn に変更（Phase 9完了後）。

## CanvasLayer の Layer 順序

同時に複数のCanvasLayerが表示される場合の重なり順を定義する。

| Layer値 | 画面 | 説明 |
|---|---|---|
| 0 | (デフォルト) | GAME画面のNode2D |
| 10 | HUD | 常時表示 |
| 20 | EQUIP / FAMILY_TREE / ACHIEVEMENT / RANKING | オーバーレイ画面 |
| 30 | PAUSE | ポーズメニュー（オーバーレイの上） |
| 40 | ITEM_COMPARE / SETTINGS | ポップアップ（さらに上） |
| 50 | DEATH / DEBT_REPAY / HEIRLOOM | 死亡フロー（最優先） |
| 60 | TUTORIAL | チュートリアル（最前面） |
| 70 | ACHIEVEMENT_POPUP | 実績解除通知（絶対最前面） |

各CanvasLayerの `Layer` プロパティにこの値を設定する。
Copy
これで全ドキュメントが揃いました。最終的なdocs/フォルダの構成は以下の通りです。

docs/
├── 00_PROJECT_SPEC.md          ← プロジェクト全体仕様
├── 01_PHASE1_GRID_MOVEMENT.md  ← Phase 1: グリッド＋移動＋掘削
├── 02_PHASE2_COMBAT.md         ← Phase 2: 敵＋弾＋ガード＋ゴールド
├── 03_PHASE3_EQUIPMENT.md      ← Phase 3: 装備＋レアリティ＋付与効果
├── 04_PHASE4_GENERATION.md     ← Phase 4: 世代継承＋リザルト＋家系図
├── 05_PHASE5_DEBT.md           ← Phase 5: 借金システム
├── 06_PHASE6_BOSS.md           ← Phase 6: ボス＋弾幕＋アリーナ
├── 07_PHASE7_ACHIEVEMENT.md    ← Phase 7: 実績＋ランキング
├── 08_PHASE8_CURSE_ENDING.md   ← Phase 8: 呪い研究＋エンディング
├── 09_PHASE9_POLISH.md         ← Phase 9: スキル＋演出＋仕上げ
├── 10_ASSET_PIPELINE.md        ← アセット管理・差し替え手順
├── 11_SAVE_SYSTEM.md           ← セーブデータ統合管理
└── 12_SCREEN_FLOW.md           ← 画面遷移フロー