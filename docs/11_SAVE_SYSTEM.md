# セーブデータ統合管理システム

## 概要

Phase 4〜8で個別に実装される永続データ（家系図、借金、実績、呪い研究度、ランキング、設定）を統合管理するセーブシステムを定義する。本ドキュメントは全Phaseを通じて参照し、各Phaseの永続化処理はここで定義する統一インターフェースに従う。

## 統合セーブ構造

全永続データを1つのマスターセーブファイルで管理する。

```
user://savedata/
├── save_master.json         ← マスターセーブ（メタ情報＋全データ統合）
├── save_master.json.bak     ← 直前のバックアップ
└── save_master.json.bak2    ← 2世代前のバックアップ
```

### save_master.json の構造

```json
{
  "version": 1,
  "last_saved": "2026-03-09T12:34:56Z",
  "play_stats": {
    "total_play_time_seconds": 3600.0,
    "total_generations": 47,
    "total_enemies_killed": 1523,
    "total_gold_earned": 584200
  },
  "current_run": {
    "generation": 48,
    "player_name": "深掘 ハヤト",
    "is_male": true,
    "inherited_gold": 2400,
    "inherited_equipment": null
  },
  "family_tree": {
    "records": [
      {
        "generation": 1,
        "name": "深掘 タケル",
        "is_male": true,
        "max_depth": 47,
        "death_cause": "寿命が尽きた",
        "weapon_name": "木のツルハシ",
        "score": 1410,
        "enemies_killed": 12,
        "human_age": 35,
        "gold_earned": 850,
        "gold_inherited": 0
      }
    ]
  },
  "debt": {
    "total_debt": 100000,
    "paid_total": 15000,
    "liberation_bonus_active": false
  },
  "achievements": {
    "unlocked": ["first_dig", "combo_master"],
    "progress": {
      "ore_collector": 0.45,
      "boss_hunter": 0.0
    },
    "cumulative_stats": {
      "total_ore_broken": 45,
      "total_hard_broken": 230,
      "total_equipment_found": 28,
      "total_rare_plus_found": 5,
      "total_legendary_found": 0,
      "total_cursed_found": 0,
      "total_guards": 67,
      "total_gold_inherited": 12500
    }
  },
  "curse_research": {
    "total_points": 85,
    "bonus_years": 0,
    "bonus_seconds": 0.0
  },
  "rankings": {
    "max_depth_top10": [
      {"name": "深掘 タケル", "generation": 1, "value": 47}
    ],
    "max_score_top10": [
      {"name": "深掘 タケル", "generation": 1, "value": 1410}
    ],
    "fastest_depth100": []
  },
  "settings": {
    "bgm_volume": 0.8,
    "se_volume": 1.0,
    "vibration": true,
    "theme": "dark"
  }
}
```

## 作成するファイル

```
Scripts/CursedBlood/Save/
  SaveManager.cs             ← 統合セーブ管理
  SaveData.cs                ← セーブデータ構造体定義
  SaveMigrator.cs            ← バージョン移行処理
```

## 詳細設計

### SaveData.cs

```
namespace: CursedBlood.Save
```

JSONシリアライズ対象の全データクラスを定義する。

**SaveData class（ルート）**:
- `int Version` — スキーマバージョン（初期値1）
- `string LastSaved` — ISO 8601形式の保存日時
- `PlayStatsData PlayStats`
- `CurrentRunData CurrentRun`
- `FamilyTreeData FamilyTree`
- `DebtData Debt`
- `AchievementSaveData Achievements`
- `CurseResearchData CurseResearch`
- `RankingsData Rankings`
- `SettingsData Settings`

各サブクラスはsave_master.jsonの構造に対応する。全プロパティにデフォルト値を設定し、新規開始時にnullが発生しないようにする。

### SaveManager.cs

```
namespace: CursedBlood.Save
class SaveManager
```

シングルトンパターンで実装する。GameManagerの_Readyで初期化する。

**定数**:
- `SavePath = "user://savedata/save_master.json"`
- `BackupPath = "user://savedata/save_master.json.bak"`
- `Backup2Path = "user://savedata/save_master.json.bak2"`
- `AutoSaveIntervalSeconds = 30.0f`

**フィールド**:
- `SaveData Data` — 現在のセーブデータ（メモリ上）
- `float _autoSaveTimer`

**メソッド**:

`void Initialize()`:
1. user://savedata/ ディレクトリが無ければ作成
2. save_master.json が存在すれば Load()
3. 存在しなければ新規SaveDataを生成して Save()

`void Load()`:
1. save_master.json を読み込み
2. JSONデシリアライズ
3. Version チェック → 必要なら SaveMigrator.Migrate() でマイグレーション
4. 読み込み失敗時は .bak → .bak2 の順でフォールバック
5. 全バックアップが読めない場合は新規データ生成＋エラーログ出力

`void Save()`:
1. 現在の .json を .bak2 にリネーム（既存の.bak2は上書き）
2. 現在の .bak を .bak2 にリネーム
3. 現在の .json を .bak にリネーム
4. Data をJSONシリアライズ → .json に書き込み
5. LastSaved を現在時刻に更新
6. 書き込み後にファイルの存在・サイズ0チェック → 異常なら .bak から復元

`void AutoSave(float delta)`:
- _autoSaveTimerを加算し、AutoSaveIntervalSeconds超過でSave()を呼ぶ
- GameManagerの_Process内から呼ばれる

`void DeleteAll()`:
- 全セーブファイルを削除（デバッグ用・設定画面の「データリセット」用）

**JSONシリアライズ**:
- `System.Text.Json.JsonSerializer` を使用
- オプション: `WriteIndented = true`（デバッグ読みやすさ）, `PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower`
- Godot固有型（Vector2I等）はstring変換のカスタムコンバーターを実装

### SaveMigrator.cs

```
namespace: CursedBlood.Save
static class SaveMigrator
```

- `SaveData Migrate(SaveData data, int fromVersion, int toVersion)`
- バージョン1→2、2→3等の段階的マイグレーションをチェーンで実行
- 新フィールドにはデフォルト値を設定
- 既存データは破壊しない

## 各Phaseでの統合方法

各Phaseの個別マネージャー（FamilyTree, DebtManager, AchievementManager, CurseResearchManager, RankingManager）は、自前でファイルI/Oを行わない。代わりに以下のパターンを使用する。

**読み込み時**:
```csharp
// GameManager._Ready() 内
SaveManager.Initialize();
_familyTree.LoadFrom(SaveManager.Data.FamilyTree);
_debtManager.LoadFrom(SaveManager.Data.Debt);
_achievementManager.LoadFrom(SaveManager.Data.Achievements);
// ...
```

**保存時**:
```csharp
// SaveManager.Save() の直前
SaveManager.Data.FamilyTree = _familyTree.ToSaveData();
SaveManager.Data.Debt = _debtManager.ToSaveData();
SaveManager.Data.Achievements = _achievementManager.ToSaveData();
// ...
SaveManager.Save();
```

各マネージャーは `LoadFrom(XxxData)` と `XxxData ToSaveData()` の2メソッドを実装する。これによりファイルI/Oの責務はSaveManagerに一元化される。

**重要**: Phase 4〜8の個別md内で `void Save()` / `void Load()` を直接ファイルに書くと記載しているが、実際の実装ではSaveManagerを経由すること。各Phase mdの記載は概念的なインターフェース定義として読み、ファイルI/Oの実装はSaveManagerに集約する。

## 保存タイミング

| イベント | 保存内容 |
|---|---|
| 世代交代時（リスタート直前） | 家系図レコード追加、借金更新、実績更新、呪い研究度更新、ランキング更新 |
| 自動保存（30秒ごと） | 全データ（プレイ中の一時データ含む） |
| 装備画面を閉じた時 | インベントリ状態 |
| 設定変更時 | 設定データのみ |
| アプリ終了時（_Notification） | 全データ |

### アプリ終了時の保存

GameManagerでGodotの通知をキャッチして保存する。

```csharp
public override void _Notification(int what)
{
    if (what == NotificationWMCloseRequest ||
        what == NotificationApplicationPaused)  // Android バックグラウンド
    {
        SaveManager.Save();
    }
}
```

## エラーハンドリング

| 異常 | 対応 |
|---|---|
| JSONパースエラー | .bak → .bak2 の順でフォールバック。全滅なら新規データ生成 |
| ファイル書き込み失敗 | リトライ1回。失敗ならエラーログ出力し、次のAutoSaveに任せる |
| ディスク容量不足 | エラーログ出力。ゲームは続行可能だが保存できない旨をHUDに警告表示 |
| バージョン不一致 | SaveMigratorで自動マイグレーション |
| データ整合性エラー（例: generation番号の不整合） | 修復可能な範囲で自動修復。修復不能なら該当セクションのみリセット |

## メモリ管理

- SaveData全体は常時メモリに保持する（数KB〜数十KB程度のため問題なし）
- 家系図レコードが1000世代を超えた場合、古いレコードを圧縮保存（詳細データを削除し、名前・世代・深度のみ保持）
- JSONシリアライズ時の一時文字列はGCに任せる（Godot C#のGCで十分）

## テスト項目

- [ ] 新規起動で空のセーブデータが生成される
- [ ] セーブ→ロードでデータが復元される
- [ ] save_master.json を破損させた場合、.bakからリカバリーされる
- [ ] .jsonと.bakの両方を破損させた場合、.bak2からリカバリーされる
- [ ] 全ファイル破損時に新規データが生成される（クラッシュしない）
- [ ] バージョンの異なるセーブデータが正しくマイグレーションされる
- [ ] 30秒ごとにAutoSaveが実行される
- [ ] アプリ終了時（×ボタン / Androidバックグラウンド）にセーブされる
- [ ] 100世代分の家系図データでパフォーマンスに問題がない
- [ ] 全フィールドが正しくJSONシリアライズ/デシリアライズされる